namespace EndlessBattle.Tests.Support;

// 測試用的技能樣板：名稱沿用舊格式的效果，TestSkills 換成數值等價的條件、消耗與效果
public enum SkillPreset
{
    AtMultiplier,
    MatMultiplier,
    DfMultiplier,
    MdfMultiplier,
    AgiMultiplier,
    MultiHit,
    MagicMultiHit,
    EnemyAgiDown,
    EnemyAttackDown,
    Heal,
    WarGodWrath,
    Counter,
    CritChanceUp,
    Bloodthirst,
    BonusDamage,
    Stun,
}

// 測試用的發動條件：Chance 擲 30 + 10·lv；HpBelow 在 HP 低於 Percent% 時發動
public sealed record TestTrigger(bool IsHpBelow, int Percent = 0)
{
    public static readonly TestTrigger Chance = new(IsHpBelow: false);

    public static TestTrigger HpBelow(int percent) => new(IsHpBelow: true, percent);
}
