using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.World;

// 角色位於地區 1・子地區 1
public class PlaceMoveTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static PlaceDefinition Place(int id, int subPlaceCount, bool restricted) => new(
        id,
        $"地區{id}",
        "area",
        Enumerable.Range(1, subPlaceCount).Select(index => new SubPlaceDefinition(index, $"子地區{index}")).ToList(),
        new LevelRange(1, 10),
        restricted);

    private static IReadOnlyList<string> Evaluate(
        PlaceDefinition target, int? subPlace, AreaPass? pass = null, bool hasKey = false, int placeId = 1) =>
        PlaceMove.Evaluate(TestCharacters.Default(1, "測試者", placeId, 1, Now), target, subPlace, pass, hasKey, Now);

    // 禁區且沒有子地區（如黑曜神廟）卻帶子地區：兩個原因都列出，restricted 在前
    [Fact]
    public void Validate_ReportsReasonsInOrder()
    {
        Assert.Equal(["restricted", "subPlaceNotAllowed"], Evaluate(Place(20, 0, restricted: true), 1));
    }

    // samePlace 要地區與子地區都相同；目前在子地區 1，未給子地區 ≠ 子地區 1，所以只有 subPlaceRequired
    [Fact]
    public void Validate_ReportsOnlySubPlaceRequired_WhenSamePlaceWithoutSubPlace()
    {
        Assert.Equal(["subPlaceRequired"], Evaluate(Place(1, 4, restricted: false), null));
    }

    // 超出範圍的子地區必然不等於目前（合法）的子地區，所以不會同時是 samePlace
    [Fact]
    public void Validate_ReportsOutOfRange_WithoutSamePlace()
    {
        Assert.Equal(["subPlaceOutOfRange"], Evaluate(Place(1, 4, restricted: false), 5));
    }

    [Fact]
    public void Evaluate_RestrictedWithoutPassOrKey()
    {
        Assert.Equal(["restricted"], Evaluate(Place(17, 4, restricted: true), 1));
    }

    [Fact]
    public void Evaluate_RestrictedWithKeyAllowed()
    {
        Assert.Empty(Evaluate(Place(17, 4, restricted: true), 1, hasKey: true));
    }

    [Fact]
    public void Evaluate_RestrictedWithValidPassAllowed()
    {
        Assert.Empty(Evaluate(Place(17, 4, restricted: true), 1, new AreaPass(17, Now.AddSeconds(1))));
    }

    // 通行證在 now 到期即失效：同一子地區也能用鑰匙再次進入
    [Fact]
    public void Evaluate_SameRestrictedPlaceAfterExpiryIsNotSamePlace()
    {
        Assert.Empty(Evaluate(Place(17, 4, restricted: true), 1, new AreaPass(17, Now), hasKey: true, placeId: 17));
    }

    [Fact]
    public void Evaluate_SameRestrictedPlaceWithValidPassIsSamePlace()
    {
        Assert.Equal(
            ["samePlace"],
            Evaluate(Place(17, 4, restricted: true), 1, new AreaPass(17, Now.AddSeconds(1)), hasKey: true, placeId: 17));
    }
}
