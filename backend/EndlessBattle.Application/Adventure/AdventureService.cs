using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Application.Adventure;

public sealed class AdventureService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillBookRepository skillBook,
    ISkillLoadoutRepository loadout,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory)
{
    public async Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await characters.FindByAccountAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("尚未建立角色");

        var now = clock.UtcNow;
        var elapsedSeconds = (now - character.LastSettledAt).TotalSeconds;

        // 時鐘倒退或剛結算過：什麼都不做，也不寫入
        if (elapsedSeconds <= 0)
        {
            return new AdvanceResult([], AdvanceSummary.Empty, EventsCoverAll: true, character,
                await proficiencies.ListAsync(character.Id, cancellationToken));
        }

        // 超過上限的離線時間直接捨棄，只模擬最後 MaxSimulatedSeconds 秒
        var from = elapsedSeconds > AdventureSimulator.MaxSimulatedSeconds
            ? now.AddSeconds(-AdventureSimulator.MaxSimulatedSeconds)
            : character.LastSettledAt;
        var run = AdventureSimulator.Run(
            character,
            content.GetJob(character.JobId),
            content.GetStage(character.CurrentStageId),
            await LoadEquippedSkillsAsync(character, cancellationToken),
            content.GetMonster,
            new StaminaState(character.ActNum, character.ActNumTime),
            from,
            now,
            randomFactory.Create());

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

        return new AdvanceResult(run.Events, run.Summary, run.EventsCoverAll, updated,
            await proficiencies.ListAsync(updated.Id, cancellationToken));
    }

    private async Task<IReadOnlyList<EquippedSkill>> LoadEquippedSkillsAsync(Character character, CancellationToken cancellationToken)
    {
        var learnedLevels = (await skillBook.ListAsync(character.Id, cancellationToken))
            .ToDictionary(skill => skill.SkillId, skill => skill.Level, StringComparer.Ordinal);
        var equipped = new List<EquippedSkill>();
        foreach (var slot in await loadout.ListAsync(character.Id, cancellationToken))
        {
            // 欄位指向技能書沒有的技能（資料不一致）就略過
            if (!learnedLevels.TryGetValue(slot.SkillId, out var level))
            {
                continue;
            }

            // 轉職與推進同時發生時，被動欄可能還留著前一個職業的技能；主動欄照 WOG 不限職業
            var skill = content.GetSkill(slot.SkillId);
            if (SkillLoadout.IsPassiveSlot(slot.Slot) && skill.JobId != character.JobId)
            {
                continue;
            }

            equipped.Add(new EquippedSkill(skill, level, slot.Chance, slot.Slot));
        }

        return equipped;
    }
}
