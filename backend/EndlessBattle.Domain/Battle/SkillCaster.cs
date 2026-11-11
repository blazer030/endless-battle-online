using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Battle;

// Heal、ManaPointsGain 是實際回復的 HP 與 SP（已扣掉超過上限的部分）；OpponentDamage、OpponentManaPointsLoss 是這次發動對對手造成的傷害與削減的 SP
// HealthPointsCost、ManaPointsCost、MoneyCost 是這次發動扣掉的消耗；SilenceRounds 是對手要被封鎖的回合數（含目前回合），沒擲中或沒有封鎖效果時為 0
public sealed record SkillCast(
    EquippedSkill Equipped, int HealthPointsAfter, int ManaPointsAfter, int Heal, int OpponentDamage = 0, int OpponentManaPointsLoss = 0,
    int HealthPointsCost = 0, int ManaPointsCost = 0, int MoneyCost = 0, int ManaPointsGain = 0, int SilenceRounds = 0);

// 技能的施放與持續（研究 §3.3），玩家與怪物共用。主動與被動規則相同，只看每級的條件、消耗與效果：
// 沒有發動條件的技能常駐：裝上就生效、不擲骰、不扣消耗；攻擊後時機的常駐技能（嗜血）每次命中都觸發。
// 有條件的技能在條件全部成立、消耗夠時發動，扣消耗，效果持續各自的 rounds；修正值每次都從裸狀態依欄位順序重套
// 常駐與仍生效的技能（快照＋重套），而持續中的技能不會再施放，所以同一技能不會疊加
public sealed class SkillCaster
{
    // 裝備的技能每一筆都是不同的物件；比較參考，不用每次把整個技能定義算雜湊
    private static readonly IEqualityComparer<EquippedSkill> ByReference = ReferenceEqualityComparer.Instance;

    private readonly CombatantState caster;

    // 對手的基本能力，給「能力比對手高」的條件比較；沒有對手時都當 0
    private readonly CombatantState? opponentStats;
    private readonly IReadOnlyList<EquippedSkill> skills;
    private readonly Dictionary<EquippedSkill, int> remainingRounds = new(ByReference);

    // 冷卻中的技能：值是還要經過幾個回合開頭才能再發動。每場戰鬥建立新的 SkillCaster，冷卻自然歸零
    private readonly Dictionary<EquippedSkill, int> cooldowns = new(ByReference);

    // 持續中的技能已經過幾個回合開頭（發動回合為 0）；每個效果在 age < max(1, rounds) 時生效
    private readonly Dictionary<EquippedSkill, int> ages = new(ByReference);

    // 發動時自己的機率沒擲中的效果（第幾個效果），這次持續期間不生效
    private readonly Dictionary<EquippedSkill, HashSet<int>> missedEffects = new(ByReference);

    // 被對手封鎖技能的剩餘回合（含目前回合）；期間不發動、不判斷條件
    private int silencedRounds;

    // money 是施放者身上的錢；null 表示不限（怪物），金錢消耗不扣
    public SkillCaster(CombatantState caster, IReadOnlyList<EquippedSkill> skills, CombatantState? opponent = null, long? money = null)
    {
        this.caster = caster;
        opponentStats = opponent;
        Money = money;
        this.skills = [.. skills.OrderBy(skill => skill.Slot)];
        Recompute();
    }

    public BattleModifiers Current { get; private set; } = new();

    // 扣完金錢消耗後身上剩下的錢；不限時為 null
    public long? Money { get; private set; }

    // 發動當下（扣消耗前）的 SP，給以 SP 為基準的效果用
    private readonly Dictionary<EquippedSkill, int> castSkillPoints = new(ByReference);

