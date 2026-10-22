using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// ScriptedRandom 的換算同 WogBattleTests；寵物出擊率固定 35：rand(1,100) = 1 出擊、100 不出擊
public class WogBattlePetTests
{
    private const string PetName = "小雪";
    private static readonly double PetAttackOffset = BattleTiming.SecondsPerRound * 0.95;

    private static CombatantState Player(int hp = 100, int at = 10, int df = 50, int agi = 10, int au = 10) =>
        new(hp, hp, Sp: 0, SpMax: 0, at, Mat: 0, df, Mdf: 50, agi, au, Element: 0, "烈焰斬", Str: at, Smart: 0);

    private static CombatantState Monster(int hp = 1000, int at = 0, int df = 50, int mdf = 50, int agi = 10, int au = 10) =>
        new(hp, hp, Sp: 0, SpMax: 0, at, Mat: 0, df, mdf, agi, au, Element: 0, SpecialAttackName: null, Str: at, Smart: 0);

    // 對上測試雪精的數值：df 6、mdf 4、agi 9、au 6；怪物 at 0 也打不中（擲 100）
    private static CombatantState Sprite(int hp = 1000) => Monster(hp, at: 0, df: 6, mdf: 4, agi: 9, au: 6);

    private static PetCombatant Pet(int guard = 500) => new(PetName, Attack: 30, Magic: 20, guard, Activation: 35);

    private static BattleOutcome Run(CombatantState player, CombatantState monster, PetCombatant? pet, IRandomSource random) =>
        WogBattle.Run(player, monster, playerPreferred: null, skills: [], supply: null, random, pet);

