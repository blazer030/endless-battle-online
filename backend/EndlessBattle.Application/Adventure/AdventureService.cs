using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Application.Adventure;

public sealed class AdventureService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillBookRepository skillBook,
    ISkillLoadoutRepository loadout,
    IEquipmentRepository equipment,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory)
{
    public async Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await characters.FindByAccountAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("尚未建立角色");

        // 裝備在一次推進中不變，只讀一次
        var bonus = await equipment.LoadBonusAsync(character.Id, content, cancellationToken);
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
            await LoadEquippedSkillsAsync(character, cancellationToken),
            new StaminaState(character.ActNum, character.ActNumTime),
            from,
            now,
            randomFactory.Create(),
            bonus);

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
            await proficiencies.ListAsync(updated.Id, cancellationToken), bonus);
    }

    // 內容檔移除的技能以 null 交給 Domain 略過；技能只有十幾個，線性查找即可
    private async Task<IReadOnlyList<EquippedSkill>> LoadEquippedSkillsAsync(Character character, CancellationToken cancellationToken) =>
        SkillLoadout.ForBattle(
            await loadout.ListAsync(character.Id, cancellationToken),
            await skillBook.ListAsync(character.Id, cancellationToken),
            character.JobId,
            skillId => content.AllSkills.FirstOrDefault(skill => skill.Id == skillId));
}
