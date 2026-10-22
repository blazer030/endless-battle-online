using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Champion;

public class ChampionDuelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);

    private static readonly ChampionSeat Seat = new("測試冠軍", Level: 5, Element: 0, SpecialAttackName: "測試劍", Hp: 500, HpMax: 500, Sp: 20, SpMax: 20,
        At: 100, Mat: 5, Df: 0, Mdf: 0, Agi: 10, Au: 6, Defenses: 2, LockedUntil: Now.AddDays(-1));

    // 裝備：Str +10、Life +4、Agi +5、at +50、df +20、hpMax +100
    private static readonly EquipmentBonus Gear = new(new StatBonus(Str: 10, Smart: 0, Life: 4, Vit: 0, Agi: 5, Au: 0, Be: 0, At: 50, Mat: 0, Df: 20, Mdf: 0, HpMax: 100));

    private static readonly Character Weak = TestCharacters.Default(1, "挑戰者", 1, 1, Now);

    // 有效能力 Str 510、Smart 20、Life 204、Vit 300、Agi 205、Au 100、Be 300 →
    // at 560、mat 20、df 320、mdf 300、hpMax 204×25+50+100 = 5250、spMax (20+300)×3+25 = 985
    private static readonly Character Strong = Weak with
    {
        Level = 40,
        Base = new BaseStats(Str: 500, Smart: 20, Life: 200, Vit: 300, Agi: 200, Au: 100, Be: 300),
        Hp = 3000,
    };

    private static DuelResult Fight(Character character, ChampionSeat seat, EquipmentBonus? equipment = null) =>
        ChampionDuel.Fight(character, equipment ?? EquipmentBonus.Zero, [], seat, Now, new SeededRandom(9));

    [Fact]
    public void Duel_WinReplacesSeatWithFullHealthSnapshot()
    {
        var result = Fight(Strong, Seat, Gear);

        Assert.True(result.Won);
        Assert.Equal(CombatSide.Player, result.Battle.Winner);
        Assert.Equal(
            new ChampionSeat("挑戰者", Level: 40, Element: 1, SpecialAttackName: "必殺", Hp: 5250, HpMax: 5250, Sp: 985, SpMax: 985,
                At: 560, Mat: 20, Df: 320, Mdf: 300, Agi: 205, Au: 100, IsPlayer: true, Defenses: 0, CrownedAt: Now, LockedUntil: Now.AddSeconds(604_800)),
            result.Seat);
        Assert.Equal(new RoundEvent(0, 1), result.Battle.Events[0]);
    }

    [Fact]
    public void Duel_LossKeepsChampionDamage()
    {
        var result = Fight(Weak, Seat);

        Assert.False(result.Won);
        Assert.Equal(CombatSide.Monster, result.Battle.Winner);
        Assert.InRange(result.Battle.Monster.Hp, 1, Seat.HpMax - 1);
        Assert.Equal(Seat with { Hp = result.Battle.Monster.Hp, Defenses = 3 }, result.Seat);
    }

    [Fact]
    public void Duel_ChargesFeeEitherWay()
    {
        foreach (var result in new[] { Fight(Strong, Seat, Gear), Fight(Weak, Seat) })
        {
            Assert.Equal(1500, result.Character.Money);
            Assert.Equal(0, result.Character.Exp);
            Assert.Equal((result.Battle.Player.Hp, result.Battle.Player.Sp), (result.Character.Hp, result.Character.Sp));
            Assert.Empty(result.Battle.Events.OfType<RewardEvent>());
        }
    }

    // 雙方都打不痛：回合用完算冠軍衛冕，HP 留在戰後剩餘值
    [Fact]
    public void Duel_TimeoutCountsAsDefense()
    {
        var turtle = Weak with { Base = new BaseStats(Str: 1, Smart: 1, Life: 400, Vit: 1000, Agi: 10, Au: 10, Be: 1000), Hp = 10_000 };
        var wall = Seat with { Hp = 9000, HpMax = 9000, At = 1, Mat = 1, Df = 1000, Mdf = 1000 };

        var result = Fight(turtle, wall);

        Assert.Null(result.Battle.Winner);
        Assert.False(result.Won);
        Assert.Equal(wall with { Hp = result.Battle.Monster.Hp, Defenses = 3 }, result.Seat);
        Assert.Equal(1500, result.Character.Money);
    }

    // 低 HP 也不喝補給（Fight 根本不接補給），也沒有寵物出場
    [Fact]
    public void Duel_UsesNoSupplyAndNoPet()
    {
        var result = Fight(Weak with { Hp = 5 }, Seat);

        Assert.Empty(result.Battle.Events.OfType<PotionEvent>());
        Assert.Empty(result.Battle.Events.OfType<PetGuardEvent>());
        Assert.Empty(result.Battle.Events.OfType<PetAttackEvent>());
        Assert.Null(result.Battle.Supply);
    }

    // 玩家裝備的技能照常施放（冠軍沒有技能），戰後 SP 寫回角色
    [Fact]
    public void Duel_AppliesEquippedSkills()
    {
        var burst = new EquippedSkill(TestSkills.Battle("burst", "爆氣", SkillEffectKind.AtMultiplier), Level: 1, Chance: 100, Slot: 1);

        var result = ChampionDuel.Fight(Weak, EquipmentBonus.Zero, [burst], Seat, Now, new SeededRandom(9));

        Assert.Contains(result.Battle.Events.OfType<SkillEvent>(), skill => skill.Caster == CombatSide.Player);
        Assert.DoesNotContain(result.Battle.Events.OfType<SkillEvent>(), skill => skill.Caster == CombatSide.Monster);
        Assert.True(result.Character.Sp < Weak.Sp);
    }
}
