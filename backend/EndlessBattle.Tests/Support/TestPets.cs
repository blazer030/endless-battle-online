using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Tests.Support;

public static class TestPets
{
    private static readonly DateTimeOffset Born = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Pet Create(
        string name = "小雪",
        string monsterId = "test-sprite",
        PetPersonality personality = PetPersonality.Aggressive,
        int attack = 10,
        int magic = 10,
        int defense = 10,
        int spirit = 1,
        int intimacy = 0,
        bool carried = true) =>
        new(0, monsterId, name, personality, attack, magic, defense, spirit, Satiety: 80, intimacy, carried, Born, Born);
}
