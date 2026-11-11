using System.Text.Json.Serialization;
using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Api.Endpoints;

// 前端用同一串事件同時驅動對戰舞台與戰報；AtSeconds 是相對於這批事件起點的秒數
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EncounterEventResponse), "encounter")]
[JsonDerivedType(typeof(RoundEventResponse), "round")]
[JsonDerivedType(typeof(AttackEventResponse), "attack")]
[JsonDerivedType(typeof(SkillEventResponse), "skill")]
[JsonDerivedType(typeof(PotionEventResponse), "potion")]
[JsonDerivedType(typeof(DefeatEventResponse), "defeat")]
[JsonDerivedType(typeof(TimeoutEventResponse), "timeout")]
[JsonDerivedType(typeof(RewardEventResponse), "reward")]
[JsonDerivedType(typeof(LevelUpEventResponse), "levelUp")]
[JsonDerivedType(typeof(RestEventResponse), "rest")]
[JsonDerivedType(typeof(MissionProgressEventResponse), "mission")]
[JsonDerivedType(typeof(NpcEncounterEventResponse), "npc")]
[JsonDerivedType(typeof(PetGuardEventResponse), "petGuard")]
[JsonDerivedType(typeof(PetAttackEventResponse), "petAttack")]
[JsonDerivedType(typeof(CaptureEventResponse), "capture")]
[JsonDerivedType(typeof(EscapeEventResponse), "escape")]
public abstract record BattleEventResponse(double AtSeconds)
{
    public static BattleEventResponse From(BattleEvent battleEvent) => battleEvent switch
    {
        EncounterEvent encounter => new EncounterEventResponse(
            encounter.AtSeconds,
            encounter.MonsterId,
            encounter.MonsterName,
            encounter.MonsterHealthPoints,
            encounter.MonsterMaxHealthPoints,
            encounter.PlayerHealthPoints,
            encounter.PlayerMaxHealthPoints,
            encounter.PlayerManaPoints,
            encounter.PlayerMaxManaPoints,
            encounter.PetName,
            encounter.PetMonsterId),
        RoundEvent round => new RoundEventResponse(round.AtSeconds, round.Round),
        AttackEvent attack => new AttackEventResponse(
            attack.AtSeconds,
            attack.Attacker,
            attack.Kind,
            attack.Hit,
            attack.Critical,
            attack.Damage,
            attack.ElementMultiplier,
            attack.TargetHealthPointsAfter,
            attack.Stunned),
        SkillEvent skill => new SkillEventResponse(
            skill.AtSeconds,
            skill.Caster,
            skill.SkillId,
            skill.SkillName,
            skill.Level,
            skill.ManaPointsAfter,
            skill.Heal,
            skill.Damage,
            skill.OpponentHealthPointsAfter,
            skill.HealthPointsCost,
            skill.ManaPointsCost,
            skill.MoneyCost,
            skill.OpponentManaPointsLoss,
            skill.ManaPointsGain,
            skill.Lasting),
        PotionEvent potion => new PotionEventResponse(potion.AtSeconds, potion.ItemId, potion.Name, potion.HealthPoints, potion.ManaPoints, potion.Remaining),
        DefeatEvent defeat => new DefeatEventResponse(defeat.AtSeconds, defeat.Loser),
        TimeoutEvent timeout => new TimeoutEventResponse(timeout.AtSeconds, timeout.Rounds, timeout.DamageDealt),
        RewardEvent reward => new RewardEventResponse(
            reward.AtSeconds,
            reward.Experience,
            reward.Money,
            reward.JobExperience,
            reward.Drops.Select(drop => new DropResponse(drop.ItemId, drop.Name, drop.Quantity, drop.Discarded)).ToList()),
        LevelUpEvent levelUp => new LevelUpEventResponse(
            levelUp.AtSeconds,
            levelUp.NewLevel,
            new BaseStatsResponse(
                levelUp.Growth.Strength,
                levelUp.Growth.Intelligence,
                levelUp.Growth.Life,
                levelUp.Growth.Vitality,
                levelUp.Growth.Agility,
                levelUp.Growth.Charisma,
                levelUp.Growth.Faith)),
        RestEvent rest => new RestEventResponse(rest.AtSeconds, rest.Cost, rest.ExperiencePenalty, rest.WaitSeconds),
        MissionProgressEvent mission => new MissionProgressEventResponse(
            mission.AtSeconds,
            mission.MissionId,
            mission.Title,
            mission.MonsterId,
            mission.MonsterName,
            mission.Progress,
            mission.Required,
            mission.RequiresDelivery,
            mission.Completed,
            mission.SkillTooLow),
        NpcEncounterEvent npc => new NpcEncounterEventResponse(npc.AtSeconds, npc.MonsterId, npc.Name, npc.Message),
        PetGuardEvent guard => new PetGuardEventResponse(guard.AtSeconds, guard.PetName, guard.Damage, guard.GuardAfter, guard.Retired),
        PetAttackEvent petAttack => new PetAttackEventResponse(
            petAttack.AtSeconds, petAttack.PetName, petAttack.Kind, petAttack.Hit, petAttack.Critical, petAttack.Damage, petAttack.TargetHealthPointsAfter),
        CaptureEvent capture => new CaptureEventResponse(
            capture.AtSeconds, capture.ItemName, capture.ItemBroken, capture.CapturedMonsterId, capture.CapturedMonsterName, capture.Remaining),
        EscapeEvent escape => new EscapeEventResponse(escape.AtSeconds),
        _ => throw new ArgumentOutOfRangeException(nameof(battleEvent), battleEvent.GetType().Name, "沒有對應的事件回應"),
    };
}

