using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.World;

// rand(0,100) = NextInt(101)：擲 0.5 → ⌊50.5⌋ = 50；擲 0.0 → 0；擲 0.999 → ⌊100.9⌋ = 100
public class MonsterPoolTests
{
    private static MonsterDefinition Monster(string id, int meet, int placeId, params int[] subPlaces) =>
        new(id, id, Level: 1, Hp: 10, Sp: 1, At: 1, Mat: 0, Df: 1, Mdf: 1, Agi: 1, Au: 1, Element: 0, JobExp: 1, meet, placeId, subPlaces);

    private static string[] Ids(IEnumerable<MonsterDefinition> monsters) => monsters.Select(monster => monster.Id).ToArray();

    // 地區 3 子地區 2：a（子地區 1,2）、b（只有 2）、c（只有 1）、d（子地區 2 但 meet 60 > 50）
    [Fact]
    public void Pool_FiltersBySubPlaceAndMeet()
    {
        MonsterDefinition[] monsters =
        [
            Monster("a", 0, 3, 1, 2), Monster("b", 50, 3, 2), Monster("c", 0, 3, 1), Monster("d", 60, 3, 2),
        ];

        Assert.Equal(["a", "b"], Ids(MonsterPool.Candidates(monsters, 3, 2, roll: 50)));
    }

    [Fact]
    public void Pool_IgnoresSubPlace_WhenNull()
    {
        MonsterDefinition[] monsters = [Monster("a", 0, 3, 1), Monster("b", 0, 3, 2)];

        Assert.Equal(["a", "b"], Ids(MonsterPool.Candidates(monsters, 3, null, roll: 0)));
    }

    // 全域怪（placeId 0）不受子地區限制；其他地區的怪不入選
    [Fact]
    public void Pool_IncludesGlobalMonsters()
    {
        MonsterDefinition[] monsters = [Monster("local", 0, 3, 1), Monster("global", 0, 0), Monster("other", 0, 4, 2)];

        Assert.Equal(["global"], Ids(MonsterPool.Candidates(monsters, 3, 2, roll: 0)));
    }

    // 子地區 4 沒有任何怪 → 退回該地區全部（含全域），連 meet 也不看（meet 90 > 擲 0）
    [Fact]
    public void Pool_FallsBackToPlace_WhenSubPlaceHasNoCandidates()
    {
        MonsterDefinition[] monsters = [Monster("a", 0, 3, 1), Monster("b", 90, 3, 2), Monster("g", 90, 0)];

        Assert.Equal(["a", "b", "g"], Ids(MonsterPool.Candidates(monsters, 3, 4, roll: 0)));
    }

    // 擲 0 → 候選 a、b、c；第二擲 0.5 → ⌊0.5 × 3⌋ = 1 → b
    [Fact]
    public void Pool_UniformAmongCandidates()
    {
        MonsterDefinition[] monsters = [Monster("a", 0, 3, 1), Monster("b", 0, 3, 1), Monster("c", 0, 3, 1)];
        var random = new ScriptedRandom(0.0, 0.0, 0.5);

        Assert.Equal("b", MonsterPool.Pick(monsters, 3, 1, random).Id);
        Assert.Equal(2, random.Consumed);
    }

    private static MonsterDefinition MissionMonster(string id, string missionId, int meet, int placeId, params int[] subPlaces) =>
        Monster(id, meet, placeId, subPlaces) with { MissionId = missionId };

    // 任務怪沒帶任務集合（或集合為空）時一律不入選，退回全地區的分支也一樣（chief meet 0 本來會入選）
    [Fact]
    public void Pool_ExcludesMissionMonstersByDefault()
    {
        MonsterDefinition[] monsters = [Monster("a", 0, 3, 1), MissionMonster("chief", "hunt", 0, 3, 1)];
        MonsterDefinition[] onlyRare = [Monster("a", 90, 3, 1), MissionMonster("chief", "hunt", 0, 3, 1)];

        Assert.Equal(["a"], Ids(MonsterPool.Candidates(monsters, 3, 1, roll: 0)));
        Assert.Equal(["a"], Ids(MonsterPool.Candidates(monsters, 3, 1, roll: 0, new HashSet<string>())));
        Assert.Equal(["a"], Ids(MonsterPool.Candidates(onlyRare, 3, 1, roll: 0)));
        Assert.Equal(["a"], Ids(MonsterPool.Candidates(monsters, 3, 4, roll: 0)));
    }

    [Fact]
    public void Pool_IncludesMissionMonsterWhenItsMissionIsActive()
    {
        MonsterDefinition[] monsters =
        [
            Monster("a", 0, 3, 1), MissionMonster("chief", "hunt", 0, 3, 1), MissionMonster("other", "elsewhere", 0, 3, 1),
        ];
        var active = new HashSet<string> { "hunt" };

        Assert.Equal(["a", "chief"], Ids(MonsterPool.Candidates(monsters, 3, 1, roll: 0, active)));
        Assert.Equal(["a", "chief"], Ids(MonsterPool.Candidates(monsters, 3, 4, roll: 0, active)));
        // 擲 0 → 候選 a、chief；第二擲 0.5 → ⌊0.5 × 2⌋ = 1 → chief
        Assert.Equal("chief", MonsterPool.Pick(monsters, 3, 1, new ScriptedRandom(0.0, 0.0, 0.5), active).Id);
    }

    [Fact]
    public void Pool_ThrowsOnEmptyPlace() =>
        Assert.Throws<InvalidOperationException>(() => MonsterPool.Pick([], 3, 1, new ScriptedRandom(0.0)));
}
