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
            encounter.MonsterHp,
            encounter.MonsterHpMax,
            encounter.PlayerHp,
            encounter.PlayerHpMax,
            encounter.PlayerSp,
            encounter.PlayerSpMax,
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
            attack.TargetHpAfter,
            attack.Stunned),
        SkillEvent skill => new SkillEventResponse(
            skill.AtSeconds,
            skill.Caster,
            skill.SkillId,
            skill.SkillName,
            skill.Level,
            skill.SpAfter,
            skill.Heal,
            skill.Damage,
            skill.OpponentHpAfter,
            skill.HpCost,
            skill.SpCost,
            skill.MoneyCost,
            skill.OpponentSpLoss,
            skill.SpGain,
            skill.Lasting),
        PotionEvent potion => new PotionEventResponse(potion.AtSeconds, potion.ItemId, potion.Name, potion.Hp, potion.Sp, potion.Remaining),
        DefeatEvent defeat => new DefeatEventResponse(defeat.AtSeconds, defeat.Loser),
        TimeoutEvent timeout => new TimeoutEventResponse(timeout.AtSeconds, timeout.Rounds, timeout.DamageDealt),
        RewardEvent reward => new RewardEventResponse(
            reward.AtSeconds,
            reward.Exp,
            reward.Money,
            reward.JobExp,
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
        RestEvent rest => new RestEventResponse(rest.AtSeconds, rest.Cost, rest.ExpPenalty, rest.WaitSeconds),
        MissionProgressEvent mission => new MissionProgressEventResponse(
            mission.AtSeconds,
            mission.MissionId,
            mission.Title,
            mission.MonsterId,
            mission.Progress,
            mission.Required,
            mission.RequiresDelivery,
            mission.Completed,
            mission.SkillTooLow),
        NpcEncounterEvent npc => new NpcEncounterEventResponse(npc.AtSeconds, npc.MonsterId, npc.Name, npc.Message),
        PetGuardEvent guard => new PetGuardEventResponse(guard.AtSeconds, guard.PetName, guard.Damage, guard.GuardAfter, guard.Retired),
        PetAttackEvent petAttack => new PetAttackEventResponse(
            petAttack.AtSeconds, petAttack.PetName, petAttack.Kind, petAttack.Hit, petAttack.Critical, petAttack.Damage, petAttack.TargetHpAfter),
        CaptureEvent capture => new CaptureEventResponse(
            capture.AtSeconds, capture.ItemName, capture.ItemBroken, capture.CapturedMonsterId, capture.Remaining),
        EscapeEvent escape => new EscapeEventResponse(escape.AtSeconds),
        _ => throw new ArgumentOutOfRangeException(nameof(battleEvent), battleEvent.GetType().Name, "沒有對應的事件回應"),
    };
}

// PetName、PetMonsterId 只在帶寵物出戰時才有值
public sealed record EncounterEventResponse(
    double AtSeconds,
    string MonsterId,
    int MonsterHp,
    int MonsterHpMax,
    int PlayerHp,
    int PlayerHpMax,
    int PlayerSp,
    int PlayerSpMax,
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
    int TargetHpAfter,
    bool Stunned) : BattleEventResponse(AtSeconds);

// Heal、SpGain 是實際回復量；Damage 是對對手造成的傷害，OpponentHpAfter 沒有傷害時為 null；Lasting 表示持續傷害的結算
public sealed record SkillEventResponse(
    double AtSeconds,
    CombatSide Caster,
    string SkillId,
    string SkillName,
    int Level,
    int SpAfter,
    int Heal,
    int Damage,
    int? OpponentHpAfter,
    int HpCost,
    int SpCost,
    int MoneyCost,
    int OpponentSpLoss,
    int SpGain,
    bool Lasting) : BattleEventResponse(AtSeconds);

// Hp／Sp 是實際回復量；Remaining 是喝完後的剩餘數量
public sealed record PotionEventResponse(double AtSeconds, string ItemId, string Name, int Hp, int Sp, int Remaining) : BattleEventResponse(AtSeconds);

public sealed record DefeatEventResponse(double AtSeconds, CombatSide Loser) : BattleEventResponse(AtSeconds);

public sealed record TimeoutEventResponse(double AtSeconds, int Rounds, int DamageDealt) : BattleEventResponse(AtSeconds);

// Drops 依擲骰順序；Discarded 表示背包該分類已滿而被丟棄
public sealed record RewardEventResponse(double AtSeconds, long Exp, long Money, long JobExp, IReadOnlyList<DropResponse> Drops)
    : BattleEventResponse(AtSeconds);

public sealed record DropResponse(string ItemId, string Name, int Quantity, bool Discarded);

public sealed record LevelUpEventResponse(double AtSeconds, int NewLevel, BaseStatsResponse Growth) : BattleEventResponse(AtSeconds);

public sealed record RestEventResponse(double AtSeconds, long Cost, bool ExpPenalty, double WaitSeconds) : BattleEventResponse(AtSeconds);

// RequiresDelivery：擊殺滿了仍要交付；Completed：擊殺目標都滿了；SkillTooLow：技能等級不夠還不能回報
public sealed record MissionProgressEventResponse(
    double AtSeconds,
    string MissionId,
    string Title,
    string MonsterId,
    int Progress,
    int Required,
    bool RequiresDelivery,
    bool Completed,
    bool SkillTooLow) : BattleEventResponse(AtSeconds);

public sealed record NpcEncounterEventResponse(double AtSeconds, string MonsterId, string Name, string? Message) : BattleEventResponse(AtSeconds);

// Retired 表示代擋量用完、寵物退出這一場
public sealed record PetGuardEventResponse(double AtSeconds, string PetName, int Damage, int GuardAfter, bool Retired) : BattleEventResponse(AtSeconds);

public sealed record PetAttackEventResponse(
    double AtSeconds, string PetName, AttackKind Kind, bool Hit, bool Critical, int Damage, int TargetHpAfter) : BattleEventResponse(AtSeconds);

// CapturedMonsterId 為 null 表示沒捕到
public sealed record CaptureEventResponse(double AtSeconds, string ItemName, bool ItemBroken, string? CapturedMonsterId, int Remaining)
    : BattleEventResponse(AtSeconds);

public sealed record EscapeEventResponse(double AtSeconds) : BattleEventResponse(AtSeconds);
