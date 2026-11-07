namespace EndlessBattle.Application.Admin.Content;

public sealed class SkillJobCheck(IContentDraftStore drafts) : IContentRecordCheck<SkillRecord>
{
    public async Task<ContentFieldError?> FindErrorAsync(SkillRecord record, CancellationToken cancellationToken)
    {
        var jobIds = await drafts.ReadIdsAsync(ContentKind.Find("jobs")!, cancellationToken);
        for (var index = 0; index < record.JobIds.Count; index++)
        {
            var jobId = record.JobIds[index];
            if (!jobIds.Contains(jobId))
            {
                return new ContentFieldError($"jobIds[{index}]", $"職業 {jobId} 不存在");
            }
        }

        return null;
    }
}
