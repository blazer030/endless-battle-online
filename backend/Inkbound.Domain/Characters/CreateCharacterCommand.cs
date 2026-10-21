namespace Inkbound.Domain.Characters;

public sealed record StatAllocation(int Str, int Smart, int Life, int Agi);

public sealed record CreateCharacterCommand(
    string Name,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    StatAllocation Allocation,
    string SpecialAttackName);
