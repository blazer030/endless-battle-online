using System.Text.RegularExpressions;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content/monsters.json：plan 6 Global Constraints 的怪物總表規則
public partial class MonsterRosterTests
{
    private const int MonstersPerPlace = 3;
    private const int EliteMeet = 60;
    private const double MinimumBand = 0.5;
    private const double MaximumBand = 1.6;

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    public static TheoryData<string> MonsterIds => new(Content.AllMonsters.Select(monster => monster.Id));

    [GeneratedRegex("^[a-z]+(-[a-z]+)*$")]
    private static partial Regex KebabCase();

    [Fact]
    public void Roster_CoversEveryPlaceWithThreeMonsters()
    {
        Assert.Equal(65, Content.AllMonsters.Count);
        Assert.All(Content.AllMonsters, monster => Assert.Matches(KebabCase(), monster.Id));

        foreach (var place in Content.AllPlaces)
        {
            var monsters = Content.AllMonsters.Where(monster => monster.PlaceId == place.Id).ToList();
            Assert.Equal(MonstersPerPlace, monsters.Count);
            Assert.All(monsters, monster =>
            {
                Assert.InRange(monster.Level, place.Recommended.Min, place.Recommended.Max);
                Assert.Contains(monster.Meet, new[] { 0, 30, EliteMeet });
                if (place.HasSubPlaces)
                {
                    Assert.NotEmpty(monster.SubPlaces);
                    Assert.All(monster.SubPlaces, subPlace => Assert.InRange(subPlace, 1, 4));
                }
                else
                {
                    Assert.Empty(monster.SubPlaces);
                }
            });
            Assert.InRange(monsters.Count(monster => monster.Meet == EliteMeet), 0, 1);
        }
    }

    [Fact]
    public void Roster_HasTwoGlobalMonsters()
    {
        var globals = Content.AllMonsters.Where(monster => monster.PlaceId == 0).ToList();

        Assert.Equal([1, 5], globals.Select(monster => monster.Level).Order());
        Assert.All(globals, monster =>
        {
            Assert.Equal(80, monster.Meet);
            Assert.Empty(monster.SubPlaces);
        });
    }

    [Fact]
    public void Roster_EverySubPlaceHasAMonster()
    {
        foreach (var place in Content.AllPlaces.Where(place => place.HasSubPlaces))
        {
            var covered = Content.AllMonsters
                .Where(monster => monster.PlaceId == place.Id)
                .SelectMany(monster => monster.SubPlaces)
                .ToHashSet();
            Assert.All(place.SubPlaces, subPlace => Assert.Contains(subPlace.Index, covered));
        }
    }

    [Theory]
    [MemberData(nameof(MonsterIds))]
    public void Roster_StatsWithinFormulaBands(string monsterId)
    {
        var monster = Content.GetMonster(monsterId);
        var baseline = MonsterFormula.Baseline(monster.Level);

        (string Name, double Actual, double Expected)[] stats =
        [
            ("hp", monster.Hp, baseline.Hp),
            ("sp", monster.Sp, baseline.Sp),
            ("at", monster.At, baseline.At),
            ("mat", monster.Mat, baseline.Mat),
            ("df", monster.Df, baseline.Df),
            ("mdf", monster.Mdf, baseline.Mdf),
            ("agi", monster.Agi, baseline.Agi),
            ("au", monster.Au, baseline.Au),
            ("jobExp", monster.JobExp, baseline.JobExp),
        ];
        Assert.All(stats, stat =>
            Assert.True(
                stat.Actual >= stat.Expected * MinimumBand && stat.Actual <= stat.Expected * MaximumBand,
                $"{monsterId} {stat.Name} = {stat.Actual}，基準 {stat.Expected}"));
    }
}
