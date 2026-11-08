using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Pets;

// 補給欄的捕捉道具；Multiplier 是能力倍率
public sealed record CaptureState(string ItemId, string Name, int Multiplier, int Quantity);

public sealed record CapturedPet(
    string MonsterId, string Name, PetPersonality Personality, int Attack, int Magic, int Defense, DateTimeOffset CapturedAt)
{
    private const int InitialSpirit = 1;
    private const int InitialSatiety = 80;
    private const int InitialIntimacy = 0;

    // 同 PHP time-20
    private static readonly TimeSpan HungerHeadStart = TimeSpan.FromSeconds(20);

    // Id 0 由 repository 給號；放獸欄
    public Pet ToPet() => new(
        0, MonsterId, Name, Personality, Attack, Magic, Defense, InitialSpirit, InitialSatiety, InitialIntimacy,
        Carried: false, BornAt: CapturedAt, HungerSince: CapturedAt - HungerHeadStart);
}

public sealed record CaptureRoll(CaptureState Capture, bool ItemBroken, CapturedPet? Captured);

public static class PetCapture
{
    public const int BreakOneIn = 40, ChancePercent = 12, StatRollMax = 10;

    private const int PersonalityCount = 4;

    // 勝場且沒有攜帶寵物時才呼叫。① rand(1,40) == 1 損壞 −1；② 還有道具才擲 rand(1,100) ≤ 12，
    // 寵物已滿時照擲但不建立寵物、不扣道具
    public static CaptureRoll Roll(CaptureState capture, MonsterDefinition monster, int petCount, DateTimeOffset at, IRandomSource random)
    {
        if (capture.Quantity <= 0)
        {
            return new CaptureRoll(capture, ItemBroken: false, Captured: null);
        }

        var broken = PhpRandom.Between(random, 1, BreakOneIn) == 1;
        if (broken)
        {
            capture = capture with { Quantity = capture.Quantity - 1 };
            if (capture.Quantity == 0)
            {
                return new CaptureRoll(capture, broken, Captured: null);
            }
        }

        if (PhpRandom.Between(random, 1, 100) > ChancePercent || petCount >= PetRules.MaxPets)
        {
            return new CaptureRoll(capture, broken, Captured: null);
        }

        return new CaptureRoll(capture with { Quantity = capture.Quantity - 1 }, broken, NewPet(monster, capture.Multiplier, at, random));
    }

    // 能力依序 Attack、Magic、Defense 各 rand(1,10) × 倍率，再擲個性 rand(1,4)；任務的寵物獎勵也用這個
    public static CapturedPet NewPet(MonsterDefinition monster, int multiplier, DateTimeOffset at, IRandomSource random)
    {
        var attack = PhpRandom.Between(random, 1, StatRollMax) * multiplier;
        var magic = PhpRandom.Between(random, 1, StatRollMax) * multiplier;
        var defense = PhpRandom.Between(random, 1, StatRollMax) * multiplier;
        var personality = (PetPersonality)PhpRandom.Between(random, 1, PersonalityCount);
        // 預設名字是怪物名，截到名字上限（以字碼計，與 CanRename 一致）
        var name = string.Concat(monster.Name.EnumerateRunes().Take(PetRules.NameMaxLength));
        return new CapturedPet(monster.Id, name, personality, attack, magic, defense, at);
    }
}