// PetName、PetMonsterId 只在帶寵物出戰時才有值
public sealed record EncounterEventResponse(
    double AtSeconds,
    string MonsterId,
    string MonsterName,
    int MonsterHealthPoints,
    int MonsterMaxHealthPoints,
    int PlayerHealthPoints,
    int PlayerMaxHealthPoints,
    int PlayerManaPoints,
    int PlayerMaxManaPoints,
    string? PetName,
    string? PetMonsterId) : BattleEventResponse(AtSeconds);

public sealed record RoundEventResponse(double AtSeconds, int Round) : BattleEventResponse(AtSeconds);

// 未命中時 Damage 為 0、ElementMultiplier 為 1；Stunned 表示攻方暈眩、這次沒有出手
public sealed record AttackEventResponse(
    double AtSeconds,
    CombatSide Attacker,
    AttackKind Kind,
    bool Hit,
    bool Critical,
    int Damage,
    double ElementMultiplier,
    int TargetHealthPointsAfter,
    bool Stunned) : BattleEventResponse(AtSeconds);

// Heal、ManaPointsGain 是實際回復量；Damage 是對對手造成的傷害，OpponentHealthPointsAfter 沒有傷害時為 null；Lasting 表示持續傷害的結算
public sealed record SkillEventResponse(
    double AtSeconds,
    CombatSide Caster,
    string SkillId,
    string SkillName,
    int Level,
    int ManaPointsAfter,
    int Heal,
    int Damage,
    int? OpponentHealthPointsAfter,
    int HealthPointsCost,
    int ManaPointsCost,
    int MoneyCost,
    int OpponentManaPointsLoss,
    int ManaPointsGain,
    bool Lasting) : BattleEventResponse(AtSeconds);

// HealthPoints／ManaPoints 是實際回復量；Remaining 是喝完後的剩餘數量
public sealed record PotionEventResponse(double AtSeconds, string ItemId, string Name, int HealthPoints, int ManaPoints, int Remaining) : BattleEventResponse(AtSeconds);

public sealed record DefeatEventResponse(double AtSeconds, CombatSide Loser) : BattleEventResponse(AtSeconds);

public sealed record TimeoutEventResponse(double AtSeconds, int Rounds, int DamageDealt) : BattleEventResponse(AtSeconds);

// Drops 依擲骰順序；Discarded 表示背包該分類已滿而被丟棄
public sealed record RewardEventResponse(double AtSeconds, long Experience, long Money, long JobExperience, IReadOnlyList<DropResponse> Drops)
    : BattleEventResponse(AtSeconds);

public sealed record DropResponse(string ItemId, string Name, int Quantity, bool Discarded);

public sealed record LevelUpEventResponse(double AtSeconds, int NewLevel, BaseStatsResponse Growth) : BattleEventResponse(AtSeconds);

public sealed record RestEventResponse(double AtSeconds, long Cost, bool ExperiencePenalty, double WaitSeconds) : BattleEventResponse(AtSeconds);

// RequiresDelivery：擊殺滿了仍要交付；Completed：擊殺目標都滿了；SkillTooLow：技能等級不夠還不能回報
public sealed record MissionProgressEventResponse(
    double AtSeconds,
    string MissionId,
    string Title,
    string MonsterId,
    string MonsterName,
    int Progress,
    int Required,
    bool RequiresDelivery,
    bool Completed,
    bool SkillTooLow) : BattleEventResponse(AtSeconds);

public sealed record NpcEncounterEventResponse(double AtSeconds, string MonsterId, string Name, string? Message) : BattleEventResponse(AtSeconds);

// Retired 表示代擋量用完、寵物退出這一場
public sealed record PetGuardEventResponse(double AtSeconds, string PetName, int Damage, int GuardAfter, bool Retired) : BattleEventResponse(AtSeconds);

public sealed record PetAttackEventResponse(
    double AtSeconds, string PetName, AttackKind Kind, bool Hit, bool Critical, int Damage, int TargetHealthPointsAfter) : BattleEventResponse(AtSeconds);

// CapturedMonsterId、CapturedMonsterName 為 null 表示沒捕到
public sealed record CaptureEventResponse(
    double AtSeconds, string ItemName, bool ItemBroken, string? CapturedMonsterId, string? CapturedMonsterName, int Remaining)
    : BattleEventResponse(AtSeconds);

public sealed record EscapeEventResponse(double AtSeconds) : BattleEventResponse(AtSeconds);
