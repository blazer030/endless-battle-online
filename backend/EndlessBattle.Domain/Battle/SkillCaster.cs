using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Heal 是實際回復量（已扣掉超過 HpMax 的部分）
public sealed record SkillCast(EquippedSkill Equipped, int HpAfter, int SpAfter, int Heal);

// 玩家技能的施放與持續（研究 §3.3）。
// 沒有發動條件的被動常駐：裝上就生效、不擲骰、不扣 SP、沒有持續回合；攻擊後時機的被動（嗜血）每次命中都觸發。
// 掛了發動條件（hpBelow）的被動在條件成立時才生效，不扣 SP。主動技能依發動條件（擲發動率或 hpBelow）施放，修正值每次都從裸狀態依欄位順序重套常駐的被動與仍生效的主動（快照＋重套），
// 而持續中的技能不會再施放，所以同一技能不會疊加
public sealed class SkillCaster
{
    private readonly CombatantState player;
    private readonly IReadOnlyList<EquippedSkill> skills;
    private readonly Dictionary<EquippedSkill, int> remainingRounds = [];

    // 冷卻中的技能：值是還要經過幾個回合開頭才能再發動。每場戰鬥建立新的 SkillCaster，冷卻自然歸零
    private readonly Dictionary<EquippedSkill, int> cooldowns = [];

    public SkillCaster(CombatantState player, IReadOnlyList<EquippedSkill> skills)
    {
        this.player = player;
        this.skills = [.. skills.OrderBy(skill => skill.Slot)];
        Recompute();
    }

    public BattleModifiers Current { get; private set; } = new();

    // 持續 n 回合含施放回合：第 n + 1 回合開頭失效。玩家每回合出手一次，所以冷卻也在回合開頭倒數
    public void ExpireAtRoundStart()
    {
        foreach (var skill in cooldowns.Keys.ToList())
        {
            if (--cooldowns[skill] <= 0)
            {
                cooldowns.Remove(skill);
            }
        }

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

    // 玩家物理命中後（嗜血）；主動技能施放後本回合持續中，同一回合不會再發動
    public IReadOnlyList<SkillCast> CastAfterPhysicalHit(int damage, int hp, int sp, IRandomSource random) =>
        Cast(SkillTiming.AfterAttack, hp, sp, random, equipped => equipped.Skill.Effect == SkillEffectKind.Bloodthirst
            ? BattleModifiers.Round(damage * 0.02 * equipped.Level)
            : 0);

    private List<SkillCast> Cast(SkillTiming timing, int hp, int sp, IRandomSource random, Func<EquippedSkill, int> healOf)
    {
        var casts = new List<SkillCast>();
        var recompute = false;
        foreach (var equipped in skills.Where(skill => skill.Skill.Timing == timing))
        {
            var level = equipped.Skill.LevelAt(equipped.Level);

            // 冷卻中不擲骰也不觸發
            if (cooldowns.ContainsKey(equipped))
            {
                continue;
            }

            if (equipped.Skill.IsAlwaysOn)
            {
                // 被動不擲骰、不扣 SP；只有實際回復時才留下紀錄（滿血時沒有事件、耗用的亂數也和沒有技能時相同）
                var passiveHeal = Math.Min(healOf(equipped), player.HpMax - hp);
                if (passiveHeal > 0)
                {
                    hp += passiveHeal;
                    StartCooldown(equipped, level);
                    casts.Add(new SkillCast(equipped, hp, sp, passiveHeal));
                }

                continue;
            }

            // 持續中、SP 不足（被動不耗 SP，不檢查）或滿血時的回復技能都不判斷發動：SP 只在住宿回復，不浪費在回復 0 上；
            // 這些情況耗用的亂數與沒有技能時相同
            var isPassive = equipped.Skill.Type == SkillType.Passive;
            if (remainingRounds.ContainsKey(equipped) || (!isPassive && sp < level.Sp) || (IsHealing(equipped) && hp >= player.HpMax)
                || !Triggers(equipped, level, hp, random))
            {
                continue;
            }

            if (!isPassive)
            {
                sp -= level.Sp;
            }

            var heal = Math.Min(healOf(equipped), player.HpMax - hp);
            hp += heal;
            // 被動的 Rounds 為 0：觸發後只在本回合生效
            remainingRounds[equipped] = Math.Max(level.Rounds, 1);
            StartCooldown(equipped, level);
            recompute = true;
            casts.Add(new SkillCast(equipped, hp, sp, heal));
        }

        if (recompute)
        {
            Recompute();
        }

        return casts;
    }

    // hpBelow 看當下 HP 比例、不擲骰；chance 擲該級的固定發動率
    private bool Triggers(EquippedSkill equipped, SkillLevel level, int hp, IRandomSource random) =>
        equipped.Skill.EffectiveTrigger is { Kind: SkillTriggerKind.HpBelow, Percent: var percent }
            ? (long)hp * 100 < (long)percent * player.HpMax
            : PhpRandom.Between(random, 1, 100) <= level.Chance;

    // 冷卻 n：接下來 n 次玩家出手不能發動。倒數在每回合開頭，下一回合開頭就會先扣 1，所以存 n + 1
    private void StartCooldown(EquippedSkill equipped, SkillLevel level)
    {
        if (level.Cooldown > 0)
        {
            cooldowns[equipped] = level.Cooldown + 1;
        }
    }

    private static bool IsHealing(EquippedSkill equipped) =>
        equipped.Skill.Effect is SkillEffectKind.Heal or SkillEffectKind.Bloodthirst;

    private bool IsInEffect(EquippedSkill equipped) =>
        equipped.Skill.IsAlwaysOn || remainingRounds.ContainsKey(equipped);

    private void Recompute() =>
        Current = skills.Where(IsInEffect).Aggregate(new BattleModifiers(), Apply);

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
