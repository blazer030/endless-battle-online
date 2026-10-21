using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
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
    IGameContent content)
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

    // 先過版本鎖更新角色，成功後才清被動欄；衝突時技能欄位不動
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

        var updated = await characters.UpdateAsync(JobChange.Apply(character, target), cancellationToken);
        var slots = await loadout.ListAsync(character.Id, cancellationToken);
        await loadout.ReplaceAsync(character.Id, slots.Where(slot => !SkillLoadout.IsPassiveSlot(slot.Slot)).ToList(), cancellationToken);
        return new CharacterProfile(updated, rows);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