    // agi 相同 → 30% 先攻。擲骰：先攻 0.99 怪物先；命中 0.0；暴擊 0.99 不中；傷害 rand(0,0) 吃掉 0.5 → (100·2 − 50) = 150；
    // 代擋 0.0 → 1 < 35 → 寵物承受 150，代擋量 500 → 350；玩家命中 0.99 → 100，70 > 100 不成立；寵物出擊 0.99 不出擊
    [Fact]
    public void Battle_PetGuardAbsorbsHit()
    {
        var outcome = Run(Player(), Monster(at: 100), Pet(guard: 500), new ScriptedRandom(0.99, 0.99, 0.0, 0.99, 0.5, 0.0, 0.99, 0.99));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new PetGuardEvent(0, PetName, Damage: 150, GuardAfter: 350, Retired: false),
                new AttackEvent(0.4, CombatSide.Player, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 1000, SpecialAttackName: null),
                new RoundEvent(BattleTiming.SecondsPerRound, 2),
            },
            outcome.Events.Take(4));
        Assert.Equal(100, outcome.Player.Hp);
    }

    // 怪物沒打中時不擲代擋：先攻 0.99 怪物先；命中 0.99 未命中；下一顆 0.0 是玩家的命中（若誤擲代擋就會被吃掉、玩家改擲 0.99 落空）；
    // 暴擊 0.99 不中；傷害 rand(0,0) 吃掉 0.5 → 150
    [Fact]
    public void Battle_PetDoesNotGuardMisses()
    {
        var outcome = Run(Player(at: 100), Monster(at: 100), Pet(), new ScriptedRandom(0.99, 0.99, 0.99, 0.0, 0.99, 0.5));

        Assert.Empty(outcome.Events.OfType<PetGuardEvent>());
        Assert.Equal(
            new AttackEvent(0.4, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 850, SpecialAttackName: null),
            outcome.Events.OfType<AttackEvent>().First(attack => attack.Attacker == CombatSide.Player));
    }

    // 怪物 at 10 對玩家 df 5：(10·2 − 5) = 15，比值 1.5 → rand(0,0)。
    // 第 1 回合：先攻 0.99 怪物先；命中 0.0；暴擊 0.99 不中；傷害 0.5；代擋 0.0 成功 → 10 − 15 ≤ 0 退場；玩家命中 0.99 落空；退場後不擲出擊。
    // 第 2 回合：先攻 0.99 怪物先；命中 0.0；暴擊 0.99；傷害 0.5 → 15 打在玩家身上（100 → 85）；
    //           下一顆 0.0 是玩家的命中（若誤擲代擋會被代擋吃掉）。之後一律 0.0：寵物若還在就會出擊
    [Fact]
    public void Battle_PetRetiresWhenGuardDepleted()
    {
        var outcome = Run(Player(df: 5), Monster(at: 10), Pet(guard: 10),
            new ScriptedRandom(0.0, 0.99, 0.0, 0.99, 0.5, 0.0, 0.99, 0.99, 0.0, 0.99, 0.5, 0.0));

        Assert.Equal(new PetGuardEvent(0, PetName, Damage: 15, GuardAfter: 0, Retired: true), outcome.Events.OfType<PetGuardEvent>().Single());
        Assert.Contains(
            new AttackEvent(BattleTiming.SecondsPerRound, CombatSide.Monster, AttackKind.Physical, Hit: true, Critical: false, Damage: 15, ElementMultiplier: 1.0, TargetHpAfter: 85, SpecialAttackName: null),
            outcome.Events);
        Assert.Empty(outcome.Events.OfType<PetAttackEvent>());
        Assert.Equal(CombatSide.Monster, outcome.Winner);
    }

    // 玩家 agi 12 > 9 → 70% 先攻。擲骰：先攻 0.0 玩家先；玩家命中 0.99 → 100，77.1 > 100 不成立；怪物物理（at 0 ≥ mat 0 不擲）命中 0.99 落空；
    // 寵物：出擊 0.0 → 1 < 35；方式 rand(1,2) 依參數；命中 rand(1,3) 0.0 → 1 < 3；暴擊 0.99 → 100 不中；
    // 物理 Attack 30 對 df 6：30·5 − 6 = 144，比值 4.8 → rand(1,2) 擲 0.0 → 1，再加 1 → 146；
    // 魔法 Magic 20 對 mdf 4：20·5 − 4 = 96，同樣 +1 +1 → 98
    [Theory]
    [InlineData(0.0, AttackKind.Physical, 146)]
    [InlineData(0.5, AttackKind.Magic, 98)]
    public void Battle_PetAttackDealsRestoredDamage(double kindRoll, AttackKind kind, int damage)
    {
        var outcome = Run(Player(agi: 12), Sprite(), Pet(), new ScriptedRandom(0.99, 0.0, 0.99, 0.99, 0.0, kindRoll, 0.0, 0.99, 0.0));

        Assert.Equal(
            new PetAttackEvent(PetAttackOffset, PetName, kind, Hit: true, Critical: false, damage, TargetHpAfter: 1000 - damage),
            outcome.Events.OfType<PetAttackEvent>().Single());
        Assert.Equal(1000 - damage, outcome.Monster.Hp);
        Assert.Equal(damage, outcome.DamageDealtToMonster);
    }

    // 同上，命中擲 0.99 → rand(1,3) = 3 → 落空，傷害 0、不擲暴擊與傷害
    [Fact]
    public void Battle_PetMissesOneInThree()
    {
        var outcome = Run(Player(agi: 12), Sprite(), Pet(), new ScriptedRandom(0.99, 0.0, 0.99, 0.99, 0.0, 0.0, 0.99));

        Assert.Equal(
            new PetAttackEvent(PetAttackOffset, PetName, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, TargetHpAfter: 1000),
            outcome.Events.OfType<PetAttackEvent>().Single());
        Assert.Equal(0, outcome.DamageDealtToMonster);
    }

    // 暴擊比 r = (玩家 au 10 + agi 12) / (怪 au 6 + agi 9) = 22/15：機率 min(20, 14.67)、倍率 min(2, 1.76)。
    // 0.135 → 14 < 14.67 暴擊：Attack 30 × 1.76 = 52.8，52.8·8.8 − 6 = 458.64，比值 8.69 → rand(2,4) 擲 0.0 → 2，+1 → round(461.64) = 462；
    // 0.145 → 15 不暴擊 → 146
    [Theory]
    [InlineData(0.135, true, 462)]
    [InlineData(0.145, false, 146)]
    public void Battle_PetCriticalUsesAuAgiRatio(double criticalRoll, bool critical, int damage)
    {
        var outcome = Run(Player(agi: 12, au: 10), Sprite(), Pet(), new ScriptedRandom(0.99, 0.0, 0.99, 0.99, 0.0, 0.0, 0.0, criticalRoll, 0.0));

        var attack = outcome.Events.OfType<PetAttackEvent>().Single();
        Assert.Equal((true, critical, damage), (attack.Hit, attack.Critical, attack.Damage));
        if (critical)
        {
            Assert.Equal(WogDamage.Fight(30 * 1.76, 6, 1.0, new ScriptedRandom(0.0), bonusDamage: 1), damage);
        }
    }

    // 擲骰同 Battle_PetAttackDealsRestoredDamage（物理）：雪精只剩 5 HP，寵物 146 擊倒 → 玩家勝，傷害計入
    [Fact]
    public void Battle_PetKillWinsTheFight()
    {
        var outcome = Run(Player(agi: 12), Sprite(hp: 5), Pet(), new ScriptedRandom(0.99, 0.0, 0.99, 0.99, 0.0, 0.0, 0.0, 0.99, 0.0));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 5, SpecialAttackName: null),
                new AttackEvent(0.4, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 100, SpecialAttackName: null),
                new PetAttackEvent(PetAttackOffset, PetName, AttackKind.Physical, Hit: true, Critical: false, Damage: 146, TargetHpAfter: 0),
                new DefeatEvent(PetAttackOffset, CombatSide.Monster),
            },
            outcome.Events);
        Assert.Equal(CombatSide.Player, outcome.Winner);
        Assert.Equal(1, outcome.Rounds);
        Assert.Equal(146, outcome.DamageDealtToMonster);
        Assert.Equal(0, outcome.Monster.Hp);
    }

    // 沒有寵物時新多載不擲任何多的骰：同一串亂數（取自 SeededRandom）跑舊多載與新多載，事件與用掉的亂數數相同
    [Fact]
    public void Battle_WithoutPetKeepsRandomSequence()
    {
        var seeded = new SeededRandom(42);
        var script = Enumerable.Range(0, 5000).Select(_ => seeded.NextDouble()).ToArray();
        var player = Player(hp: 300, at: 40, df: 20, agi: 12);
        var monster = Monster(hp: 300, at: 40, df: 20, agi: 11);
        var before = new ScriptedRandom(0.5, script);
        var after = new ScriptedRandom(0.5, script);

        var expected = WogBattle.Run(player, monster, playerPreferred: null, skills: [], supply: null, before);
        var actual = WogBattle.Run(player, monster, playerPreferred: null, skills: [], supply: null, after, pet: null);

        Assert.Equivalent(expected.Events, actual.Events, strict: true);
        Assert.Equal(
            (expected.Winner, expected.Rounds, expected.DamageDealtToMonster, expected.Player, expected.Monster),
            (actual.Winner, actual.Rounds, actual.DamageDealtToMonster, actual.Player, actual.Monster));
        Assert.Equal(before.Consumed, after.Consumed);
        Assert.True(before.Consumed > 10);
    }
}
