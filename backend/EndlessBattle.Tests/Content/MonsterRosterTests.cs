using EndlessBattle.Domain.Content;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 匯入的原版怪物：每個地區、子地區都遇得到怪；任務怪不算在一般怪裡
public class MonsterRosterTests
{
    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static IEnumerable<MonsterDefinition> RegularMonsters => Content.AllMonsters.Where(monster => monster.MissionId is null);

    [Fact]
    public void Roster_CoversEveryPlace()
    {
        Assert.Equal(337, RegularMonsters.Count());
        Assert.All(Content.AllMonsters, monster => Assert.False(CustomContent.IsCustom(monster.Id)));

        foreach (var place in Content.AllPlaces)
        {
            var monsters = RegularMonsters.Where(monster => monster.PlaceId == place.Id).ToList();
            Assert.NotEmpty(monsters);
            Assert.All(monsters, monster =>
            {
                if (place.HasSubPlaces)
                {
                    Assert.NotEmpty(monster.SubPlaces);
                }
                else
                {
                    Assert.Empty(monster.SubPlaces);
                }
            });
        }
    }

    [Fact]
    public void Roster_DropsReferenceKnownItems()
    {
        Assert.All(Content.AllMonsters, monster =>
        {
            Assert.Equal(monster.Drops.Count, monster.Drops.Select(drop => drop.ItemId).Distinct().Count());
            Assert.All(monster.Drops, drop =>
            {
                Assert.Contains(Content.AllItems, item => item.Id == drop.ItemId);
                Assert.True(drop.Topr >= 1, $"{monster.Id} 的掉落 {drop.ItemId} topr {drop.Topr}");
            });
        });
    }

    // 原版只有一隻全域怪：不死小白
    [Fact]
    public void Roster_HasOneGlobalMonster()
    {
        var global = Assert.Single(Content.AllMonsters, monster => monster.PlaceId == 0);

        Assert.Equal(("monster-192", "不死小白", 80), (global.Id, global.Name, global.Meet));
        Assert.Empty(global.SubPlaces);
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
}
