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
    int MagicBonusDamage = 0)
{
    // 能力倍率最低 0.9，正值四捨五入後仍至少 1，所以不另設下限；倍率為 1 時數值完全不變
    public CombatantState ApplyToPlayer(CombatantState player) => player with
    {
        At = Scale(player.At, this.At) + FlatAt,
        Mat = Scale(player.Mat, this.Mat) + FlatMat,
        Df = Scale(player.Df, this.Df) + FlatDf,
        Mdf = Scale(player.Mdf, this.Mdf) + FlatMdf,
        Agi = Scale(player.Agi, this.Agi) + FlatAgi,
    };

    public CombatantState ApplyToMonster(CombatantState monster) => monster with
    {
        At = Scale(monster.At, EnemyAt),
        Mat = Scale(monster.Mat, EnemyMat),
        Agi = Scale(monster.Agi, EnemyAgi),
    };

    // 玩家受到的物理傷害；原本有傷害時減免後至少 1
    public int PhysicalDamageTaken(int damage) => damage > 0 ? Math.Max(1, Round(damage * PhysicalTaken)) : damage;

    // 玩家這一下的附加傷害：不分攻擊方式的加上該方式專屬的
    public int BonusDamageFor(AttackKind kind) => BonusDamage + (kind == AttackKind.Physical ? PhysicalBonusDamage : MagicBonusDamage);

    internal static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static int Scale(int value, double multiplier) => Round(value * multiplier);
}
