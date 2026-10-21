using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Api.Endpoints;

// 欄位允許 null：缺欄位時由 Application 的驗證回 400，而不是在反序列化時失敗
public sealed record CredentialsRequest(string? Username, string? Password);

public sealed record RegisterResponse(long AccountId);

public sealed record LoginResponse(string AccessToken);

public sealed record AccountResponse(long AccountId, string Username);

public sealed record NamedContent(string Id, string Name);

public sealed record JobContent(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirementContent> StatRequirements,
    JobGrowthContent Growth);

public sealed record StatRequirementContent(string Stat, int Minimum);

// 每項為 [min, max]
public sealed record JobGrowthContent(int[] Str, int[] Smart, int[] Life, int[] Vit, int[] Agi, int[] Au, int[] Be);

public sealed record ContentResponse(
    IReadOnlyList<NamedContent> Monsters,
    IReadOnlyList<NamedContent> Stages,
    IReadOnlyList<JobContent> Jobs,
    IReadOnlyList<NamedContent> Elements,
    IReadOnlyList<NamedContent> Births,
    IReadOnlyList<NamedContent> Sexes);

public sealed record CreateCharacterRequest(
    string? Name,
    int? Sex,
    int? Element,
    int? Birth,
    string? JobId,
    StatAllocationRequest? Allocation,
    string? SpecialAttackName);

public sealed record StatAllocationRequest(int? Str, int? Smart, int? Life, int? Agi);

public sealed record StatsResponse(
    int Str,
    int Smart,
    int Life,
    int Vit,
    int Agi,
    int Au,
    int Be,
    int At,
    int Mat,
    int Df,
    int Mdf,
    int HpMax,
    int SpMax);

public sealed record CharacterResponse(
    string Name,
    int Level,
    long Exp,
    long NextExp,
    long Money,
    int Hp,
    int Sp,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    string JobName,
    string SpecialAttackName,
    StatsResponse Stats,
    string CurrentStageId,
    int ActNum,
    int ActNumMax,
    IReadOnlyList<JobProficiencyResponse> JobProficiency);

public sealed record JobProficiencyResponse(string JobId, long Proficiency, long SkillPoints);

// Events 只含最後 30 秒開始的場次與住宿，其餘只計入 Summary；EventsCoverAll 為 false 表示有戰鬥被略去（只略去住宿仍為 true）
public sealed record AdvanceResponse(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    bool EventsCoverAll,
    CharacterResponse Character);

public sealed record LearnSkillRequest(string? SkillId);

// 省略的欄位＝清空；主動欄省略 Chance 時用預設發動率，被動欄的 Chance 一律由伺服器填入
public sealed record SetSkillSlotsRequest(IReadOnlyList<SkillSlotRequest?>? Slots);

public sealed record SkillSlotRequest(int? Slot, string? SkillId, int? Chance);

public sealed record SkillOverviewResponse(
    string JobId,
    long SkillPoints,
    IReadOnlyList<SkillResponse> Skills,
    IReadOnlyList<SkillSlotResponse> Slots);

// Level 0 表示尚未學習；Next 在已滿級時為 null
public sealed record SkillResponse(
    string Id,
    string Name,
    SkillType Type,
    SkillEffectKind Effect,
    string Description,
    string JobId,
    int Level,
    int MaxLevel,
    SkillNextLevelResponse? Next);

public sealed record SkillNextLevelResponse(
    int Level,
    int Sp,
    int Rounds,
    int Chance,
    long Money,
    long Proficiency,
    bool CanLearn,
    IReadOnlyList<string> Reasons);

public sealed record SkillSlotResponse(int Slot, string SkillId, int Chance);
