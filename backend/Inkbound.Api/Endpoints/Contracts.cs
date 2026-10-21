using Inkbound.Domain.Combat;

namespace Inkbound.Api.Endpoints;

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
    IReadOnlyList<StatRequirementContent> StatRequirements);

public sealed record StatRequirementContent(string Stat, int Minimum);

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
    string CurrentStageId);

public sealed record AdvanceResponse(
    IReadOnlyList<BattleEvent> Events,
    long ExpGained,
    long MoneyGained,
    CharacterResponse Character);
