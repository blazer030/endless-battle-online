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
        Str: Roll(growth.Str, random),
        Smart: Roll(growth.Smart, random),
        Life: Roll(growth.Life, random),
        Vit: Roll(growth.Vit, random),
        Agi: Roll(growth.Agi, random),
        Au: Roll(growth.Au, random),
        Be: Roll(growth.Be, random));

    private static int Roll(StatRange range, IRandomSource random) => PhpRandom.Between(random, range.Min, range.Max);

    private static BaseStats Add(BaseStats stats, BaseStats growth) => new(
        stats.Str + growth.Str,
        stats.Smart + growth.Smart,
        stats.Life + growth.Life,
        stats.Vit + growth.Vit,
        stats.Agi + growth.Agi,
        stats.Au + growth.Au,
        stats.Be + growth.Be);
}
