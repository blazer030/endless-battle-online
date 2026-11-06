namespace EndlessBattle.Domain.Missions;

public sealed record KillTarget(string MonsterId, int Count);

public sealed record ItemQuantity(string ItemId, int Quantity);

// null 表示不限（WOG 的職業 99、性別 3、出生地 0）；ConflictsWith 對應 WOG m_not_id
public sealed record MissionRequirements(
    string? JobId = null, int? Sex = null, int? Birth = null, string? PrerequisiteId = null, string? ConflictsWith = null);

public sealed record SkillReward(string SkillId, int Level);

// Level 是能力倍率（WOG g_pet_lv）：新寵物同捕捉的初始值，Attack、Magic、Defense 各 rand(1,10) × Level
public sealed record PetReward(string MonsterId, int Level);

public sealed record MissionReward(long Money, IReadOnlyList<ItemQuantity> Items, SkillReward? Skill = null, PetReward? Pet = null);

// 一般任務：Kills 與 Deliver 至少一項；每日任務：Rewards 為空，目標從 DailyKills 或 DailyPets（恰好一個非空）擲出、報酬從 DailyRewards 擲出
public sealed record MissionDefinition(
    string Id,
    string Giver,
    string Title,
    string Description,
    string CompletionMessage,
    int Level,
    int? PlaceId,
    bool Daily,
    IReadOnlyList<KillTarget> Kills,
    IReadOnlyList<ItemQuantity> Deliver,
    MissionRequirements Requirements,
    MissionReward Rewards,
    IReadOnlyList<KillTarget> DailyKills,
    IReadOnlyList<MissionReward> DailyRewards,
    IReadOnlyList<string>? DailyPets = null)
{
    public IReadOnlyList<string> DailyPets { get; init; } = DailyPets ?? [];

    // WOG wog_mission_tool.php：經驗 = m_lv × 190
    public const long ExpPerLevel = 190;

    public long Exp => Level * ExpPerLevel;
}
