using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Heal 是實際回復量（已扣掉超過 HpMax 的部分）
public sealed record SkillCast(EquippedSkill Equipped, int HpAfter, int SpAfter, int Heal);

// 玩家技能的施放與持續（研究 §3.3）。
// 沒有發動條件的被動常駐：裝上就生效、不擲骰、不扣 SP、沒有持續回合；攻擊後時機的被動（嗜血）每次命中都觸發。
// 觸發型被動（有發動條件：chance 或 hpBelow）在條件成立時才生效，不扣 SP，效果持續該級 rounds。
// 主動技能依發動條件（擲發動率或 hpBelow）施放，修正值每次都從裸狀態依欄位順序重套常駐的被動與仍生效的主動（快照＋重套），
// 而持續中的技能不會再施放，所以同一技能不會疊加
public sealed class SkillCaster
{
    private readonly CombatantState player;
    private readonly IReadOnlyList<EquippedSkill> skills;
    private readonly Dictionary<EquippedSkill, int> remainingRounds = [];

    // 冷卻中的技能：值是還要經過幾個回合開頭才能再發動。每場戰鬥建立新的 SkillCaster，冷卻自然歸零
    private readonly Dictionary<EquippedSkill, int> cooldowns = [];

    // 新格式持續中的技能已經過幾個回合開頭（發動回合為 0）；每個效果在 age < max(1, rounds) 時生效
    private readonly Dictionary<EquippedSkill, int> ages = [];

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

        var expired = ages.Count > 0;
        foreach (var skill in ages.Keys.ToList())
        {
            ages[skill]++;
        }

