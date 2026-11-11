using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Battle;

// AtSeconds 是相對於這批事件起點的秒數；API 的回應型別另外定義
public abstract record BattleEvent(double AtSeconds);

public sealed record EncounterEvent(
    double AtSeconds,
    string MonsterId,
    int MonsterHealthPoints,
    int MonsterMaxHealthPoints,
    int PlayerHealthPoints,
    int PlayerMaxHealthPoints,
    int PlayerManaPoints,
    int PlayerMaxManaPoints,
    string? PetName = null,
    string? PetMonsterId = null) : BattleEvent(AtSeconds);

public sealed record RoundEvent(double AtSeconds, int Round) : BattleEvent(AtSeconds);

// 未命中時 Damage 為 0、ElementMultiplier 為 1；Stunned 表示攻方暈眩、這次沒有出手
public sealed record AttackEvent(
    double AtSeconds,
    CombatSide Attacker,
    AttackKind Kind,
    bool Hit,
    bool Critical,
    int Damage,
    double ElementMultiplier,
    int TargetHealthPointsAfter,
    bool Stunned = false) : BattleEvent(AtSeconds);

// 寵物代擋怪物的這一下（取代那一下的怪物 AttackEvent，玩家 HP 不變）；Retired 表示代擋量用完、寵物退出這一場
public sealed record PetGuardEvent(double AtSeconds, string PetName, int Damage, int GuardAfter, bool Retired) : BattleEvent(AtSeconds);

// 落空時 Damage 為 0、Critical 為 false
public sealed record PetAttackEvent(double AtSeconds, string PetName, AttackKind Kind, bool Hit, bool Critical, int Damage, int TargetHealthPointsAfter)
    : BattleEvent(AtSeconds);

// 捕捉道具損壞或捕到寵物時才產生，緊接在同一場的 RewardEvent 之後；CapturedMonsterId 為 null 表示沒捕到，Remaining 是擲完後的剩餘數量
public sealed record CaptureEvent(double AtSeconds, string ItemName, bool ItemBroken, string? CapturedMonsterId, int Remaining) : BattleEvent(AtSeconds);

// Heal、ManaPointsGain 是實際回復的 HP 與 MP，沒有回復時為 0
// Damage 是對對手造成的傷害（削減 HP、持續傷害），OpponentHealthPointsAfter 是對手剩下的 HP（沒有傷害時為 null）；OpponentManaPointsLoss 是削減對手的 MP；
// HealthPointsCost、ManaPointsCost、MoneyCost 是這次發動扣掉的消耗；Lasting 表示這是已發動技能的持續傷害結算，不是新的發動
public sealed record SkillEvent(
    double AtSeconds, CombatSide Caster, string SkillId, string SkillName, int Level, int ManaPointsAfter, int Heal, int Damage = 0, int? OpponentHealthPointsAfter = null,
    int HealthPointsCost = 0, int ManaPointsCost = 0, int MoneyCost = 0, int OpponentManaPointsLoss = 0, int ManaPointsGain = 0, bool Lasting = false)
    : BattleEvent(AtSeconds);

// HealthPoints／ManaPoints 是實際回復量（已扣掉超過上限的部分）；Remaining 是喝完後的剩餘數量
public sealed record PotionEvent(double AtSeconds, string ItemId, string Name, int HealthPoints, int ManaPoints, int Remaining) : BattleEvent(AtSeconds);

public sealed record DefeatEvent(double AtSeconds, CombatSide Loser) : BattleEvent(AtSeconds);

// 玩家逃跑成功，這一場就此結束（WOG end_date「逃跑成功」）
public sealed record EscapeEvent(double AtSeconds) : BattleEvent(AtSeconds);

public sealed record TimeoutEvent(double AtSeconds, int Rounds, int DamageDealt) : BattleEvent(AtSeconds);

// Drops 依擲骰順序；Discarded 表示背包該分類已滿而被丟棄
public sealed record RewardEvent(double AtSeconds, long Experience, long Money, long JobExperience, IReadOnlyList<DropResult>? Drops = null) : BattleEvent(AtSeconds)
{
    public IReadOnlyList<DropResult> Drops { get; init; } = Drops ?? [];
}

public sealed record DropResult(string ItemId, string Name, int Quantity, bool Discarded);

public sealed record LevelUpEvent(double AtSeconds, int NewLevel, BaseStats Growth) : BattleEvent(AtSeconds);

public sealed record RestEvent(double AtSeconds, long Cost, bool ExperiencePenalty, double WaitSeconds) : BattleEvent(AtSeconds);

// 遇到 NPC：不戰鬥、沒有報酬；任務目標未滿時，後面緊接 MissionProgressEvent。Message 是原版訊息，可以沒有
public sealed record NpcEncounterEvent(double AtSeconds, string MonsterId, string Name, string? Message) : BattleEvent(AtSeconds);

// 擊殺任務目標後的新進度；緊接在同一場的 RewardEvent 之後。RequiresDelivery：擊殺滿了仍要交付才能回報；Completed：所有擊殺目標都滿了；SkillTooLow：擊殺滿了也要技能等級夠才能回報
public sealed record MissionProgressEvent(
    double AtSeconds, string MissionId, string Title, string MonsterId, int Progress, int Required, bool RequiresDelivery, bool Completed, bool SkillTooLow) : BattleEvent(AtSeconds);
