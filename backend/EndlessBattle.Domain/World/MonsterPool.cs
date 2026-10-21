using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.World;

public static class MonsterPool
{
    public const int GlobalPlaceId = 0;

    // WOG：擲一次 rand(0,100)，遇敵率 Meet 不高於它、且屬於該地區（子地區）或全域的怪物入選；
    // 沒有候選時退回該地區全部怪物（含全域），不看 Meet 與子地區
    public static IReadOnlyList<MonsterDefinition> Candidates(
        IReadOnlyList<MonsterDefinition> placeMonsters, int placeId, int? subPlace, int roll)
    {
        var candidates = placeMonsters
            .Where(monster => monster.Meet <= roll && InSubPlace(monster, placeId, subPlace))
            .ToList();
        return candidates.Count > 0 ? candidates : placeMonsters.Where(monster => InPlace(monster, placeId)).ToList();
    }

    public static MonsterDefinition Pick(
        IReadOnlyList<MonsterDefinition> placeMonsters, int placeId, int? subPlace, IRandomSource random)
    {
        if (placeMonsters.Count == 0)
        {
            throw new InvalidOperationException("地區沒有任何怪物");
        }

        var candidates = Candidates(placeMonsters, placeId, subPlace, PhpRandom.Between(random, 0, 100));
        return candidates[random.NextInt(candidates.Count)];
    }

    private static bool InPlace(MonsterDefinition monster, int placeId) =>
        monster.PlaceId == GlobalPlaceId || monster.PlaceId == placeId;

    private static bool InSubPlace(MonsterDefinition monster, int placeId, int? subPlace) =>
        monster.PlaceId == GlobalPlaceId
        || (monster.PlaceId == placeId && (subPlace is null || monster.SubPlaces.Contains(subPlace.Value)));
}
