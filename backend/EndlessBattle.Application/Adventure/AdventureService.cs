using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Items;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Rest;
using EndlessBattle.Application.Skills;
using EndlessBattle.Application.World;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.Adventure;

public sealed class AdventureService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillBookRepository skillBook,
    ISkillLoadoutRepository loadout,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IMissionRepository missions,
    IPetRepository pets,
    IMercenaryRepository mercenaries,
    IAreaPassRepository areaPasses,
    IBuffRepository buffs,
    ILodgingRepository lodgings,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
    IUnitOfWork unitOfWork)
{
    // 讀取與模擬（純 CPU）都在寫入之前，交易裡只有寫入：角色的版本更新是第一個寫入，
    // 同一角色的並行請求會在這一列上排隊；模擬依據的狀態若已過期，版本不符而 409。
    // 喝密藥時在外層交易內呼叫：外層在此之前沒有寫入、不持有列鎖，在交易內模擬不會延長鎖
    public async Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await characters.FindByAccountAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("尚未建立角色");

        // 裝備在一次推進中不變，只讀一次
        var equipped = await equipment.GetAsync(character.Id, cancellationToken);
        var bonus = EquipmentBonusCalculator.Sum(equipped, content.GetItem);
        var now = clock.UtcNow;
        var elapsedSeconds = (now - character.LastSettledAt).TotalSeconds;
        // 內容移除了目前地區時找不到：不需要模擬就照樣回傳空結果，角色才能移出去
        var currentPlace = content.AllPlaces.FirstOrDefault(place => place.Id == character.PlaceId);
        var pass = currentPlace is { Restricted: true } ? await areaPasses.FindAsync(character.Id, currentPlace.Id, cancellationToken) : null;

        // 時鐘倒退或剛結算過：什麼都不做，也不寫入
        if (elapsedSeconds <= 0)
        {
            return new AdvanceResult([], AdvanceSummary.Empty, EventsCoverAll: true, character,
                await proficiencies.ListAsync(character.Id, cancellationToken), bonus, AreaPassExpiresAt: pass?.ExpiresAt);
        }

        // 要模擬就必須有這個地區；已移除時 GetPlace 丟出內容錯誤
        var place = currentPlace ?? content.GetPlace(character.PlaceId);

        // 超過上限的離線時間直接捨棄，只模擬最後 MaxSimulatedSeconds 秒
        var from = elapsedSeconds > AdventureSimulator.MaxSimulatedSeconds
            ? now.AddSeconds(-AdventureSimulator.MaxSimulatedSeconds)
            : character.LastSettledAt;
        // 禁區只模擬到通行證到期為止；到期（或沒有通行證）後的時間捨棄，不自動再扣鑰匙
        var until = AreaKeys.SimulateUntil(place, pass, now);
        var tracked = KillCounter.Track(await missions.ListAsync(character.Id, cancellationToken), content.AllMissions, character.Id);
        var ownedPets = await pets.ListAsync(character.Id, cancellationToken);
        var supplySlot = SupplySlotOf(equipped, character.Settings);
        var carriedPet = ownedPets.FirstOrDefault(pet => pet.Carried);
        var contract = await mercenaries.FindAsync(character.Id, cancellationToken);
        var job = content.GetJob(character.JobId);
        var skills = await EquippedSkills.LoadAsync(loadout, skillBook, content, character, equipped, cancellationToken);
        var characterBuffs = await buffs.ListAsync(character.Id, cancellationToken);
        var lodging = Inn.ToBuff(await lodgings.FindAsync(character.Id, cancellationToken), content.Inn);
        var random = randomFactory.Create();

        // 傭兵的時間軸先結算，角色從傭兵帶回的經驗、等級與任務進度接著冒險；沒有出擊中的傭兵時不擲任何骰。
        // 內容裡已經沒有這位傭兵或這個地區時不結算
        var mercenaryRun = contract is { Fighting: true, PlaceId: { } placeId }
            && content.AllMercenaries.FirstOrDefault(mercenary => mercenary.Id == contract.MercenaryId) is { } mercenary
            && content.AllPlaces.Any(place => place.Id == placeId)
                ? MercenarySimulator.Run(character, job, contract, mercenary, content.MonstersOf(placeId), skills, bonus, carriedPet, tracked, now, random)
                : null;
        var run = AdventureSimulator.Run(
            mercenaryRun?.Character ?? character,
            job,
            place,
            content.MonstersOf(character.PlaceId),
            content.GetItem,
            skills,
            from,
            until > from ? until : from,
            random,
            bonus,
            supplySlot.Supply,
            mercenaryRun?.Missions ?? tracked,
            new AdventureCompanions(carriedPet, ownedPets.Count, supplySlot.Capture),
            characterBuffs,
            lodging);
        var mercenaryDrops = mercenaryRun?.DroppedItemIds ?? [];

        // 每次推進最多結算一次飢餓（同 WOG 的線上心跳），接在模擬之後擲骰
        var hungryPet = carriedPet is null ? null : PetCare.Hunger(carriedPet, now, random);

        return await unitOfWork.RunAsync(async cancellationToken =>
        {
            // 結算到最後一場打完的時間；最後一場可能超過現在，下次推進前會先回傳空結果。
            // 通行證在現在之前到期時直接結算到現在
            var updated = await characters.UpdateAsync(
                run.Character with
                {
                    LastSettledAt = until < now ? now : from.AddSeconds(run.ConsumedSeconds),
                },
                cancellationToken);

            // 角色以版本檢查寫入成功後才記熟練度，衝突的推進不會重複累加
            foreach (var gain in MergeJobGains(job.Id, mercenaryRun?.JobExp ?? 0, run.JobGains))
            {
                await proficiencies.AddAsync(updated.Id, gain.JobId, gain.Proficiency, gain.SkillPoints, cancellationToken);
            }

            // 任務進度同樣在角色寫入成功後；推進期間被放棄（刪除）的任務因角色版本已變而 409，不會寫回
            await StoreMissionProgressAsync(updated.Id, tracked, run.Missions, cancellationToken);

            var drops = await StoreDropsAsync(updated, run, mercenaryDrops, cancellationToken);
            await StoreSupplyAsync(updated.Id, equipped, run.SupplyRemaining, cancellationToken);

            // 捕到的寵物依捕捉順序寫入，之後才寫攜帶寵物的飢餓
            foreach (var captured in run.Captured)
            {
                await pets.AddAsync(updated.Id, captured.ToPet(), cancellationToken);
            }

            if (hungryPet is not null && !ReferenceEquals(hungryPet, carriedPet))
            {
                await pets.UpdateAsync(updated.Id, hungryPet, cancellationToken);
            }

            var report = await StoreMercenaryAsync(updated.Id, contract, mercenaryRun, cancellationToken);
            return new AdvanceResult(drops.Events, run.Summary with { Drops = drops.Accepted }, run.EventsCoverAll, updated,
                await proficiencies.ListAsync(updated.Id, cancellationToken), bonus, report, pass?.ExpiresAt);
        }, cancellationToken);
    }

    // 兩條時間軸都是同一個職業，熟練度合併成一筆寫入
    private static IReadOnlyList<JobGain> MergeJobGains(string jobId, long mercenaryJobExp, IReadOnlyList<JobGain> adventure)
    {
        if (mercenaryJobExp == 0)
        {
            return adventure;
        }

        var proficiency = mercenaryJobExp + adventure.Sum(gain => gain.Proficiency);
        var skillPoints = mercenaryJobExp + adventure.Sum(gain => gain.SkillPoints);
        return [new JobGain(jobId, proficiency, skillPoints)];
    }

    // 合約在所有寫入之後；次數用完就刪除。下一場還沒到（合約沒變）時不寫也不回報
    private async Task<MercenaryReport?> StoreMercenaryAsync(
        long characterId, MercenaryContract? before, MercenaryRun? run, CancellationToken cancellationToken)
    {
        if (run is null || before is null || run.Contract == before)
        {
            return null;
        }

        if (run.Contract is null)
        {
            await mercenaries.RemoveAsync(characterId, cancellationToken);
        }
        else
        {
            await mercenaries.UpdateAsync(characterId, run.Contract, cancellationToken);
        }

        return new MercenaryReport(
            before.Name,
            run.Fights,
            run.Fights.Sum(fight => fight.Exp),
            run.Fights.Sum(fight => fight.Money),
            run.LevelsGained,
            run.Contract?.Remaining ?? 0,
            Dismissed: run.Contract is null);
    }

    private async Task StoreMissionProgressAsync(
        long characterId, IReadOnlyList<ActiveMission> before, IReadOnlyList<ActiveMission> after, CancellationToken cancellationToken)
    {
        var progressBefore = before.ToDictionary(mission => mission.MissionId, mission => mission.Progress);
        foreach (var mission in after.Where(mission => mission.Progress != progressBefore[mission.MissionId]))
        {
            await missions.UpdateProgressAsync(characterId, mission.MissionId, [mission.Progress], cancellationToken);
        }
    }

    // 補給欄放的是補給品或捕捉道具（至多一種）；捕捉道具不會被當成補給品喝。補給門檻取自角色設定
    private (SupplyState? Supply, CaptureState? Capture) SupplySlotOf(Loadout equipped, AdventureSettings settings)
    {
        if (!equipped.Equipped.TryGetValue(EquipmentSlot.Supply, out var entry))
        {
            return (null, null);
        }

        var item = content.GetItem(entry.ItemId);
        return item.Consumable == ConsumableKind.Capture
            ? (null, new CaptureState(item.Id, item.Name, item.Amount, equipped.SupplyQuantity))
            : (new SupplyState(item.Id, item.Name, item.SupplyHp, item.SupplySp, equipped.SupplyQuantity)
            {
                HpPercent = settings.PotionHpPercent,
                SpPercent = settings.PotionSpPercent,
            }, null);
    }

    // 補給欄在掉落之後寫回；用完就清空補給欄並刪掉那一列（先清欄位，否則外鍵擋刪除）
    private async Task StoreSupplyAsync(long characterId, Loadout equipped, int? remaining, CancellationToken cancellationToken)
    {
        if (remaining is not { } quantity || quantity == equipped.SupplyQuantity)
        {
            return;
        }

        var entry = equipped.Equipped[EquipmentSlot.Supply];
        if (quantity > 0)
        {
            await equipment.SetSlotAsync(characterId, EquipmentSlot.Supply, entry.Id, quantity, cancellationToken);
            await inventory.UpdateQuantityAsync(characterId, entry.Id, quantity, cancellationToken);
            return;
        }

        await equipment.SetSlotAsync(characterId, EquipmentSlot.Supply, null, null, cancellationToken);
        await inventory.RemoveAsync(characterId, entry.Id, cancellationToken);
    }

    // 掉落在角色與熟練度之後才寫入背包，傭兵的在前、冒險的在後；事件裡的 Discarded 以背包實際放不放得下為準。
    // 事件只含窗口內的勝場，是全部掉落的尾段，所以旗標從尾端對齊。Accepted 只算冒險的掉落
    private async Task<(IReadOnlyList<BattleEvent> Events, int Accepted)> StoreDropsAsync(
        Character character, AdventureRun run, IReadOnlyList<string> mercenaryDrops, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> dropped = [.. mercenaryDrops, .. run.DroppedItemIds];
        if (dropped.Count == 0)
        {
            return (run.Events, 0);
        }

        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var applied = DropApplier.Apply(state.Bag, dropped, content.GetItem, character.BagBonus);
        foreach (var entry in applied.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
        }

        foreach (var entry in applied.NewEntries)
        {
            await inventory.AddAsync(character.Id, entry, cancellationToken);
        }

        var eventDrops = run.Events.OfType<RewardEvent>().Sum(reward => reward.Drops.Count);
        var flagIndex = dropped.Count - eventDrops;
        var events = run.Events.Select(battleEvent =>
        {
            if (battleEvent is not RewardEvent { Drops.Count: > 0 } reward)
            {
                return battleEvent;
            }

            var results = reward.Drops.Select(drop => drop with { Discarded = applied.Discarded[flagIndex++] }).ToList();
            return reward with { Drops = results };
        }).ToList();

        return (events, applied.Discarded.Skip(mercenaryDrops.Count).Count(discarded => !discarded));
    }
}