        foreach (var skill in remainingRounds.Keys.ToList())
        {
            remainingRounds[skill]--;
            if (remainingRounds[skill] <= 0)
            {
                remainingRounds.Remove(skill);
                ages.Remove(skill);
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
        Cast(SkillTiming.BeforeAttack, hp, sp, random, equipped => equipped.Skill.IsComposed
            ? RestoreAmount(equipped, damage: 0)
            : equipped.Skill.Effect == SkillEffectKind.Heal
                ? BattleModifiers.Round(player.Smart * (1 + 0.375 * (equipped.Level - 1)))
                : 0);

    // 玩家物理命中後（嗜血）；主動技能施放後本回合持續中，同一回合不會再發動
    public IReadOnlyList<SkillCast> CastAfterPhysicalHit(int damage, int hp, int sp, IRandomSource random) =>
        Cast(SkillTiming.AfterAttack, hp, sp, random, equipped => equipped.Skill.IsComposed
            ? RestoreAmount(equipped, damage)
            : equipped.Skill.Effect == SkillEffectKind.Bloodthirst
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
            var spCost = equipped.Skill.IsComposed ? SkillPointCost(level) : isPassive ? 0 : level.Sp;
            if (remainingRounds.ContainsKey(equipped) || !ConditionsWithoutRollHold(equipped, level, hp) || sp < spCost
                || (IsHealing(equipped) && hp >= player.HpMax) || !Triggers(equipped, level, hp, random))
            {
                continue;
            }

            sp -= spCost;

            var heal = Math.Min(healOf(equipped), player.HpMax - hp);
            hp += heal;
            remainingRounds[equipped] = equipped.Skill.IsComposed ? LastingRounds(level) : level.Rounds;
            if (equipped.Skill.IsComposed)
            {
                ages[equipped] = 0;
            }
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

    // 新格式：不需要亂數的條件先判斷，全部成立且消耗夠，才在 Triggers 擲 Chance；舊格式沒有這類條件
    private bool ConditionsWithoutRollHold(EquippedSkill equipped, SkillLevel level, int hp) =>
        !equipped.Skill.IsComposed || level.Conditions!.All(condition => condition.Kind switch
        {
            SkillConditionKind.HitPointsBelow => (long)hp * 100 < (long)condition.Percent * player.HpMax,
            _ => true,
        });

    // hpBelow 看當下 HP 比例、不擲骰；chance 擲該級的固定發動率。新格式依序擲每個 Chance 條件
    private bool Triggers(EquippedSkill equipped, SkillLevel level, int hp, IRandomSource random)
    {
        if (equipped.Skill.IsComposed)
        {
            return level.Conditions!.Where(condition => condition.Kind == SkillConditionKind.Chance)
                .All(condition => PhpRandom.Between(random, 1, 100) <= condition.Percent);
        }

        return equipped.Skill.EffectiveTrigger is { Kind: SkillTriggerKind.HpBelow, Percent: var percent }
            ? (long)hp * 100 < (long)percent * player.HpMax
            : PhpRandom.Between(random, 1, 100) <= level.Chance;
    }

    private static int SkillPointCost(SkillLevel level) =>
        level.Costs!.Where(cost => cost.Kind == SkillCostKind.SkillPoints).Sum(cost => cost.Amount);

    // 持續中不重複發動：直到最長的效果結束；只在發動回合生效的效果也擋住同一回合
    private static int LastingRounds(SkillLevel level) => level.Effects!.Max(effect => Math.Max(1, effect.Rounds));

    private int RestoreAmount(EquippedSkill equipped, int damage) =>
        equipped.Skill.LevelAt(equipped.Level).Effects!.OfType<RestoreEffect>().Sum(effect => effect.Scaling is { } scaling
            ? Scaled(scaling)
            : BattleModifiers.Round(damage * effect.PercentOfDamage!.Value / 100));

    private int Scaled(IReadOnlyList<SkillScaling> scaling) =>
        BattleModifiers.Round(scaling.Sum(term => Basis(term.Basis) * term.Ratio));

    private int Basis(SkillScalingBasis basis) => basis switch
    {
        SkillScalingBasis.Strength => player.Str,
        SkillScalingBasis.Intelligence => player.Smart,
        SkillScalingBasis.Agility => player.Agi,
        SkillScalingBasis.Charisma => player.Au,
        SkillScalingBasis.Attack => player.At,
        SkillScalingBasis.MagicAttack => player.Mat,
        SkillScalingBasis.Defense => player.Df,
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "未知的能力基準"),
    };

    // 冷卻 n：接下來 n 次玩家出手不能發動。倒數在每回合開頭，下一回合開頭就會先扣 1，所以存 n + 1
    private void StartCooldown(EquippedSkill equipped, SkillLevel level)
    {
        if (level.Cooldown > 0)
        {
            cooldowns[equipped] = level.Cooldown + 1;
        }
    }

    // 效果全部是回復的技能，在要回復的資源已滿時不發動
    private static bool IsHealing(EquippedSkill equipped) =>
        equipped.Skill.IsComposed
            ? equipped.Skill.LevelAt(equipped.Level).Effects!.All(effect => effect is RestoreEffect)
            : equipped.Skill.Effect is SkillEffectKind.Heal or SkillEffectKind.Bloodthirst;

    private bool IsInEffect(EquippedSkill equipped) =>
        equipped.Skill.IsAlwaysOn || remainingRounds.ContainsKey(equipped);

    private void Recompute() =>
        Current = skills.Where(IsInEffect).Aggregate(new BattleModifiers(), Apply);

    // 技能數值照 plan 的技能表，value = lv
    private BattleModifiers Apply(BattleModifiers modifiers, EquippedSkill equipped)
    {
        if (equipped.Skill.IsComposed)
        {
            return ApplyComposed(modifiers, equipped);
        }

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

    // 同一技能裡同一項能力的百分比相加後才乘進修正值；不同技能之間相乘；固定值與 scaling 一律相加
    private BattleModifiers ApplyComposed(BattleModifiers modifiers, EquippedSkill equipped)
    {
        var age = ages.GetValueOrDefault(equipped);
        var effects = equipped.Skill.LevelAt(equipped.Level).Effects!
            .Where(effect => equipped.Skill.IsAlwaysOn || age < Math.Max(1, effect.Rounds))
            .ToList();
        var percents = new Dictionary<(SkillTarget, SkillStat), double>();
        var flats = new Dictionary<SkillStat, int>();
        var physicalReduction = 0.0;
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case StatChangeEffect { Percent: { } percent } change:
                    percents[(change.Target, change.Stat)] = percents.GetValueOrDefault((change.Target, change.Stat)) + percent;
                    break;
                case StatChangeEffect change:
                    flats[change.Stat] = flats.GetValueOrDefault(change.Stat) + (change.Amount ?? Scaled(change.Scaling!));
                    break;
                case BonusDamageEffect bonus:
                    var amount = Scaled(bonus.Scaling);
                    modifiers = bonus.AttackKind switch
                    {
                        SkillAttackKind.Physical => modifiers with { PhysicalBonusDamage = modifiers.PhysicalBonusDamage + amount },
                        SkillAttackKind.Magic => modifiers with { MagicBonusDamage = modifiers.MagicBonusDamage + amount },
                        _ => modifiers with { BonusDamage = modifiers.BonusDamage + amount },
                    };
                    break;
                case MultiHitEffect multiHit:
                    modifiers = modifiers with
                    {
                        PhysicalHits = multiHit.AttackKind == SkillAttackKind.Magic ? modifiers.PhysicalHits : Math.Max(modifiers.PhysicalHits, multiHit.Hits),
                        MagicHits = multiHit.AttackKind == SkillAttackKind.Physical ? modifiers.MagicHits : Math.Max(modifiers.MagicHits, multiHit.Hits),
                    };
                    break;
                case DamageReductionEffect reduction:
                    physicalReduction += reduction.Percent;
                    break;
                case StunEffect stun:
                    modifiers = modifiers with { StunChance = Math.Max(modifiers.StunChance, stun.Chance) };
                    break;
            }
        }

        foreach (var ((target, stat), percent) in percents)
        {
            var multiplier = 1 + percent / 100;
            modifiers = (target, stat) switch
            {
                (SkillTarget.Caster, SkillStat.Attack) => modifiers with { At = modifiers.At * multiplier },
                (SkillTarget.Caster, SkillStat.MagicAttack) => modifiers with { Mat = modifiers.Mat * multiplier },
                (SkillTarget.Caster, SkillStat.Defense) => modifiers with { Df = modifiers.Df * multiplier },
                (SkillTarget.Caster, SkillStat.MagicDefense) => modifiers with { Mdf = modifiers.Mdf * multiplier },
                (SkillTarget.Caster, SkillStat.Agility) => modifiers with { Agi = modifiers.Agi * multiplier },
                (SkillTarget.Opponent, SkillStat.Attack) => modifiers with { EnemyAt = modifiers.EnemyAt * multiplier },
                (SkillTarget.Opponent, SkillStat.MagicAttack) => modifiers with { EnemyMat = modifiers.EnemyMat * multiplier },
                (SkillTarget.Opponent, SkillStat.Agility) => modifiers with { EnemyAgi = modifiers.EnemyAgi * multiplier },
                _ => throw new ArgumentOutOfRangeException(nameof(equipped), (target, stat), "這項能力不能用百分比增減"),
            };
        }

        foreach (var (stat, flat) in flats)
        {
            modifiers = stat switch
            {
                SkillStat.Attack => modifiers with { FlatAt = modifiers.FlatAt + flat },
                SkillStat.MagicAttack => modifiers with { FlatMat = modifiers.FlatMat + flat },
                SkillStat.Defense => modifiers with { FlatDf = modifiers.FlatDf + flat },
                SkillStat.MagicDefense => modifiers with { FlatMdf = modifiers.FlatMdf + flat },
                SkillStat.Agility => modifiers with { FlatAgi = modifiers.FlatAgi + flat },
                SkillStat.HitRate => modifiers with { Rag = modifiers.Rag + flat },
                SkillStat.PhysicalCriticalChance => modifiers with { CritChanceBonus = modifiers.CritChanceBonus + flat },
                _ => throw new ArgumentOutOfRangeException(nameof(equipped), stat, "未知的能力"),
            };
        }

        return physicalReduction == 0 ? modifiers : modifiers with { PhysicalTaken = modifiers.PhysicalTaken * (1 - physicalReduction / 100) };
    }
}
