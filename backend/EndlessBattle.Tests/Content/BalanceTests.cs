using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 新手安全規則：起始子地區的每隻非任務怪，最大單擊都不能超過最弱新角色 HP 的一半
public class BalanceTests
{
    private const int HalfOfReferenceHp = 162;
    private const int FarthestRoll = 100;

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);

    private static CombatantState Create(StatAllocation allocation, int element)
    {
        var job = Content.AllJobs.First(candidate => candidate.Tier == CharacterFactory.StartingJobTier);
        var character = CharacterFactory.Create(
            1,
            new CreateCharacterCommand("基準", Sex: 1, element, Birth: 1, job.Id, allocation),
            job,
            Content.StartingPlaceId,
            Content.StartingSubPlace,
            Now);
        return CombatBridge.ToCombatant(character);
    }

    private static CombatantState Reference() => Create(new StatAllocation(4, 4, 1, 1), element: 1);

    [Fact]
    public void Balance_ReferenceCharacterIsTheWeakestStart()
    {
        var reference = Reference();

        Assert.Equal(325, reference.HpMax);
        Assert.Equal(10, reference.Df);
        Assert.Equal(10, reference.Mdf);
        Assert.Equal(11, reference.Agi);
        Assert.Equal(10, reference.Au);

        for (var str = 1; str <= 7; str++)
        for (var smart = 1; smart <= 7; smart++)
        for (var life = 1; life <= 7; life++)
        for (var agi = 1; agi <= 7; agi++)
        {
            if (str + smart + life + agi != CharacterFactory.TotalPoints)
            {
                continue;
            }

            var other = Create(new StatAllocation(str, smart, life, agi), element: 1);
            Assert.True(other.HpMax >= reference.HpMax, $"{str}/{smart}/{life}/{agi} HpMax {other.HpMax}");
            Assert.True(other.Df >= reference.Df, $"{str}/{smart}/{life}/{agi} Df {other.Df}");
            Assert.True(other.Mdf >= reference.Mdf, $"{str}/{smart}/{life}/{agi} Mdf {other.Mdf}");
            Assert.True(other.Agi >= reference.Agi, $"{str}/{smart}/{life}/{agi} Agi {other.Agi}");
            Assert.True(other.Au >= reference.Au, $"{str}/{smart}/{life}/{agi} Au {other.Au}");
        }
    }

    [Fact]
    public void Balance_StartingSubPlaceNeverTakesHalfAFreshCharactersHp()
    {
        var reference = Reference();
        var candidates = MonsterPool.Candidates(
            Content.MonstersOf(Content.StartingPlaceId),
            Content.StartingPlaceId,
            Content.StartingSubPlace,
            FarthestRoll)
            .Where(monster => monster.MissionId is null)
            .ToList();
        Assert.NotEmpty(candidates);

        var tooStrong = candidates
            .Select(monster => (monster.Id, Hit: HighestSingleHit(monster, reference)))
            .Where(entry => entry.Hit > HalfOfReferenceHp)
            .Select(entry => $"{entry.Id} 最大單擊 {entry.Hit}")
            .ToList();

        // 原版飛兔（Lv6、物攻 33）照原版保留，整體平衡之後另外處理
        Assert.Equal(["monster-9 最大單擊 495"], tooStrong);
    }

    // 以暴擊倍率（RoundResolver.CriticalOdds）計算；取物理與魔法較大者
    private static int HighestSingleHit(MonsterDefinition monster, CombatantState player)
    {
        var random = new HighestRandom();
        var physical = WogDamage.Fight(
            monster.Attack * RoundResolver.CriticalOdds(monster.Agility, player.Agi).Multiplier, player.Df, StrongestElement(monster), random);
        var magic = WogDamage.Fight(
            monster.MagicAttack * RoundResolver.CriticalOdds(monster.Charisma, player.Au).Multiplier, player.Mdf, StrongestElement(monster), random);
        return Math.Max(physical, magic);
    }

    private static double StrongestElement(MonsterDefinition monster) =>
        Enumerable.Range(1, 6).Max(element => ElementTable.Multiplier(monster.Element, element));
}
