using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Application.Adventure;

public sealed class AdventureService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillBookRepository skillBook,
    ISkillLoadoutRepository loadout,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
    IUnitOfWork unitOfWork)
{
    // 角色寫入、熟練度、掉落與補給同屬一筆交易：角色的版本更新是第一個寫入，
    // 同一角色的並行請求會在這一列上排隊，後到的因版本不符而 409
    public Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await characters.FindByAccountAsync(accountId, cancellationToken)
                ?? throw new NotFoundException("尚未建立角色");

            // 裝備在一次推進中不變，只讀一次
            var equipped = await equipment.GetAsync(character.Id, cancellationToken);
            var bonus = EquipmentBonusCalculator.Sum(equipped, content.GetItem);
            var now = clock.UtcNow;
            var elapsedSeconds = (now - character.LastSettledAt).TotalSeconds;

            // 時鐘倒退或剛結算過：什麼都不做，也不寫入
            if (elapsedSeconds <= 0)
            {
                return new AdvanceResult([], AdvanceSummary.Empty, EventsCoverAll: true, character,
                    await proficiencies.ListAsync(character.Id, cancellationToken), bonus);
            }

            // 超過上限的離線時間直接捨棄，只模擬最後 MaxSimulatedSeconds 秒
            var from = elapsedSeconds > AdventureSimulator.MaxSimulatedSeconds
                ? now.AddSeconds(-AdventureSimulator.MaxSimulatedSeconds)
                : character.LastSettledAt;
            var run = AdventureSimulator.Run(
                character,
                content.GetJob(character.JobId),
                content.GetPlace(character.PlaceId),
                content.MonstersOf(character.PlaceId),
                content.GetItem,
                await LoadEquippedSkillsAsync(character, cancellationToken),
                new StaminaState(character.ActNum, character.ActNumTime),
                from,
                now,
                randomFactory.Create(),
                bonus,
                SupplyOf(equipped));

            // 結算到最後一場打完的時間；最後一場可能超過現在，下次推進前會先回傳空結果
            var updated = await characters.UpdateAsync(
                run.Character with
                {
                    ActNum = run.Stamina.ActNum,
                    ActNumTime = run.Stamina.LastRegenAt,
                    LastSettledAt = from.AddSeconds(run.ConsumedSeconds),
                },
                cancellationToken);

            // 角色以版本檢查寫入成功後才記熟練度，衝突的推進不會重複累加
            foreach (var gain in run.JobGains)
            {
                await proficiencies.AddAsync(updated.Id, gain.JobId, gain.Proficiency, gain.SkillPoints, cancellationToken);
            }

            var drops = await StoreDropsAsync(updated, run, cancellationToken);
            await StoreSupplyAsync(updated.Id, equipped, run.SupplyRemaining, cancellationToken);

            return new AdvanceResult(drops.Events, run.Summary with { Drops = drops.Accepted }, run.EventsCoverAll, updated,
                await proficiencies.ListAsync(updated.Id, cancellationToken), bonus);
        }, cancellationToken);

    private SupplyState? SupplyOf(Loadout equipped)
    {
        if (!equipped.Equipped.TryGetValue(EquipmentSlot.Supply, out var entry))
        {
            return null;
        }

        var item = content.GetItem(entry.ItemId);
        return new SupplyState(item.Id, item.Name, item.SupplyHp, item.SupplySp, equipped.SupplyQuantity);
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

    // 掉落在角色與熟練度之後才寫入背包；事件裡的 Discarded 以背包實際放不放得下為準。
    // 事件只含窗口內的勝場，是全部掉落的尾段，所以旗標從尾端對齊
    private async Task<(IReadOnlyList<BattleEvent> Events, int Accepted)> StoreDropsAsync(
        Character character, AdventureRun run, CancellationToken cancellationToken)
    {
        if (run.DroppedItemIds.Count == 0)
        {
            return (run.Events, 0);
        }

        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var applied = DropApplier.Apply(state.Bag, run.DroppedItemIds, content.GetItem, character.BagBonus);
        foreach (var entry in applied.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
        }

        foreach (var entry in applied.NewEntries)
        {
            await inventory.AddAsync(character.Id, entry, cancellationToken);
        }

        var eventDrops = run.Events.OfType<RewardEvent>().Sum(reward => reward.Drops.Count);
        var flagIndex = run.DroppedItemIds.Count - eventDrops;
        var events = run.Events.Select(battleEvent =>
        {
            if (battleEvent is not RewardEvent { Drops.Count: > 0 } reward)
            {
                return battleEvent;
            }

            var results = reward.Drops.Select(drop => drop with { Discarded = applied.Discarded[flagIndex++] }).ToList();
            return reward with { Drops = results };
        }).ToList();

        return (events, applied.Discarded.Count(discarded => !discarded));
    }

    // 內容檔移除的技能以 null 交給 Domain 略過；技能只有十幾個，線性查找即可
    private async Task<IReadOnlyList<EquippedSkill>> LoadEquippedSkillsAsync(Character character, CancellationToken cancellationToken) =>
        SkillLoadout.ForBattle(
            await loadout.ListAsync(character.Id, cancellationToken),
            await skillBook.ListAsync(character.Id, cancellationToken),
            character.JobId,
            skillId => content.AllSkills.FirstOrDefault(skill => skill.Id == skillId));
}
