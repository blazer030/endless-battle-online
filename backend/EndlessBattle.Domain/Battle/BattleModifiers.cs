using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Level 已依技能書與內容上限整理好；主動技能的發動率取 Skill.LevelAt(Level).Chance
public sealed record EquippedSkill(SkillDefinition Skill, int Level, int Slot);

// 生效中技能對這一回合的修正；全部預設值等於沒有技能
public sealed record BattleModifiers(
    double At = 1,
    double Mat = 1,
    double Df = 1,
    double Mdf = 1,
    double Agi = 1,
    int FlatAt = 0,
    int Rag = 0,
    double CritChanceBonus = 0,
    int BonusDamage = 0,
    double PhysicalTaken = 1,
    int PhysicalHits = 1,
    int MagicHits = 1,
    double EnemyAgi = 1,
    int StunChance = 0,
    int FlatMat = 0,
    int FlatDf = 0,
    int FlatMdf = 0,
    int FlatAgi = 0,
    double EnemyAt = 1,
    double EnemyMat = 1,
    int PhysicalBonusDamage = 0,
    int MagicBonusDamage = 0,
    double Au = 1,
    int FlatAu = 0,
    double EnemyDf = 1,
    double EnemyMdf = 1,
    double EnemyAu = 1,
    int EnemyFlatAt = 0,
    int EnemyFlatMat = 0,
    int EnemyFlatDf = 0,
    int EnemyFlatMdf = 0,
    int EnemyFlatAgi = 0,
    int EnemyFlatAu = 0,
    int EnemyRag = 0,
    double MagicCritChanceBonus = 0,
    double PhysicalCritCapBonus = 0,
    double MagicCritCapBonus = 0,
    int EscapeBonus = 0,
    double MagicTaken = 1,
    int? AttackElement = null,
    int? DefenseElement = null,
    double ElementAdvantagePercent = 0)
{
    // 能力倍率最低 0.9，正值四捨五入後仍至少 1，所以不另設下限；倍率為 1 時數值完全不變
    public CombatantState ApplyToCaster(CombatantState caster) => caster with
    {
        Attack = Math.Max(0, Scale(caster.Attack, this.At) + FlatAt),
        MagicAttack = Math.Max(0, Scale(caster.MagicAttack, this.Mat) + FlatMat),
        Defense = Math.Max(0, Scale(caster.Defense, this.Df) + FlatDf),
        MagicDefense = Math.Max(0, Scale(caster.MagicDefense, this.Mdf) + FlatMdf),
        Agility = Math.Max(1, Scale(caster.Agility, this.Agi) + FlatAgi),
        Charisma = Math.Max(1, Scale(caster.Charisma, this.Au) + FlatAu),
    };

    public CombatantState ApplyToOpponent(CombatantState opponent) => opponent with
    {
        Attack = Math.Max(0, Scale(opponent.Attack, EnemyAt) + EnemyFlatAt),
        MagicAttack = Math.Max(0, Scale(opponent.MagicAttack, EnemyMat) + EnemyFlatMat),
        Defense = Math.Max(0, Scale(opponent.Defense, EnemyDf) + EnemyFlatDf),
        MagicDefense = Math.Max(0, Scale(opponent.MagicDefense, EnemyMdf) + EnemyFlatMdf),
        Agility = Math.Max(1, Scale(opponent.Agility, EnemyAgi) + EnemyFlatAgi),
        Charisma = Math.Max(1, Scale(opponent.Charisma, EnemyAu) + EnemyFlatAu),
    };

    // 施放者受到的物理傷害；原本有傷害時減免後至少 1
    public int PhysicalDamageTaken(int damage) => DamageTaken(AttackKind.Physical, damage);

    // 施放者受到的傷害，依攻擊方式套用減傷；原本有傷害時減免後至少 1
    public int DamageTaken(AttackKind kind, int damage) =>
        damage > 0 ? Math.Max(1, Round(damage * (kind == AttackKind.Physical ? PhysicalTaken : MagicTaken))) : damage;

    // 施放者出手時的攻擊屬性、被攻擊時的防禦屬性（魔劍、魔盾）；沒有轉換時用本身的屬性
    public CombatantState AttackingAs(CombatantState caster) => AttackElement is { } element ? caster with { Element = element } : caster;

    public CombatantState DefendingAs(CombatantState caster) => DefenseElement is { } element ? caster with { Element = element } : caster;

    // 施放者這一下的暴擊率加成（百分點）與暴擊倍率上限加成（百分比），依攻擊方式
    public double CriticalChanceFor(AttackKind kind) => kind == AttackKind.Physical ? CritChanceBonus : MagicCritChanceBonus;

    public double CriticalCapFor(AttackKind kind) => kind == AttackKind.Physical ? PhysicalCritCapBonus : MagicCritCapBonus;

    // 施放者這一下的附加傷害：不分攻擊方式的加上該方式專屬的
    public int BonusDamageFor(AttackKind kind) => BonusDamage + (kind == AttackKind.Physical ? PhysicalBonusDamage : MagicBonusDamage);

    internal static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static int Scale(int value, double multiplier) => Round(value * multiplier);
}
