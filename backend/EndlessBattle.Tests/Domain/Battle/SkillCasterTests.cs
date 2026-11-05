using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

public class SkillCasterTests
{
    private static readonly SkillDefinition Burst = TestSkills.Battle("burst", "爆氣", SkillPreset.AtMultiplier);
    private static readonly SkillDefinition IronSkin = TestSkills.Battle("iron-skin", "硬甲", SkillPreset.DfMultiplier, SkillType.Passive);
    private static readonly SkillDefinition Lullaby = TestSkills.Battle("lullaby", "靡靡之音", SkillPreset.EnemyAgiDown);
    private static readonly SkillDefinition ManaSurge = TestSkills.Battle("mana-surge", "魔力增幅", SkillPreset.MatMultiplier);

    private static CombatantState Player(int sp) =>
        new(Hp: 100, HpMax: 100, Sp: sp, SpMax: sp, At: 100, Mat: 0, Df: 50, Mdf: 50, Agi: 20, Au: 25, Element: 0, SpecialAttackName: null, Str: 100, Smart: 0);

    // 欄位 3、1、2 亂序傳入，擲骰都是 0.0 → 1，不超過任何等級的發動率（30 + 10·lv）。依欄位 1→5：
    // 爆氣 lv1 sp 5（20 → 15）、魔力增幅 lv2 sp 8（15 → 7）、靡靡之音 lv3 sp 11 > 7 → 不擲骰也不扣。
    // 修正值：at ×1.01、mat ×1.02，其他維持預設
    [Fact]
    public void Caster_CastsInSlotOrder_AndDeductsSp()
    {
        var random = new ScriptedRandom(0.0);
        var caster = new SkillCaster(Player(sp: 20),
        [
            new EquippedSkill(Lullaby, Level: 3, Slot: 3),
            new EquippedSkill(Burst, Level: 1, Slot: 1),
            new EquippedSkill(ManaSurge, Level: 2, Slot: 2),
        ]);

        var casts = caster.CastBeforeAttack(hp: 100, sp: 20, random);

        Assert.Equal(["burst", "mana-surge"], casts.Select(cast => cast.Equipped.Skill.Id));
        Assert.Equal([15, 7], casts.Select(cast => cast.SpAfter));
        Assert.Equal(2, random.Consumed);
        Assert.Equal(1.01, caster.Current.At, 10);
        Assert.Equal(1.02, caster.Current.Mat, 10);
        Assert.Equal(new BattleModifiers(), caster.Current with { At = 1, Mat = 1 });
    }

    // 被動常駐：裝上就生效、不擲骰、不扣 SP，回合開頭也不會失效
    [Fact]
    public void Caster_PassivesAreAlwaysOn_WithoutRollOrSp()
    {
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(sp: 0), [new EquippedSkill(IronSkin, Level: 2, Slot: 4)]);

        Assert.Equal(1.02, caster.Current.Df, 10);
        Assert.Empty(caster.CastBeforeAttack(hp: 100, sp: 0, random));
        caster.ExpireAtRoundStart();
        caster.ExpireAtRoundStart();

