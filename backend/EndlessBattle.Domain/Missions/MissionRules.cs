using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Missions;

public static class MissionRules
{
    public const int MaxActive = 3;

    // 沒有技能條件也算；比對同一技能的已學等級（WOG mission_check）
    public static bool HasRequiredSkill(MissionDefinition mission, IReadOnlyList<LearnedSkill> learned) =>
        mission.RequiredSkill is not { } required
        || learned.Any(known => known.SkillId == required.SkillId && known.Level >= required.Level);

    // records：已去掉過期每日紀錄與未知任務。代碼依序：MISSION_UNKNOWN（單獨回傳）、ALREADY_ACTIVE／ALREADY_COMPLETED（單獨回傳）、
    // TOO_MANY、LEVEL_TOO_LOW、JOB_MISMATCH、SEX_MISMATCH、BIRTH_MISMATCH、PREREQUISITE_MISSING、MISSION_CONFLICT
    public static IReadOnlyList<string> CanAccept(Character character, MissionDefinition? mission, IReadOnlyList<MissionRecord> records)
    {
        if (mission is null)
        {
            return ["MISSION_UNKNOWN"];
        }

        var own = records.FirstOrDefault(record => record.MissionId == mission.Id);
        if (own is not null)
        {
            return [own.State == MissionState.Active ? "ALREADY_ACTIVE" : "ALREADY_COMPLETED"];
        }

        var requirements = mission.Requirements;
        var reasons = new List<string>();
        if (records.Count(record => record.State == MissionState.Active) >= MaxActive)
        {
            reasons.Add("TOO_MANY");
        }

        if (character.Level < mission.Level)
        {
            reasons.Add("LEVEL_TOO_LOW");
        }

        if (requirements.JobId is not null && requirements.JobId != character.JobId)
        {
            reasons.Add("JOB_MISMATCH");
        }

        if (requirements.Sex is not null && requirements.Sex != character.Sex)
        {
            reasons.Add("SEX_MISMATCH");
        }

        if (requirements.Birth is not null && requirements.Birth != character.Birth)
        {
            reasons.Add("BIRTH_MISMATCH");
        }

        if (requirements.PrerequisiteId is not null
            && !records.Any(record => record.MissionId == requirements.PrerequisiteId && record.State == MissionState.Completed))
        {
            reasons.Add("PREREQUISITE_MISSING");
        }

        if (requirements.ConflictsWith is not null && records.Any(record => record.MissionId == requirements.ConflictsWith))
        {
            reasons.Add("MISSION_CONFLICT");
        }

        return reasons;
    }
}
