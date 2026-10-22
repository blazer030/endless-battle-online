using System.Text.RegularExpressions;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content/monsters.json：plan 6 Global Constraints 的怪物總表規則；
// 任務怪（plan 8）不算在每地區 3 隻、精英數與子地區覆蓋內，但同樣要落在公式範圍內
public partial class MonsterRosterTests
{
    private const int MonstersPerPlace = 3;
    private const int EliteMeet = 60;
    private const double MinimumBand = 0.5;
    private const double MaximumBand = 1.6;

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    public static TheoryData<string> MonsterIds => new(Content.AllMonsters.Select(monster => monster.Id));

    private static IEnumerable<MonsterDefinition> RegularMonsters => Content.AllMonsters.Where(monster => monster.MissionId is null);

    [GeneratedRegex("^[a-z]+(-[a-z]+)*$")]
    private static partial Regex KebabCase();

    [Fact]
    public void Roster_CoversEveryPlaceWithThreeMonsters()
    {
        Assert.Equal(65 + 3, Content.AllMonsters.Count);
        Assert.Equal(65, RegularMonsters.Count());
        Assert.All(Content.AllMonsters, monster => Assert.Matches(KebabCase(), monster.Id));

        foreach (var place in Content.AllPlaces)
        {
            var monsters = RegularMonsters.Where(monster => monster.PlaceId == place.Id).ToList();
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

    // 每隻怪 1～3 個掉落、topr 3～200，不重複；全域怪有明確的指定（絨球小補給 topr 3、寶箱哥布林材料 topr 2 + 精鍊石 topr 50）
    [Fact]
    public void Roster_DropsReferenceKnownItems()
    {
        Assert.All(Content.AllMonsters, monster =>
        {
            Assert.InRange(monster.Drops.Count, 1, 3);
            Assert.Equal(monster.Drops.Count, monster.Drops.Select(drop => drop.ItemId).Distinct().Count());
            Assert.All(monster.Drops, drop =>
            {
                Assert.Contains(Content.AllItems, item => item.Id == drop.ItemId);
                var minimum = monster.PlaceId == 0 ? 2 : 3;
                Assert.InRange(drop.Topr, minimum, 200);
            });
        });

        Assert.Equal([new DropEntry("small-supply", 3)], Content.GetMonster("wandering-puff").Drops);
        var goblin = Content.GetMonster("treasure-goblin").Drops;
        Assert.Equal(2, goblin.Count);
        Assert.Equal(2, goblin[0].Topr);
        Assert.Equal(ItemCategory.Material, Content.GetItem(goblin[0].ItemId).Category);
        Assert.Equal(50, goblin[1].Topr);
        Assert.Equal(ItemCategory.RefineStone, Content.GetItem(goblin[1].ItemId).Category);
    }

    [Fact]
    public void Roster_HasTwoGlobalMonsters()
    {
        var globals = Content.AllMonsters.Where(monster => monster.PlaceId == 0).ToList();

        Assert.Equal([1, 5], globals.Select(monster => monster.Level).Order());
        Assert.All(globals, monster =>
        {
            Assert.Equal(95, monster.Meet);
            Assert.Empty(monster.SubPlaces);
        });
    }

    [Fact]
    public void Roster_EverySubPlaceHasAMonster()
    {
        foreach (var place in Content.AllPlaces.Where(place => place.HasSubPlaces))
        {
            var covered = RegularMonsters
                .Where(monster => monster.PlaceId == place.Id)
                .SelectMany(monster => monster.SubPlaces)
                .ToHashSet();
            Assert.All(place.SubPlaces, subPlace => Assert.Contains(subPlace.Index, covered));
        }
    }

    // meet 0 的怪每次擲骰都是候選，確保每個子地區（無子地區時為地區本身）不會只剩稀有怪
    [Fact]
    public void Roster_EverySubPlaceHasAnAlwaysMeetMonster()
    {
        foreach (var place in Content.AllPlaces)
        {
            var alwaysMeet = RegularMonsters
                .Where(monster => monster.PlaceId == place.Id && monster.Meet == 0)
                .ToList();
            if (!place.HasSubPlaces)
            {
                Assert.NotEmpty(alwaysMeet);
                continue;
            }

            var covered = alwaysMeet.SelectMany(monster => monster.SubPlaces).ToHashSet();
            Assert.All(place.SubPlaces, subPlace =>
                Assert.True(covered.Contains(subPlace.Index), $"地區 {place.Id} 子地區 {subPlace.Index} 沒有 meet 0 的怪"));
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
