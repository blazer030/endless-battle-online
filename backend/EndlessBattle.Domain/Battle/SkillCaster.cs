using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Heal 是實際回復量（已扣掉超過 HpMax 的部分）
public sealed record SkillCast(EquippedSkill Equipped, int HpAfter, int SpAfter, int Heal);

// 玩家技能的施放與持續（研究 §3.3）。修正值每次都從裸狀態依欄位順序重套仍生效的技能（快照＋重套），
// 而持續中的技能不會再施放，所以同一技能不會疊加
public sealed class SkillCaster
{
    private readonly CombatantState player;
    private readonly IReadOnlyList<EquippedSkill> skills;
    private readonly Dictionary<EquippedSkill, int> remainingRounds = [];

    // skills 由呼叫端整理好：被動欄只放當前職業的技能
    public SkillCaster(CombatantState player, IReadOnlyList<EquippedSkill> skills)
    {
        this.player = player;
        this.skills = [.. skills.OrderBy(skill => skill.Slot)];
    }

    public BattleModifiers Current { get; private set; } = new();

    // 持續 n 回合含施放回合：第 n + 1 回合開頭失效
    public void ExpireAtRoundStart()
    {
        var expired = false;
        foreach (var skill in remainingRounds.Keys.ToList())
        {
            remainingRounds[skill]--;
            if (remainingRounds[skill] <= 0)
            {
                remainingRounds.Remove(skill);
                expired = true;
            }
        }

        if (expired)
        {
            Recompute();
        }
    }

    // 玩家出手前，依欄位 1→5 嘗試施放
    public IReadOnlyList<SkillCast> CastBeforeAttack(int hp, int sp, IRandomSource random) =>
        Cast(SkillTiming.BeforeAttack, hp, sp, random, equipped => equipped.Skill.Effect == SkillEffectKind.Heal
            ? BattleModifiers.Round(player.Smart * (1 + 0.375 * (equipped.Level - 1)))
            : 0);

    // 玩家物理命中後（嗜血）；施放後本回合持續中，同一回合不會再發動
    public IReadOnlyList<SkillCast> CastAfterPhysicalHit(int damage, int hp, int sp, IRandomSource random) =>
        Cast(SkillTiming.AfterAttack, hp, sp, random, equipped => equipped.Skill.Effect == SkillEffectKind.Bloodthirst
            ? BattleModifiers.Round(damage * 0.02 * equipped.Level)
            : 0);

    private List<SkillCast> Cast(SkillTiming timing, int hp, int sp, IRandomSource random, Func<EquippedSkill, int> healOf)
    {
        var casts = new List<SkillCast>();
        foreach (var equipped in skills.Where(skill => skill.Skill.Timing == timing))
        {
            var level = equipped.Skill.LevelAt(equipped.Level);

            // 持續中、SP 不足或滿血時的回復技能都不擲骰：SP 只在住宿回復，不浪費在回復 0 上；
            // 這些情況耗用的亂數與沒有技能時相同
            if (remainingRounds.ContainsKey(equipped) || sp < level.Sp || (IsHealing(equipped) && hp >= player.HpMax)
                || PhpRandom.Between(random, 1, 100) > equipped.Chance)
            {
                continue;
            }

            sp -= level.Sp;
            var heal = Math.Min(healOf(equipped), player.HpMax - hp);
            hp += heal;
            remainingRounds[equipped] = level.Rounds;
            casts.Add(new SkillCast(equipped, hp, sp, heal));
        }

        if (casts.Count > 0)
        {
            Recompute();
        }

        return casts;
    }

    private static bool IsHealing(EquippedSkill equipped) =>
        equipped.Skill.Effect is SkillEffectKind.Heal or SkillEffectKind.Bloodthirst;

    private void Recompute() =>
        Current = skills.Where(remainingRounds.ContainsKey).Aggregate(new BattleModifiers(), Apply);

    // 技能數值照 plan 的技能表，value = lv
    private BattleModifiers Apply(BattleModifiers modifiers, EquippedSkill equipped)
    {
        var level = equipped.Level;
        return equipped.Skill.Effect switch
        {
            SkillEffectKind.AtMultiplier => modifiers with { At = modifiers.At * (1 + 0.01 * level) },
            SkillEffectKind.MatMultiplier => modifiers with { Mat = modifiers.Mat * (1 + 0.01 * level) },
            SkillEffectKind.DfMultiplier => modifiers with { Df = modifiers.Df * (1 + 0.01 * level) },
            SkillEffectKind.MdfMultiplier => modifiers with { Mdf = modifiers.Mdf * (1 + 0.01 * level) },
            SkillEffectKind.AgiMultiplier => modifiers with { Agi = modifiers.Agi * (1 + 0.01 * level) },
            SkillEffectKind.MultiHit => modifiers with { PhysicalHits = level + 1, At = modifiers.At * (1 - 0.01 * level) },
            SkillEffectKind.MagicMultiHit => modifiers with { MagicHits = level + 1 },
            SkillEffectKind.EnemyAgiDown => modifiers with { EnemyAgi = modifiers.EnemyAgi * (1 - 0.01 * level) },
            SkillEffectKind.EnemyAttackDown => modifiers with { EnemyAttack = modifiers.EnemyAttack * (1 - 0.02 * level) },
            SkillEffectKind.WarGodWrath => modifiers with
            {
                FlatAt = modifiers.FlatAt + BattleModifiers.Round(player.Str * 0.45),
                Rag = modifiers.Rag - 75 + 5 * level,
            },
            SkillEffectKind.Counter => modifiers with
            {
                PhysicalTaken = modifiers.PhysicalTaken * (0.92 - 0.02 * level),
                At = modifiers.At * (1 + 0.01 * level),
            },
            SkillEffectKind.CritChanceUp => modifiers with { CritChanceBonus = modifiers.CritChanceBonus + 2 * level },
            SkillEffectKind.BonusDamage => modifiers with { BonusDamage = modifiers.BonusDamage + BattleModifiers.Round(player.Au * 0.08 * level) },
            SkillEffectKind.Stun => modifiers with { StunChance = 3 * level },
            SkillEffectKind.Heal or SkillEffectKind.Bloodthirst => modifiers,
            _ => throw new ArgumentOutOfRangeException(nameof(equipped), equipped.Skill.Effect, "未知的技能效果"),
        };
    }
}
