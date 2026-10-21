namespace EndlessBattle.Domain.Content;

public sealed record StageDefinition(string Id, string Name, IReadOnlyList<string> MonsterIds);
