using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.World;

public class AreaKeysTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static PlaceDefinition Place(int id, bool restricted) =>
        new(id, $"地區{id}", "area", [], new LevelRange(1, 10), restricted);

    private static ItemDefinition Key(string id, int placeId) =>
        TestItems.Item(id, ItemCategory.Key) with { Key = new AreaKeySpec(placeId, 3600) };

    // 別的地區的鑰匙與不在內容裡的道具都略過；同地區有兩格時取 Id 最小的那格
    [Fact]
    public void FindKey_TakesLowestIdStackForThatPlace()
    {
        var items = new Dictionary<string, ItemDefinition>
        {
            ["cave-key"] = Key("cave-key", 17),
            ["fort-key"] = Key("fort-key", 18),
            ["ore"] = TestItems.Item("ore", ItemCategory.Material),
        };
        IReadOnlyList<InventoryEntry> bag =
        [
            TestItems.Entry(2, "fort-key", 1),
            TestItems.Entry(3, "ghost", 1),
            TestItems.Entry(4, "ore", 1),
            TestItems.Entry(5, "cave-key", 99),
            TestItems.Entry(6, "cave-key", 3),
        ];

        var found = AreaKeys.FindKey(bag.Reverse().ToList(), 17, items.GetValueOrDefault);

        Assert.Equal(5, found?.Id);
        Assert.Null(AreaKeys.FindKey(bag, 19, items.GetValueOrDefault));
    }

    [Fact]
    public void SimulateUntil_CapsAtExpiry()
    {
        var place = Place(17, restricted: true);

        Assert.Equal(Now.AddSeconds(-5), AreaKeys.SimulateUntil(place, new AreaPass(17, Now.AddSeconds(-5)), Now));
        Assert.Equal(Now, AreaKeys.SimulateUntil(place, new AreaPass(17, Now.AddSeconds(5)), Now));
        Assert.Equal(DateTimeOffset.MinValue, AreaKeys.SimulateUntil(place, null, Now));
        Assert.Equal(new AreaPass(17, Now.AddSeconds(3600)), AreaKeys.Enter(new AreaKeySpec(17, 3600), Now));
    }

    [Fact]
    public void SimulateUntil_NormalPlaceIsNow()
    {
        Assert.Equal(Now, AreaKeys.SimulateUntil(Place(1, restricted: false), null, Now));
        Assert.Equal(Now, AreaKeys.SimulateUntil(Place(1, restricted: false), new AreaPass(1, Now.AddSeconds(-5)), Now));
    }
}
