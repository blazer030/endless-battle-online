namespace EndlessBattle.Application.Admin.Content;

public sealed class MonsterSkillCheck(IContentDraftStore drafts) : IContentRecordCheck<MonsterRecord>
{
    public async Task<ContentFieldError?> FindErrorAsync(MonsterRecord record, CancellationToken cancellationToken)
    {
        var skillIds = await drafts.ReadIdsAsync(ContentKind.Find("skills")!, cancellationToken);
        for (var index = 0; index < record.Skills.Count; index++)
        {
            var skillId = record.Skills[index].SkillId;
            if (!skillIds.Contains(skillId))
            {
                return new ContentFieldError($"skills[{index}].skillId", $"技能 {skillId} 不存在");
            }
        }

        return null;
    }
}
