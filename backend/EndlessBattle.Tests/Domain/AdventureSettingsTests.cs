using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Tests.Domain;

public class AdventureSettingsTests
{
    [Fact]
    public void Settings_DefaultMatchesPlanNine()
    {
        Assert.Equal(new AdventureSettings(null, 15, 100, 15, 15, 0), AdventureSettings.Default);
    }

    // 補給與住宿 -1～100（-1 略過條件），逃跑 0～100（0 不逃）
    [Fact]
    public void Settings_ValidateRanges()
    {
        Assert.Empty(new AdventureSettings(AttackKind.Magic, -1, -1, -1, -1, 0).Validate());
        Assert.Empty(new AdventureSettings(AttackKind.Physical, 100, 100, 100, 100, 100).Validate());

        AdventureSettings[] invalid =
        [
            AdventureSettings.Default with { PotionHealthPointsPercent = -2 },
            AdventureSettings.Default with { PotionHealthPointsPercent = 101 },
            AdventureSettings.Default with { PotionManaPointsPercent = -2 },
            AdventureSettings.Default with { PotionManaPointsPercent = 101 },
            AdventureSettings.Default with { RestHealthPointsPercent = -2 },
            AdventureSettings.Default with { RestHealthPointsPercent = 101 },
            AdventureSettings.Default with { RestManaPointsPercent = -2 },
            AdventureSettings.Default with { RestManaPointsPercent = 101 },
            AdventureSettings.Default with { EscapeHealthPointsPercent = -1 },
            AdventureSettings.Default with { EscapeHealthPointsPercent = 101 },
            new AdventureSettings(null, 101, -2, 101, -2, 101),
        ];
        Assert.All(invalid, settings => Assert.Equal(["invalidPercent"], settings.Validate()));
    }

    [Fact]
    public void Settings_RejectsUnknownAttackKind()
    {
        Assert.Equal(["invalidAttack"], (AdventureSettings.Default with { Attack = (AttackKind)7 }).Validate());
    }
}
