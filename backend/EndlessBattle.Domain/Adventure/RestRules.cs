using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Adventure;

// spec §7.1 自動住宿：死亡一定住；門檻 -1 略過該條件。SP 只有住宿才會回復、被動技能不耗 SP，所以只在裝備了主動技能時才看 SP 門檻
public static class RestRules
{
    public static bool ShouldRest(Character character, DerivedStats derived, bool hasActiveSkills)
    {
        var settings = character.Settings;
        return character.HealthPoints <= 0
            || Below(character.HealthPoints, derived.MaxHealthPoints, settings.RestHealthPointsPercent)
            || (hasActiveSkills && Below(character.ManaPoints, derived.MaxManaPoints, settings.RestManaPointsPercent));
    }

    private static bool Below(int value, int maximum, int percent) =>
        percent >= 0 && (long)value * 100 < (long)percent * maximum;
}
