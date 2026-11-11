using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Progression;

public sealed record LevelUpResult(Character Character, IReadOnlyList<LevelUpEvent> Events);

// WOG lv_up：exp 嚴格大於 nextExp 才升級；成長取當時職業的範圍；不回滿 HP／SP；沒有等級上限
public static class LevelUp
{
    public static LevelUpResult Apply(Character character, JobDefinition job, IRandomSource random, double atSeconds)
    {
        var events = new List<LevelUpEvent>();
        while (character.Exp > character.NextExp)
        {
            var level = character.Level + 1;
            var growth = RollGrowth(job.Growth, random);
            character = character with
            {
                Level = level,
                Exp = character.Exp - character.NextExp,
                NextExp = ExperienceCurve.NextExpFor(level),
                Base = Add(character.Base, growth),
            };
            events.Add(new LevelUpEvent(atSeconds, level, growth));
        }

        return new LevelUpResult(character, events);
    }

    private static BaseStats RollGrowth(JobGrowth growth, IRandomSource random) => new(
        Strength: Roll(growth.Strength, random),
        Intelligence: Roll(growth.Intelligence, random),
        Life: Roll(growth.Life, random),
        Vitality: Roll(growth.Vitality, random),
        Agility: Roll(growth.Agility, random),
        Charisma: Roll(growth.Charisma, random),
        Faith: Roll(growth.Faith, random));

    private static int Roll(StatRange range, IRandomSource random) => PhpRandom.Between(random, range.Minimum, range.Maximum);

    private static BaseStats Add(BaseStats stats, BaseStats growth) => new(
        stats.Strength + growth.Strength,
        stats.Intelligence + growth.Intelligence,
        stats.Life + growth.Life,
        stats.Vitality + growth.Vitality,
        stats.Agility + growth.Agility,
        stats.Charisma + growth.Charisma,
        stats.Faith + growth.Faith);
}
