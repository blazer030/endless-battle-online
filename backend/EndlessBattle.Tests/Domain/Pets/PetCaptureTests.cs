using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Pets;

// ScriptedRandom 的 rand(1, n) = ⌊擲骰 × n⌋ + 1：0.0 → 1、0.5 → 損壞 21／捕捉 51、0.115 → 12、0.125 → 13
public class PetCaptureTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly MonsterDefinition Sprite = new(
        "test-sprite", "雪精", Level: 3, Hp: 60, Sp: 10, At: 14, Mat: 8, Df: 6, Mdf: 4, Agi: 9, Au: 6, Element: 2, JobExp: 12, Meet: 0);

    private static CaptureState Net(int quantity = 5) => new("capture-net", "捕獸網", Multiplier: 1, quantity);

    // 損壞擲 1 → 數量 −1，接著照擲捕捉（51 > 12 沒捕到）；損壞擲 21 → 不損壞
    [Fact]
    public void Roll_BreaksOneInForty()
    {
        var broken = new ScriptedRandom(0.5, 0.0, 0.5);
        var roll = PetCapture.Roll(Net(), Sprite, petCount: 0, At, broken);

        Assert.Equal(new CaptureRoll(Net(4), ItemBroken: true, Captured: null), roll);
        Assert.Equal(2, broken.Consumed);

        var intact = PetCapture.Roll(Net(), Sprite, petCount: 0, At, new ScriptedRandom(0.5));
        Assert.Equal(new CaptureRoll(Net(5), ItemBroken: false, Captured: null), intact);
    }

    // 不損壞（21）；捕捉擲 12 → 捕到、數量 −1；擲 13 → 沒捕到、數量不變
    [Theory]
    [InlineData(0.115, true)]
    [InlineData(0.125, false)]
    public void Roll_CapturesAtTwelvePercent(double chanceRoll, bool captured)
    {
        var roll = PetCapture.Roll(Net(), Sprite, petCount: 2, At, new ScriptedRandom(0.0, 0.5, chanceRoll));

        Assert.False(roll.ItemBroken);
        Assert.Equal(captured, roll.Captured is not null);
        Assert.Equal(captured ? 4 : 5, roll.Capture.Quantity);
    }

    // 倍率 8：擲 3、10、1 → Attack 24、Magic 80、Defense 8；個性擲 4 → 危急
    [Fact]
    public void NewPet_RollsStatsTimesMultiplier()
    {
        var random = new ScriptedRandom(0.0, 0.25, 0.95, 0.0, 0.8);

        var captured = PetCapture.NewPet(Sprite, multiplier: 8, At, random);

        Assert.Equal(new CapturedPet("test-sprite", "雪精", PetPersonality.Desperate, Attack: 24, Magic: 80, Defense: 8, CapturedAt: At), captured);
        Assert.Equal(4, random.Consumed);
        Assert.Equal(
            new Pet(0, "test-sprite", "雪精", PetPersonality.Desperate, 24, 80, 8, Spirit: 1, Satiety: 80, Intimacy: 0, Carried: false,
                BornAt: At, HungerSince: At.AddSeconds(-20)),
            captured.ToPet());
    }

    // 預設名字取怪物名，超過 12 個字碼就截斷（含非 BMP 字元，以字碼而非 UTF-16 單位計）
    [Fact]
    public void NewPet_TruncatesLongMonsterName()
    {
        var longName = "一二三四五六七八九十甲😀乙丙";
        var monster = Sprite with { Name = longName };

        var captured = PetCapture.NewPet(monster, multiplier: 1, At, new ScriptedRandom(0.0, 0.0, 0.0, 0.0));

        Assert.Equal("一二三四五六七八九十甲😀", captured.Name);
        Assert.Empty(PetRules.CanRename(captured.Name));
    }

    // 寵物已有 3 隻：照擲損壞（21）與捕捉（1），但不建立寵物、不扣道具
    [Fact]
    public void Roll_SkipsWhenPetsFull()
    {
        var random = new ScriptedRandom(0.0, 0.5, 0.0);

        var roll = PetCapture.Roll(Net(), Sprite, petCount: 3, At, random);

        Assert.Equal(new CaptureRoll(Net(5), ItemBroken: false, Captured: null), roll);
        Assert.Equal(2, random.Consumed);
    }

    // 最後一個道具損壞 → 數量 0，不再擲捕捉；數量 0 時一個亂數都不擲
    [Fact]
    public void Roll_StopsAfterLastItemBreaks()
    {
        var random = new ScriptedRandom(0.0);

        var roll = PetCapture.Roll(Net(1), Sprite, petCount: 0, At, random);

        Assert.Equal(new CaptureRoll(Net(0), ItemBroken: true, Captured: null), roll);
        Assert.Equal(1, random.Consumed);

        var empty = new ScriptedRandom(0.0);
        Assert.Equal(new CaptureRoll(Net(0), ItemBroken: false, Captured: null), PetCapture.Roll(Net(0), Sprite, petCount: 0, At, empty));
        Assert.Equal(0, empty.Consumed);
    }
}
