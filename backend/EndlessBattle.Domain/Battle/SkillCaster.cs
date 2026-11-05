using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Heal 是實際回復量（已扣掉超過 HpMax 的部分）
public sealed record SkillCast(EquippedSkill Equipped, int HpAfter, int SpAfter, int Heal);

// 玩家技能的施放與持續（研究 §3.3）。主動與被動規則相同，只看每級的條件、消耗與效果：
// 沒有發動條件的技能常駐：裝上就生效、不擲骰、不扣消耗；攻擊後時機的常駐技能（嗜血）每次命中都觸發。
// 有條件的技能在條件全部成立、消耗夠時發動，扣消耗，效果持續各自的 rounds；修正值每次都從裸狀態依欄位順序重套
// 常駐與仍生效的技能（快照＋重套），而持續中的技能不會再施放，所以同一技能不會疊加
public sealed class SkillCaster
{
    private readonly CombatantState player;
    private readonly IReadOnlyList<EquippedSkill> skills;
    private readonly Dictionary<EquippedSkill, int> remainingRounds = [];

    // 冷卻中的技能：值是還要經過幾個回合開頭才能再發動。每場戰鬥建立新的 SkillCaster，冷卻自然歸零
    private readonly Dictionary<EquippedSkill, int> cooldowns = [];

    // 持續中的技能已經過幾個回合開頭（發動回合為 0）；每個效果在 age < max(1, rounds) 時生效
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
        Cast(SkillTiming.BeforeAttack, hp, sp, random, damage: 0);

    // 玩家物理命中後（嗜血）；主動技能施放後本回合持續中，同一回合不會再發動
    public IReadOnlyList<SkillCast> CastAfterPhysicalHit(int damage, int hp, int sp, IRandomSource random) =>
        Cast(SkillTiming.AfterAttack, hp, sp, random, damage);

    private List<SkillCast> Cast(SkillTiming timing, int hp, int sp, IRandomSource random, int damage)
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
                // 常駐不擲骰、不扣消耗；只有實際回復時才留下紀錄（滿血時沒有事件、耗用的亂數也和沒有技能時相同）
                var passiveHeal = Math.Min(RestoreAmount(equipped, damage), player.HpMax - hp);
                if (passiveHeal > 0)
                {
                    hp += passiveHeal;
                    StartCooldown(equipped, level);
                    casts.Add(new SkillCast(equipped, hp, sp, passiveHeal));
                }

                continue;
            }

            // 持續中、不擲骰的條件不成立、SP 不足或滿血時的回復技能都不擲 Chance：SP 只在住宿回復，不浪費在回復 0 上；
            // 這些情況耗用的亂數與沒有技能時相同
            var spCost = SkillPointCost(level);
            if (remainingRounds.ContainsKey(equipped) || !ConditionsWithoutRollHold(level, hp) || sp < spCost
                || (IsHealing(equipped) && hp >= player.HpMax) || !ChanceConditionsHold(level, random))
            {
                continue;
            }

            sp -= spCost;

            var heal = Math.Min(RestoreAmount(equipped, damage), player.HpMax - hp);
            hp += heal;
            remainingRounds[equipped] = LastingRounds(level);
            ages[equipped] = 0;
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

    private bool ConditionsWithoutRollHold(SkillLevel level, int hp) =>
        level.Conditions.All(condition => condition.Kind switch
        {
            SkillConditionKind.HitPointsBelow => (long)hp * 100 < (long)condition.Percent * player.HpMax,
            _ => true,
        });

    // 依序擲每個 Chance 條件；前面的條件不成立就不擲
    private static bool ChanceConditionsHold(SkillLevel level, IRandomSource random) =>
        level.Conditions.Where(condition => condition.Kind == SkillConditionKind.Chance)
            .All(condition => PhpRandom.Between(random, 1, 100) <= condition.Percent);

    private static int SkillPointCost(SkillLevel level) =>
        level.Costs.Where(cost => cost.Kind == SkillCostKind.SkillPoints).Sum(cost => cost.Amount);

    // 持續中不重複發動：直到最長的效果結束；只在發動回合生效的效果也擋住同一回合
    private static int LastingRounds(SkillLevel level) => level.Effects.Max(effect => Math.Max(1, effect.Rounds));

    private int RestoreAmount(EquippedSkill equipped, int damage) =>
        equipped.Skill.LevelAt(equipped.Level).Effects.OfType<RestoreEffect>().Sum(effect => effect.Scaling is { } scaling
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
        equipped.Skill.LevelAt(equipped.Level).Effects.All(effect => effect is RestoreEffect);

    private bool IsInEffect(EquippedSkill equipped) =>
        equipped.Skill.IsAlwaysOn || remainingRounds.ContainsKey(equipped);

    private void Recompute() =>
        Current = skills.Where(IsInEffect).Aggregate(new BattleModifiers(), ApplyComposed);

    // 同一技能裡同一項能力的百分比相加後才乘進修正值；不同技能之間相乘；固定值與 scaling 一律相加
    private BattleModifiers ApplyComposed(BattleModifiers modifiers, EquippedSkill equipped)
    {
        var age = ages.GetValueOrDefault(equipped);
        var effects = equipped.Skill.LevelAt(equipped.Level).Effects
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