        Assert.Equal(0, random.Consumed);
        Assert.Equal(1.02, caster.Current.Df, 10);
    }

    // 攻擊後的被動（嗜血 lv5：傷害 ×0.1）每次物理命中都回復，不擲骰、不扣 SP
    [Fact]
    public void Caster_PassiveAfterHit_HealsWithoutRollOrSp()
    {
        var bloodthirst = TestSkills.Battle("bloodthirst", "嗜血", SkillPreset.Bloodthirst, SkillType.Passive, SkillTiming.AfterHit, rounds: 1);
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(sp: 0), [new EquippedSkill(bloodthirst, Level: 5, Slot: 4)]);

        var first = Assert.Single(caster.CastAfterPhysicalHit(damage: 150, hp: 50, sp: 0, random));
        var second = Assert.Single(caster.CastAfterPhysicalHit(damage: 150, hp: 65, sp: 0, random));

        Assert.Equal((65, 0, 15), (first.HpAfter, first.SpAfter, first.Heal));
        Assert.Equal((80, 0, 15), (second.HpAfter, second.SpAfter, second.Heal));
        Assert.Equal(0, random.Consumed);
    }

    // 依 WogBattle 的順序跑 n 回合（回合開頭失效 → 出手前施放），回傳有施放的回合
    private static List<int> CastRounds(SkillCaster caster, int rounds, int hp, IRandomSource random)
    {
        var castRounds = new List<int>();
        for (var round = 1; round <= rounds; round++)
        {
            caster.ExpireAtRoundStart();
            if (caster.CastBeforeAttack(hp, sp: 100, random).Count > 0)
            {
                castRounds.Add(round);
            }
        }

        return castRounds;
    }

    // 冷卻 2、持續 1 回合，擲骰都是 0.0（必中）：第 1 回合施放，接下來 2 次玩家出手（第 2、3 回合）冷卻中、不擲骰，
    // 第 4 回合冷卻結束可再施放，第 5 回合又在冷卻中。只擲了 2 顆
    [Fact]
    public void Caster_CooldownBlocksTheNextPlayerTurns()
    {
        var strike = TestSkills.Battle("strike", "強擊", SkillPreset.AtMultiplier, rounds: 1, cooldown: 2);
        var random = new ScriptedRandom(0.0);
        var caster = new SkillCaster(Player(sp: 100), [new EquippedSkill(strike, Level: 1, Slot: 1)]);

        Assert.Equal([1, 4], CastRounds(caster, rounds: 5, hp: 100, random));
        Assert.Equal(2, random.Consumed);
    }

    // SP 不夠時不發動、不擲骰，也不進冷卻：強擊 lv1 耗 5、冷卻 3。第 1 回合 SP 4 不發；第 2 回合 SP 夠了立刻發動
    [Fact]
    public void Caster_SpShortActive_DoesNotStartCooldown()
    {
        var strike = TestSkills.Battle("strike", "強擊", SkillPreset.AtMultiplier, rounds: 1, cooldown: 3);
        var random = new ScriptedRandom(0.0);
        var caster = new SkillCaster(Player(sp: 100), [new EquippedSkill(strike, Level: 1, Slot: 1)]);

        caster.ExpireAtRoundStart();
        Assert.Empty(caster.CastBeforeAttack(hp: 100, sp: 4, random));
        caster.ExpireAtRoundStart();
        Assert.Single(caster.CastBeforeAttack(hp: 100, sp: 5, random));

        Assert.Equal(1, random.Consumed);
    }

    // 發動那一回合本身不算冷卻：Cooldown 1 → 第 1 回合發動、第 2 回合不能發、第 3 回合起可以再發
    [Fact]
    public void Caster_CooldownOne_SkipsExactlyOneTurn()
    {
        var strike = TestSkills.Battle("strike", "強擊", SkillPreset.AtMultiplier, rounds: 1, cooldown: 1);
        var caster = new SkillCaster(Player(sp: 100), [new EquippedSkill(strike, Level: 1, Slot: 1)]);

        Assert.Equal([1, 3], CastRounds(caster, rounds: 3, hp: 100, new ScriptedRandom(0.0)));
    }

    // Cooldown 0 且只持續 1 回合：下一回合就能再判斷，每回合都發動
    [Fact]
    public void Caster_CooldownZero_AllowsTheNextTurn()
    {
        var strike = TestSkills.Battle("strike", "強擊", SkillPreset.AtMultiplier, rounds: 1);
        var caster = new SkillCaster(Player(sp: 100), [new EquippedSkill(strike, Level: 1, Slot: 1)]);

        Assert.Equal([1, 2, 3], CastRounds(caster, rounds: 3, hp: 100, new ScriptedRandom(0.0)));
    }

    // 治癒（hpBelow 50）：智力 20、lv1 回復 20。HP 49／100 低於 50% 必定發動、不擲骰；50／100 不低於門檻就不發
    [Theory]
    [InlineData(49, true)]
    [InlineData(50, false)]
    public void Caster_HpBelow_FiresOnlyUnderThresholdWithoutRoll(int hp, bool fires)
    {
        var heal = TestSkills.Battle("heal", "治癒", SkillPreset.Heal, rounds: 1, trigger: TestTrigger.HpBelow(50));
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(sp: 100) with { Smart = 20 }, [new EquippedSkill(heal, Level: 1, Slot: 1)]);

        var casts = caster.CastBeforeAttack(hp, sp: 100, random);

        Assert.Equal(fires ? [(hp + 20, 95)] : [], casts.Select(cast => (cast.HpAfter, cast.SpAfter)));
        Assert.Equal(0, random.Consumed);
    }

    // 回復技滿血時不發動：門檻最高 99%，HP 100／100 永遠不觸發
    [Fact]
    public void Caster_HpBelowHeal_SkipsAtFullHp()
    {
        var heal = TestSkills.Battle("heal", "治癒", SkillPreset.Heal, rounds: 1, trigger: TestTrigger.HpBelow(99));
        var caster = new SkillCaster(Player(sp: 100) with { Smart = 20 }, [new EquippedSkill(heal, Level: 1, Slot: 1)]);

        Assert.Empty(caster.CastBeforeAttack(hp: 100, sp: 100, new ScriptedRandom(0.0)));
    }

    // hpBelow 也吃冷卻：冷卻 4、HP 一直低於門檻，第 1 回合發動後第 2～5 回合不發，第 6 回合再發
    [Fact]
    public void Caster_HpBelow_RespectsCooldown()
    {
        var heal = TestSkills.Battle("heal", "治癒", SkillPreset.Heal, rounds: 1, cooldown: 4, trigger: TestTrigger.HpBelow(50));
        var caster = new SkillCaster(Player(sp: 100) with { Smart = 20 }, [new EquippedSkill(heal, Level: 1, Slot: 1)]);

        Assert.Equal([1, 6], CastRounds(caster, rounds: 6, hp: 10, new ScriptedRandom(0.0)));
    }

    // 掛 hpBelow 的被動：平時不生效，HP 低於門檻才觸發；不擲骰、不扣 SP（SP 0 也能發）。硬甲 lv2 觸發後 df ×1.02
    [Fact]
    public void Caster_TriggeredPassive_AppliesOnlyWhenTriggered_WithoutSp()
    {
        var lastStand = TestSkills.Battle("last-stand", "背水", SkillPreset.DfMultiplier, SkillType.Passive, trigger: TestTrigger.HpBelow(30));
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(sp: 0), [new EquippedSkill(lastStand, Level: 2, Slot: 4)]);

        Assert.Equal(1.0, caster.Current.Df);
        Assert.Empty(caster.CastBeforeAttack(hp: 30, sp: 0, random));
        var cast = Assert.Single(caster.CastBeforeAttack(hp: 29, sp: 0, random));

        Assert.Equal(0, cast.SpAfter);
        Assert.Equal(1.02, caster.Current.Df, 10);
        Assert.Equal(0, random.Consumed);
    }

    // 觸發型被動也能擲骰（chance 型）：lv5 發動率 80。第 1 回合擲 0.99 → 100 不發；第 2 回合擲 0.0 → 1 發動，不扣 SP，
    // 效果照該級 rounds 持續 3 回合（第 2～4 回合），持續中不再擲骰；第 5 回合開頭失效後再擲 0.0 又發動。共擲 3 顆
    [Fact]
    public void Caster_ChancePassive_RollsAndLastsItsRounds()
    {
        var guard = TestSkills.Battle("guard", "架式", SkillPreset.DfMultiplier, SkillType.Passive, rounds: 3, trigger: TestTrigger.Chance);
        var random = new ScriptedRandom(0.0, 0.99);
        var caster = new SkillCaster(Player(sp: 0), [new EquippedSkill(guard, Level: 5, Slot: 4)]);
        var dfByRound = new List<double>();

        for (var round = 1; round <= 5; round++)
        {
            caster.ExpireAtRoundStart();
            Assert.All(caster.CastBeforeAttack(hp: 100, sp: 0, random), cast => Assert.Equal(0, cast.SpAfter));
            dfByRound.Add(Math.Round(caster.Current.Df, 10));
        }

        Assert.Equal([1.0, 1.05, 1.05, 1.05, 1.05], dfByRound);
        Assert.Equal(3, random.Consumed);
    }

    // 攻擊後的被動也吃冷卻（嗜血 lv5、冷卻 1）：第 1 回合第一下回復，同回合第二下與第 2 回合都在冷卻中，第 3 回合再回復
    [Fact]
    public void Caster_PassiveAfterHit_RespectsCooldown()
    {
        var bloodthirst = TestSkills.Battle("bloodthirst", "嗜血", SkillPreset.Bloodthirst, SkillType.Passive, SkillTiming.AfterHit, rounds: 1, cooldown: 1);
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(sp: 0), [new EquippedSkill(bloodthirst, Level: 5, Slot: 4)]);
        var heals = new List<int>();

        for (var round = 1; round <= 3; round++)
        {
            caster.ExpireAtRoundStart();
            heals.Add(caster.CastAfterPhysicalHit(damage: 150, hp: 10, sp: 0, random).Sum(cast => cast.Heal));
            heals.Add(caster.CastAfterPhysicalHit(damage: 150, hp: 10, sp: 0, random).Sum(cast => cast.Heal));
        }

        Assert.Equal([15, 0, 0, 0, 15, 0], heals);
        Assert.Equal(0, random.Consumed);
    }

    // SP 剛好等於消耗（爆氣 lv1 sp 5）也能施放，施放後 SP 為 0
    [Fact]
    public void Caster_CastsWhenSpEqualsCost()
    {
        var caster = new SkillCaster(Player(sp: 5), [new EquippedSkill(Burst, Level: 1, Slot: 1)]);

        var cast = Assert.Single(caster.CastBeforeAttack(hp: 100, sp: 5, new ScriptedRandom(0.0)));

        Assert.Equal(0, cast.SpAfter);
    }

    // 技能表的公式（value = lv）；玩家 str 100、au 25
    public static TheoryData<SkillPreset, int, BattleModifiers> EffectFormulas => new()
    {
        { SkillPreset.AtMultiplier, 5, new BattleModifiers(At: 1.05) },
        { SkillPreset.MatMultiplier, 5, new BattleModifiers(Mat: 1.05) },
        { SkillPreset.DfMultiplier, 5, new BattleModifiers(Df: 1.05) },
        { SkillPreset.MdfMultiplier, 5, new BattleModifiers(Mdf: 1.05) },
        { SkillPreset.AgiMultiplier, 5, new BattleModifiers(Agi: 1.05) },
        { SkillPreset.MultiHit, 3, new BattleModifiers(At: 0.97, PhysicalHits: 4) },
        { SkillPreset.MagicMultiHit, 3, new BattleModifiers(MagicHits: 4) },
        { SkillPreset.EnemyAgiDown, 5, new BattleModifiers(EnemyAgi: 0.95) },
        { SkillPreset.EnemyAttackDown, 5, new BattleModifiers(EnemyAt: 0.9, EnemyMat: 0.9) },
        { SkillPreset.Heal, 5, new BattleModifiers() },
        { SkillPreset.WarGodWrath, 1, new BattleModifiers(FlatAt: 45, Rag: -70) },
        { SkillPreset.Counter, 5, new BattleModifiers(At: 1.05, PhysicalTaken: 0.82) },
        { SkillPreset.CritChanceUp, 5, new BattleModifiers(CritChanceBonus: 10) },
        { SkillPreset.Bloodthirst, 5, new BattleModifiers() },
        { SkillPreset.BonusDamage, 5, new BattleModifiers(BonusDamage: 10) },
        { SkillPreset.Stun, 5, new BattleModifiers(StunChance: 15) },
    };

    [Theory]
    [MemberData(nameof(EffectFormulas))]
    public void Caster_AppliesEffectFormulas(SkillPreset effect, int level, BattleModifiers expected)
    {
        var caster = new SkillCaster(Player(sp: 100), [new EquippedSkill(TestSkills.Battle("skill", "技能", effect), level, Slot: 1)]);

        caster.CastBeforeAttack(hp: 100, sp: 100, new ScriptedRandom(0.0));

        Assert.Equal(Normalize(expected), Normalize(caster.Current));
    }

    private static BattleModifiers Normalize(BattleModifiers modifiers) => modifiers with
    {
        At = Math.Round(modifiers.At, 10),
        Mat = Math.Round(modifiers.Mat, 10),
        Df = Math.Round(modifiers.Df, 10),
        Mdf = Math.Round(modifiers.Mdf, 10),
        Agi = Math.Round(modifiers.Agi, 10),
        PhysicalTaken = Math.Round(modifiers.PhysicalTaken, 10),
        EnemyAgi = Math.Round(modifiers.EnemyAgi, 10),
        EnemyAt = Math.Round(modifiers.EnemyAt, 10),
        EnemyMat = Math.Round(modifiers.EnemyMat, 10),
    };
}
