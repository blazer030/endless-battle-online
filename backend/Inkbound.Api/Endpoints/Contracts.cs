using Inkbound.Domain.Combat;

namespace Inkbound.Api.Endpoints;

// 欄位允許 null：缺欄位時由 Application 的驗證回 400，而不是在反序列化時失敗
public sealed record CredentialsRequest(string? Username, string? Password);

public sealed record RegisterResponse(long AccountId);

public sealed record LoginResponse(string AccessToken);

public sealed record AccountResponse(long AccountId, string Username);

public sealed record NamedContent(string Id, string Name);

public sealed record ContentResponse(IReadOnlyList<NamedContent> Monsters, IReadOnlyList<NamedContent> Stages);

public sealed record CreateCharacterRequest(string? Name);

public sealed record CharacterResponse(string Name, long Experience, long SpiritStones, string CurrentStageId);

public sealed record AdvanceResponse(
    IReadOnlyList<BattleEvent> Events,
    long ExperienceGained,
    long SpiritStonesGained,
    CharacterResponse Character);