    // 持續 n 回合含施放回合：第 n + 1 回合開頭失效。每回合出手一次，所以冷卻也在回合開頭倒數
    public void ExpireAtRoundStart()
    {
        silencedRounds = Math.Max(0, silencedRounds - 1);
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
                missedEffects.Remove(skill);
                expired = true;
            }
        }

        if (expired)
        {
            Recompute();
        }
    }

    // 對手的封鎖技能：接下來 rounds 回合（含目前回合）這一方的技能都不發動
    public void Silence(int rounds) => silencedRounds = Math.Max(silencedRounds, rounds);

    // 這一回合持續傷害的總量：仍在持續、沒有失手的 DamageOverTime 效果
    public int DamageOverTime() => DamageOverTimeBySkill().Sum(entry => entry.Damage);

    public IReadOnlyList<(EquippedSkill Skill, int Damage)> DamageOverTimeBySkill() =>
        [.. skills.Where(IsInEffect)
            .Select(equipped => (equipped, ActiveEffects(equipped).OfType<DamageOverTimeEffect>().Sum(effect => Scaled(effect.Scaling, equipped))))
            .Where(entry => entry.Item2 > 0)];

    // 出手前，依欄位順序嘗試施放；opponent* 是對手目前的 HP、SP 與 SP 上限，用在削減 HP、SP 的效果
    public IReadOnlyList<SkillCast> CastBeforeAttack(
        int healthPoints, int manaPoints, IRandomSource random, int opponentHealthPoints = 0, int opponentManaPoints = 0, int opponentMaxManaPoints = 0) =>
        Cast(SkillTiming.BeforeAttack, healthPoints, manaPoints, random, damage: 0, new Opponent(opponentHealthPoints, opponentManaPoints, opponentMaxManaPoints));

    // 物理命中後（嗜血）；主動技能施放後本回合持續中，同一回合不會再發動
    public IReadOnlyList<SkillCast> CastAfterPhysicalHit(
        int damage, int healthPoints, int manaPoints, IRandomSource random, int opponentHealthPoints = 0, int opponentManaPoints = 0, int opponentMaxManaPoints = 0) =>
        Cast(SkillTiming.AfterHit, healthPoints, manaPoints, random, damage, new Opponent(opponentHealthPoints, opponentManaPoints, opponentMaxManaPoints));

    private readonly record struct Opponent(int HealthPoints, int ManaPoints, int MaxManaPoints);

    // 回合結束時嘗試施放
    public IReadOnlyList<SkillCast> CastAtRoundEnd(
        int healthPoints, int manaPoints, IRandomSource random, int opponentHealthPoints = 0, int opponentManaPoints = 0, int opponentMaxManaPoints = 0) =>
        Cast(SkillTiming.RoundEnd, healthPoints, manaPoints, random, damage: 0, new Opponent(opponentHealthPoints, opponentManaPoints, opponentMaxManaPoints));

    private List<SkillCast> Cast(SkillTiming timing, int healthPoints, int manaPoints, IRandomSource random, int damage, Opponent opponent)
    {
        var casts = new List<SkillCast>();
        var recompute = false;
        foreach (var equipped in skills.Where(skill => skill.Skill.Timing == timing))
        {
            // 被封鎖時連條件都不判斷
            if (silencedRounds > 0)
            {
                break;
            }

            var level = equipped.Skill.LevelAt(equipped.Level);

            // 冷卻中不擲骰也不觸發
            if (cooldowns.ContainsKey(equipped))
            {
                continue;
            }

            if (equipped.Skill.IsAlwaysOn)
            {
                // 常駐不擲骰、不扣消耗；一次性效果（回復、削減）每到時機都生效，有作用才留下紀錄
                //（滿血時沒有事件、耗用的亂數也和沒有技能時相同）
                var passive = Instant(equipped, damage, healthPoints, manaPoints, opponent);
                if (passive.Heal > 0 || passive.ManaPointsGain > 0 || passive.OpponentDamage > 0 || passive.OpponentManaPointsLoss > 0)
                {
                    healthPoints += passive.Heal;
                    manaPoints += passive.ManaPointsGain;
                    opponent = opponent with { HealthPoints = opponent.HealthPoints - passive.OpponentDamage, ManaPoints = opponent.ManaPoints - passive.OpponentManaPointsLoss };
                    StartCooldown(equipped, level);
                    casts.Add(new SkillCast(equipped, healthPoints, manaPoints, passive.Heal, passive.OpponentDamage, passive.OpponentManaPointsLoss, ManaPointsGain: passive.ManaPointsGain));
                }

                continue;
            }

            // 持續中、不擲骰的條件不成立、消耗不夠或滿血時的回復技能都不擲 Chance：SP 只在住宿回復，不浪費在回復 0 上；
            // 這些情況耗用的亂數與沒有技能時相同
            var (healthPointsCost, manaPointsCost, moneyCost) = CostsOf(level, manaPoints);
            if (remainingRounds.ContainsKey(equipped) || !ConditionsWithoutRollHold(level, healthPoints, opponent.HealthPoints) || !CanPay(level, healthPoints, manaPoints, healthPointsCost, manaPointsCost, moneyCost)
                || RestoresOnlyFullResources(level, healthPoints, manaPoints) || !ChanceConditionsHold(level, random))
            {
                continue;
            }

            // 效果的數值用扣消耗前的 SP 算，再扣消耗、套用效果
            castSkillPoints[equipped] = manaPoints;
            healthPoints -= healthPointsCost;
            manaPoints -= manaPointsCost;
            Money -= Money is null ? null : moneyCost;

            var instant = Instant(equipped, damage, healthPoints, manaPoints, opponent);
            healthPoints += instant.Heal;
            manaPoints += instant.ManaPointsGain;
            opponent = opponent with { HealthPoints = opponent.HealthPoints - instant.OpponentDamage, ManaPoints = opponent.ManaPoints - instant.OpponentManaPointsLoss };
            remainingRounds[equipped] = LastingRounds(level);
            ages[equipped] = 0;
            missedEffects[equipped] = RollEffectChances(level, random);
            var silenceRounds = RollSilence(level, random);
            StartCooldown(equipped, level);
            recompute = true;
            casts.Add(new SkillCast(equipped, healthPoints, manaPoints, instant.Heal, instant.OpponentDamage, instant.OpponentManaPointsLoss,
                healthPointsCost, manaPointsCost, Money is null ? 0 : moneyCost, instant.ManaPointsGain, silenceRounds));
        }

        if (recompute)
        {
            Recompute();
        }

        return casts;
    }

    private bool ConditionsWithoutRollHold(SkillLevel level, int healthPoints, int opponentHealthPoints) =>
        level.Conditions.All(condition => condition.Kind switch
        {
            SkillConditionKind.HitPointsBelow => (long)healthPoints * 100 < (long)condition.Percent * caster.MaxHealthPoints,
            SkillConditionKind.HitPointsBelowOpponent => healthPoints < opponentHealthPoints,
            SkillConditionKind.StatAbove => StatOf(condition.Stat!.Value, caster) > StatOf(condition.Stat.Value, opponentStats),
            _ => true,
        });

    // 依序擲每個 Chance 條件；前面的條件不成立就不擲
    private static bool ChanceConditionsHold(SkillLevel level, IRandomSource random) =>
        level.Conditions.Where(condition => condition.Kind == SkillConditionKind.Chance)
            .All(condition => PhpRandom.Between(random, 1, 100) <= condition.Percent);

    // 百分比一律以最大值為基準；All 是目前全部的 SP
    private (int HealthPoints, int ManaPoints, int Money) CostsOf(SkillLevel level, int manaPoints)
    {
        int AmountOf(SkillCost cost, int maximum) =>
            cost.All ? manaPoints : cost.Percent is { } percent ? BattleModifiers.Round(maximum * percent / 100) : cost.Amount;

        return (
            level.Costs.Where(cost => cost.Kind == SkillCostKind.HitPoints).Sum(cost => AmountOf(cost, caster.MaxHealthPoints)),
            level.Costs.Where(cost => cost.Kind == SkillCostKind.SkillPoints).Sum(cost => AmountOf(cost, caster.MaxManaPoints)),
            level.Costs.Where(cost => cost.Kind == SkillCostKind.Money).Sum(cost => cost.Amount));
    }

    // 不夠扣就不能發動：HP 扣完至少剩 1；SP、金錢要夠；「全部 SP」至少要有 1 點
    private bool CanPay(SkillLevel level, int healthPoints, int manaPoints, int healthPointsCost, int manaPointsCost, int moneyCost) =>
        (healthPointsCost == 0 || healthPoints > healthPointsCost) && manaPoints >= manaPointsCost && (manaPoints > 0 || !level.Costs.Any(cost => cost.All))
        && (Money is null || Money >= moneyCost);

    // 持續中不重複發動：直到最長的效果結束；只在發動回合生效的效果也擋住同一回合
    private static int LastingRounds(SkillLevel level) => level.Effects.Max(effect => Math.Max(1, effect.Rounds));

    private readonly record struct InstantResult(int Heal, int ManaPointsGain, int OpponentDamage, int OpponentManaPointsLoss);

    // 發動當下生效一次的效果：回復 HP／SP、削減對手 HP 與 SP；都不超過可回復或可削減的量
    private InstantResult Instant(EquippedSkill equipped, int damage, int healthPoints, int manaPoints, Opponent opponent)
    {
        var effects = equipped.Skill.LevelAt(equipped.Level).Effects;
        int RestoreOf(SkillResource resource, int maximum) => effects.OfType<RestoreEffect>().Where(effect => effect.Resource == resource)
            .Sum(effect => effect.Scaling is { } scaling ? Scaled(scaling, equipped)
                : effect.PercentOfMaximum is { } percent ? BattleModifiers.Round(maximum * percent / 100)
                : BattleModifiers.Round(damage * effect.PercentOfDamage!.Value / 100));

        var opponentDamage = effects.OfType<PercentDamageEffect>().Sum(effect => BattleModifiers.Round(opponent.HealthPoints * effect.Percent / 100));
        var opponentManaPointsLoss = effects.OfType<ReduceSkillPointsEffect>()
            .Sum(effect => effect.Amount ?? BattleModifiers.Round(opponent.MaxManaPoints * effect.PercentOfMaximum!.Value / 100));
        return new InstantResult(
            Math.Min(RestoreOf(SkillResource.HitPoints, caster.MaxHealthPoints), caster.MaxHealthPoints - healthPoints),
            Math.Max(0, Math.Min(RestoreOf(SkillResource.SkillPoints, caster.MaxManaPoints), caster.MaxManaPoints - manaPoints)),
            Math.Min(opponentDamage, Math.Max(0, opponent.HealthPoints)),
            Math.Min(opponentManaPointsLoss, Math.Max(0, opponent.ManaPoints)));
    }

    // 有自己機率的效果在發動後依序擲；回傳沒擲中的效果
    private static HashSet<int> RollEffectChances(SkillLevel level, IRandomSource random)
    {
        var missed = new HashSet<int>();
        for (var index = 0; index < level.Effects.Count; index++)
        {
            if (SkillPartRules.EffectChance(level.Effects[index]) is { } percent && PhpRandom.Between(random, 1, 100) > percent)
            {
                missed.Add(index);
            }
        }

        return missed;
    }

    // 封鎖在效果自己的機率之後依序擲；只在發動回合生效的封鎖也算 1 回合
    private static int RollSilence(SkillLevel level, IRandomSource random) =>
        level.Effects.OfType<SilenceEffect>()
            .Where(silence => PhpRandom.Between(random, 1, 100) <= silence.Chance)
            .Select(silence => Math.Max(1, silence.Rounds))
            .DefaultIfEmpty(0)
            .Max();

    // 以 SP 為基準時用該技能發動當下（扣消耗前）的 SP；常駐技能用開戰時的 SP
    private int Scaled(IReadOnlyList<SkillScaling> scaling, EquippedSkill equipped) =>
        BattleModifiers.Round(scaling.Sum(term => term.Ratio * (term.Basis == SkillScalingBasis.SkillPoints
            ? castSkillPoints.GetValueOrDefault(equipped, caster.ManaPoints)
            : StatOf(term.Basis, caster))));

    private int StunChanceOf(StunEffect stun) => stun.ChanceByStat is not { } adjustment
        ? stun.Chance
        : stun.Chance + (StatOf(adjustment.Stat, caster) > StatOf(adjustment.Stat, opponentStats) ? adjustment.Percent : -adjustment.Percent);

    // 沒有對手資料時一律當 0
    private static int StatOf(SkillScalingBasis basis, CombatantState? state) => state is null ? 0 : basis switch
    {
        SkillScalingBasis.Strength => state.Strength,
        SkillScalingBasis.Intelligence => state.Intelligence,
        SkillScalingBasis.Vitality => state.Vitality,
        SkillScalingBasis.Agility => state.Agility,
        SkillScalingBasis.Charisma => state.Charisma,
        SkillScalingBasis.Faith => state.Faith,
        SkillScalingBasis.Level => state.Level,
        SkillScalingBasis.Attack => state.Attack,
        SkillScalingBasis.MagicAttack => state.MagicAttack,
        SkillScalingBasis.Defense => state.Defense,
        SkillScalingBasis.SkillPoints => state.ManaPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "未知的能力基準"),
    };

    // 冷卻 n：接下來 n 次出手不能發動。倒數在每回合開頭，下一回合開頭就會先扣 1，所以存 n + 1
    private void StartCooldown(EquippedSkill equipped, SkillLevel level)
    {
        if (level.Cooldown > 0)
        {
            cooldowns[equipped] = level.Cooldown + 1;
        }
    }

    // 效果全部是回復的技能，在要回復的資源都已滿時不發動
    private bool RestoresOnlyFullResources(SkillLevel level, int healthPoints, int manaPoints) =>
        level.Effects.All(effect => effect is RestoreEffect restore
            && (restore.Resource == SkillResource.HitPoints ? healthPoints >= caster.MaxHealthPoints : manaPoints >= caster.MaxManaPoints));

    private bool IsInEffect(EquippedSkill equipped) =>
        equipped.Skill.IsAlwaysOn || remainingRounds.ContainsKey(equipped);

    private void Recompute() =>
        Current = skills.Where(IsInEffect).Aggregate(new BattleModifiers(), ApplyComposed);

    // 同一技能裡同一項能力的百分比相加後才乘進修正值；不同技能之間相乘；固定值與 scaling 一律相加
    // 正在生效的效果：常駐全部生效；持續中的照各自 rounds，發動時沒擲中機率的不算
    private IEnumerable<SkillEffect> ActiveEffects(EquippedSkill equipped)
    {
        var age = ages.GetValueOrDefault(equipped);
        var missed = missedEffects.GetValueOrDefault(equipped);
        return equipped.Skill.LevelAt(equipped.Level).Effects
            .Where((effect, index) => equipped.Skill.IsAlwaysOn || (age < Math.Max(1, effect.Rounds) && missed?.Contains(index) != true));
    }

    private BattleModifiers ApplyComposed(BattleModifiers modifiers, EquippedSkill equipped)
    {
        var effects = ActiveEffects(equipped).ToList();
        var percents = new Dictionary<(SkillTarget, SkillStat), double>();
        var flats = new Dictionary<(SkillTarget, SkillStat), double>();
        var physicalReduction = 0.0;
        var magicReduction = 0.0;
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case StatChangeEffect { Percent: { } percent } change:
                    percents[(change.Target, change.Stat)] = percents.GetValueOrDefault((change.Target, change.Stat)) + percent;
                    break;
                case StatChangeEffect change:
                    flats[(change.Target, change.Stat)] = flats.GetValueOrDefault((change.Target, change.Stat)) + (change.Amount ?? Scaled(change.Scaling!, equipped));
                    break;
                case BonusDamageEffect bonus:
                    var amount = Scaled(bonus.Scaling, equipped);
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
                    physicalReduction += reduction.AttackKind == SkillAttackKind.Magic ? 0 : reduction.Percent;
                    magicReduction += reduction.AttackKind == SkillAttackKind.Physical ? 0 : reduction.Percent;
                    break;
                case StunEffect stun:
                    modifiers = modifiers with { StunChance = Math.Max(modifiers.StunChance, StunChanceOf(stun)) };
                    break;
                case AttackElementEffect element:
                    modifiers = modifiers with { AttackElement = element.Element };
                    break;
                case DefenseElementEffect element:
                    modifiers = modifiers with { DefenseElement = element.Element };
                    break;
                case ElementAdvantageEffect advantage:
                    modifiers = modifiers with { ElementAdvantagePercent = modifiers.ElementAdvantagePercent + advantage.Percent };
                    break;
            }
        }

        foreach (var ((target, stat), percent) in percents)
        {
            var multiplier = 1 + percent / 100;
            modifiers = (target, stat) switch
            {
                (SkillTarget.Caster, SkillStat.Attack) => modifiers with { Attack = modifiers.Attack * multiplier },
                (SkillTarget.Caster, SkillStat.MagicAttack) => modifiers with { MagicAttack = modifiers.MagicAttack * multiplier },
                (SkillTarget.Caster, SkillStat.Defense) => modifiers with { Defense = modifiers.Defense * multiplier },
                (SkillTarget.Caster, SkillStat.MagicDefense) => modifiers with { MagicDefense = modifiers.MagicDefense * multiplier },
                (SkillTarget.Caster, SkillStat.Agility) => modifiers with { Agility = modifiers.Agility * multiplier },
                (SkillTarget.Caster, SkillStat.Charisma) => modifiers with { Charisma = modifiers.Charisma * multiplier },
                (SkillTarget.Opponent, SkillStat.Attack) => modifiers with { EnemyAttack = modifiers.EnemyAttack * multiplier },
                (SkillTarget.Opponent, SkillStat.MagicAttack) => modifiers with { EnemyMagicAttack = modifiers.EnemyMagicAttack * multiplier },
                (SkillTarget.Opponent, SkillStat.Defense) => modifiers with { EnemyDefense = modifiers.EnemyDefense * multiplier },
                (SkillTarget.Opponent, SkillStat.MagicDefense) => modifiers with { EnemyMagicDefense = modifiers.EnemyMagicDefense * multiplier },
                (SkillTarget.Opponent, SkillStat.Agility) => modifiers with { EnemyAgility = modifiers.EnemyAgility * multiplier },
                (SkillTarget.Opponent, SkillStat.Charisma) => modifiers with { EnemyCharisma = modifiers.EnemyCharisma * multiplier },
                _ => throw new ArgumentOutOfRangeException(nameof(equipped), (target, stat), "這項能力不能用百分比增減"),
            };
        }

        foreach (var ((target, stat), flat) in flats)
        {
            // 暴擊率、暴擊上限可以有小數；其餘能力是整數
            var whole = BattleModifiers.Round(flat);
            modifiers = (target, stat) switch
            {
                (SkillTarget.Caster, SkillStat.Attack) => modifiers with { FlatAttack = modifiers.FlatAttack + whole },
                (SkillTarget.Caster, SkillStat.MagicAttack) => modifiers with { FlatMagicAttack = modifiers.FlatMagicAttack + whole },
                (SkillTarget.Caster, SkillStat.Defense) => modifiers with { FlatDefense = modifiers.FlatDefense + whole },
                (SkillTarget.Caster, SkillStat.MagicDefense) => modifiers with { FlatMagicDefense = modifiers.FlatMagicDefense + whole },
                (SkillTarget.Caster, SkillStat.Agility) => modifiers with { FlatAgility = modifiers.FlatAgility + whole },
                (SkillTarget.Caster, SkillStat.Charisma) => modifiers with { FlatCharisma = modifiers.FlatCharisma + whole },
                (SkillTarget.Caster, SkillStat.HitRate) => modifiers with { HitRate = modifiers.HitRate + whole },
                (SkillTarget.Caster, SkillStat.Evasion) => modifiers with { EnemyHitRate = modifiers.EnemyHitRate - whole },
                (SkillTarget.Caster, SkillStat.PhysicalCriticalChance) => modifiers with { CritChanceBonus = modifiers.CritChanceBonus + flat },
                (SkillTarget.Caster, SkillStat.MagicCriticalChance) => modifiers with { MagicCritChanceBonus = modifiers.MagicCritChanceBonus + flat },
                (SkillTarget.Caster, SkillStat.PhysicalCriticalCap) => modifiers with { PhysicalCritCapBonus = modifiers.PhysicalCritCapBonus + flat },
                (SkillTarget.Caster, SkillStat.MagicCriticalCap) => modifiers with { MagicCritCapBonus = modifiers.MagicCritCapBonus + flat },
                (SkillTarget.Caster, SkillStat.EscapeRate) => modifiers with { EscapeBonus = modifiers.EscapeBonus + whole },
                (SkillTarget.Opponent, SkillStat.Attack) => modifiers with { EnemyFlatAttack = modifiers.EnemyFlatAttack + whole },
                (SkillTarget.Opponent, SkillStat.MagicAttack) => modifiers with { EnemyFlatMagicAttack = modifiers.EnemyFlatMagicAttack + whole },
                (SkillTarget.Opponent, SkillStat.Defense) => modifiers with { EnemyFlatDefense = modifiers.EnemyFlatDefense + whole },
                (SkillTarget.Opponent, SkillStat.MagicDefense) => modifiers with { EnemyFlatMagicDefense = modifiers.EnemyFlatMagicDefense + whole },
                (SkillTarget.Opponent, SkillStat.Agility) => modifiers with { EnemyFlatAgility = modifiers.EnemyFlatAgility + whole },
                (SkillTarget.Opponent, SkillStat.Charisma) => modifiers with { EnemyFlatCharisma = modifiers.EnemyFlatCharisma + whole },
                (SkillTarget.Opponent, SkillStat.HitRate) => modifiers with { EnemyHitRate = modifiers.EnemyHitRate + whole },
                _ => throw new ArgumentOutOfRangeException(nameof(equipped), (target, stat), "這項能力不能用固定值增減"),
            };
        }

        if (physicalReduction != 0)
        {
            modifiers = modifiers with { PhysicalTaken = modifiers.PhysicalTaken * (1 - physicalReduction / 100) };
        }

        return magicReduction == 0 ? modifiers : modifiers with { MagicTaken = modifiers.MagicTaken * (1 - magicReduction / 100) };
    }
}
