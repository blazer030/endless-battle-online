namespace EndlessBattle.Domain.Characters;

public sealed record StatAllocation(int Strength, int Intelligence, int Life, int Agility);

public sealed record CreateCharacterCommand(
    string Name,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    StatAllocation Allocation);
