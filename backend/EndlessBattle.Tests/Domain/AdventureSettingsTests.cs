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
            AdventureSettings.Default with { PotionHpPercent = -2 },
            AdventureSettings.Default with { PotionHpPercent = 101 },
            AdventureSettings.Default with { PotionSpPercent = -2 },
            AdventureSettings.Default with { PotionSpPercent = 101 },
            AdventureSettings.Default with { RestHpPercent = -2 },
            AdventureSettings.Default with { RestHpPercent = 101 },
            AdventureSettings.Default with { RestSpPercent = -2 },
            AdventureSettings.Default with { RestSpPercent = 101 },
            AdventureSettings.Default with { EscapeHpPercent = -1 },
            AdventureSettings.Default with { EscapeHpPercent = 101 },
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
