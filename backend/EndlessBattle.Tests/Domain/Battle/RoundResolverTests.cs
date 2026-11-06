using EndlessBattle.Domain.Battle;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

public class RoundResolverTests
{
    // 雙方 agi、au 都是 10 → 物理與魔法的基礎暴擊率都是 10；物暴擊率 +10 只加在物理 → 20。
    // 擲骰：物理先擲命中 0.0；暴擊 0.145 → 15（物理 15 < 20 暴擊、魔法 15 < 10 不暴擊）；傷害 0.0
    [Theory]
    [InlineData(AttackKind.Physical, true)]
    [InlineData(AttackKind.Magic, false)]
    public void Strike_CritChanceBonusAppliesToPhysicalOnly(AttackKind kind, bool expectedCritical)
    {
        var fighter = new CombatantState(100, 100, 0, 0, At: 100, Mat: 100, Df: 50, Mdf: 50, Agi: 10, Au: 10, Element: 0, Str: 100, Smart: 100);
        var rolls = kind == AttackKind.Physical ? new[] { 0.0, 0.145, 0.0 } : [0.145, 0.0];

        // 物理暴擊率技能只算進物理攻擊：依攻擊方式取這一下的暴擊率加成
        var bonus = new BattleModifiers(CritChanceBonus: 10).CriticalChanceFor(kind);
        var result = RoundResolver.Strike(fighter, fighter, kind, new ScriptedRandom(0.99, rolls), criticalChanceBonus: bonus);

        Assert.Equal(expectedCritical, result.Critical);
    }
}
