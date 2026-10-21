using System.Text.Json.Serialization;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Battle;

// 前端用同一串事件同時驅動對戰舞台與戰報；AtSeconds 是相對於這批事件起點的秒數
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EncounterEvent), "encounter")]
[JsonDerivedType(typeof(RoundEvent), "round")]
[JsonDerivedType(typeof(AttackEvent), "attack")]
[JsonDerivedType(typeof(DefeatEvent), "defeat")]
[JsonDerivedType(typeof(TimeoutEvent), "timeout")]
[JsonDerivedType(typeof(RewardEvent), "reward")]
[JsonDerivedType(typeof(LevelUpEvent), "levelUp")]
[JsonDerivedType(typeof(RestEvent), "rest")]
public abstract record BattleEvent(double AtSeconds);

public sealed record EncounterEvent(
    double AtSeconds,
    string MonsterId,
    int MonsterHp,
    int MonsterHpMax,
    int PlayerHp,
    int PlayerHpMax,
    int PlayerSp,
    int PlayerSpMax,
    bool StaminaDepleted) : BattleEvent(AtSeconds);

public sealed record RoundEvent(double AtSeconds, int Round) : BattleEvent(AtSeconds);

// 未命中時 Damage 為 0、ElementMultiplier 為 1
public sealed record AttackEvent(
    double AtSeconds,
    CombatSide Attacker,
    AttackKind Kind,
    bool Hit,
    bool Critical,
    int Damage,
    double ElementMultiplier,
    int TargetHpAfter,
    string? SpecialAttackName) : BattleEvent(AtSeconds);

public sealed record DefeatEvent(double AtSeconds, CombatSide Loser) : BattleEvent(AtSeconds);

public sealed record TimeoutEvent(double AtSeconds, int Rounds, int DamageDealt) : BattleEvent(AtSeconds);

public sealed record RewardEvent(double AtSeconds, long Exp, long Money, long JobExp) : BattleEvent(AtSeconds);

public sealed record LevelUpEvent(double AtSeconds, int NewLevel, BaseStats Growth) : BattleEvent(AtSeconds);

public sealed record RestEvent(double AtSeconds, long Cost, bool ExpPenalty, double WaitSeconds) : BattleEvent(AtSeconds);
