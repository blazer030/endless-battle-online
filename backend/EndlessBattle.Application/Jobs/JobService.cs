using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Jobs;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Jobs;

public sealed record JobOption(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    string? ParentName,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirement> StatRequirements,
    JobGrowth Growth,
    bool Current,
    int ProficiencyPercent,
    bool Eligible,
    IReadOnlyList<string> Reasons);

public sealed class JobService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillLoadoutRepository loadout,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IGameContent content,
    IClock clock)
{
    public async Task<IReadOnlyList<JobOption>> ListAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var rows = await proficiencies.ListAsync(character.Id, cancellationToken);

        return content.AllJobs.Select(job =>
        {
            var check = JobChange.Evaluate(character, job, rows);
            var own = rows.FirstOrDefault(row => row.JobId == job.Id)?.Proficiency ?? 0;
            return new JobOption(
                job.Id,
                job.Name,
                job.Tier,
                job.ParentId,
                job.ParentId is null ? null : content.GetJob(job.ParentId).Name,
                job.UnlockLevel,
                job.ProficiencyRequired,
                job.StatRequirements,
                job.Growth,
                character.JobId == job.Id,
                JobProficiencyDisplay.Percent(own),
                check.Eligible,
                check.Reasons);
        }).ToList();
    }

    // 轉職時全部裝備卸回背包（spec §5.2），放不下就 400 bagFull、不轉職。
    // 先過版本鎖更新角色，成功後才清裝備欄、把補給併回背包、清被動欄；衝突或背包滿時什麼都不寫
    public async Task<CharacterProfile> ChangeAsync(long accountId, string jobId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var target = content.AllJobs.FirstOrDefault(job => job.Id == jobId)
            ?? throw new InputValidationException("找不到職業");
        var rows = await proficiencies.ListAsync(character.Id, cancellationToken);

        var check = JobChange.Evaluate(character, target, rows);
        if (!check.Eligible)
        {
            throw new InputValidationException("轉職條件不符", check.Reasons);
        }

        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var unequipped = Unequipping.TryReturnAll(state.Bag, state.Loadout, character.BagBonus, content.GetItem)
            ?? throw new InputValidationException("背包已滿，無法卸下全部裝備", ["bagFull"]);

        var updated = await characters.UpdateAsync(JobChange.Apply(character, target), cancellationToken);
        await UnequipAllAsync(character.Id, state.Loadout, unequipped, cancellationToken);
        var slots = await loadout.ListAsync(character.Id, cancellationToken);
        await loadout.ReplaceAsync(character.Id, slots.Where(slot => !SkillLoadout.IsPassiveSlot(slot.Slot)).ToList(), cancellationToken);
        return new CharacterProfile(
            CharacterStamina.Regenerated(updated, clock.UtcNow), rows, await equipment.LoadBonusAsync(updated.Id, content, cancellationToken));
    }

    // 欄位必須先清空：裝備欄引用背包列，刪除仍被引用的補給列會撞外鍵
    private async Task UnequipAllAsync(long characterId, Loadout loadout, UnequipAllResult unequipped, CancellationToken cancellationToken)
    {
        foreach (var slot in loadout.Equipped.Keys)
        {
            await equipment.SetSlotAsync(characterId, slot, null, null, cancellationToken);
        }

        if (unequipped.Supply is not { } supply)
        {
            return;
        }

        foreach (var entry in supply.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(characterId, entry.Id, entry.Quantity, cancellationToken);
        }

        foreach (var entry in supply.NewEntries)
        {
            await inventory.AddAsync(characterId, entry, cancellationToken);
        }

        await inventory.RemoveAsync(characterId, loadout.Equipped[EquipmentSlot.Supply].Id, cancellationToken);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
