namespace EndlessBattle.Domain.Pets;

public enum PetPersonality
{
    Aggressive = 1,
    Cold = 2,
    Ironwall = 3,
    Desperate = 4,
}

// Defense 也是代擋量；Spirit 是出擊值；HungerSince 是餵食與飢餓共用的計時起點
public sealed record Pet(
    long Id,
    string MonsterId,
    string Name,
    PetPersonality Personality,
    int Attack,
    int Magic,
    int Defense,
    int Spirit,
    int Satiety,
    int Intimacy,
    bool Carried,
    DateTimeOffset BornAt,
    DateTimeOffset HungerSince);
