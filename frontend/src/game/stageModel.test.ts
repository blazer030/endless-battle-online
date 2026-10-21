import { describe, expect, it } from "vitest"
import { applyToStage, initialStageState, stageEffectsFor } from "./stageModel"
import type { StageState } from "./stageModel"
import type { AttackEvent, BattleEvent, SkillEvent } from "./types"

const encounter: BattleEvent = {
  type: "encounter",
  monsterId: "snow-sprite",
  monsterHp: 90,
  monsterHpMax: 100,
  playerHp: 150,
  playerHpMax: 200,
  playerSp: 10,
  playerSpMax: 20,
  staminaDepleted: false,
  atSeconds: 0,
}

const attack = (overrides: Partial<AttackEvent> = {}): BattleEvent => ({
  type: "attack",
  attacker: "Player",
  kind: "Physical",
  hit: true,
  critical: false,
  damage: 30,
  elementMultiplier: 1,
  targetHpAfter: 70,
  specialAttackName: null,
  stunned: false,
  atSeconds: 1,
  ...overrides,
})

const skill = (overrides: Partial<SkillEvent> = {}): BattleEvent => ({
  type: "skill",
  caster: "Player",
  skillId: "burst",
  skillName: "爆氣",
  level: 1,
  spAfter: 3,
  heal: 0,
  atSeconds: 1,
  ...overrides,
})

const midFight: StageState = {
  monsterId: "frost-wolf",
  monsterHp: 5,
  monsterHpMax: 50,
  playerHp: 12,
  playerHpMax: 80,
}

describe("stageModel", () => {
  it("applyToStage_EncounterSetsBothHealthBarsFromEvent", () => {
    expect(applyToStage(midFight, encounter)).toEqual({
      monsterId: "snow-sprite",
      monsterHp: 90,
      monsterHpMax: 100,
      playerHp: 150,
      playerHpMax: 200,
    })
  })

  it("applyToStage_AttackUpdatesTargetHealth", () => {
    const started = applyToStage(initialStageState, encounter)

    const afterPlayer = applyToStage(started, attack())
    expect(afterPlayer.monsterHp).toBe(70)
    expect(afterPlayer.playerHp).toBe(150)

    const afterMonster = applyToStage(started, attack({ attacker: "Monster" }))
    expect(afterMonster.playerHp).toBe(70)
    expect(afterMonster.monsterHp).toBe(90)
    expect(started.monsterHp).toBe(90)
  })

  it("applyToStage_RestRefillsPlayerHealth", () => {
    const rest: BattleEvent = {
      type: "rest",
      cost: 0,
      expPenalty: false,
      waitSeconds: 15,
      atSeconds: 3,
    }
    expect(applyToStage({ ...midFight, playerHp: 0 }, rest)).toEqual({
      ...midFight,
      playerHp: 80,
    })
  })

  it("applyToStage_RoundTimeoutLevelUpLeaveStateUnchanged", () => {
    const events: BattleEvent[] = [
      { type: "round", round: 2, atSeconds: 1 },
      { type: "timeout", rounds: 100, damageDealt: 3, atSeconds: 80 },
      {
        type: "levelUp",
        newLevel: 2,
        growth: { str: 1, smart: 0, life: 0, vit: 0, agi: 0, au: 0, be: 0 },
        atSeconds: 2,
      },
    ]
    for (const event of events) expect(applyToStage(midFight, event)).toBe(midFight)
  })

  it("applyToStage_HealSkillRaisesPlayerHealthUpToMax", () => {
    expect(applyToStage(midFight, skill({ heal: 30 })).playerHp).toBe(42)
    expect(applyToStage(midFight, skill({ heal: 500 })).playerHp).toBe(80)
    expect(applyToStage(midFight, skill())).toBe(midFight)
  })

  it("stageEffectsFor_PlayerSkill_FlashesCyanWithSkillPlate", () => {
    expect(stageEffectsFor(skill())).toEqual([
      { type: "skillFlash" },
      { type: "plate", text: "爆氣", tone: "cyan" },
    ])
  })

  it("stageEffectsFor_MonsterSkill_HasNoEffects", () => {
    expect(stageEffectsFor(skill({ caster: "Monster" }))).toEqual([])
  })

  it("stageEffectsFor_StunnedMonster_ShowsMutedPlateOverMonster", () => {
    const stunned = attack({ attacker: "Monster", hit: false, damage: 0, stunned: true })
    expect(stageEffectsFor(stunned)).toEqual([
      { type: "plate", text: "暈眩", tone: "muted", over: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerPhysicalHit_SlashesMonster", () => {
    expect(stageEffectsFor(attack())).toEqual([
      { type: "slash" },
      { type: "damageNumber", side: "monster", value: 30, critical: false },
      { type: "hitShake", side: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerMagicHit_UsesBeamInsteadOfSlash", () => {
    expect(stageEffectsFor(attack({ kind: "Magic" }))).toEqual([
      { type: "beam" },
      { type: "damageNumber", side: "monster", value: 30, critical: false },
      { type: "hitShake", side: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerCritical_ShowsSpecialAttackPlate", () => {
    expect(stageEffectsFor(attack({ critical: true, specialAttackName: "星落" }))).toEqual([
      { type: "slash" },
      { type: "damageNumber", side: "monster", value: 30, critical: true },
      { type: "hitShake", side: "monster" },
      { type: "plate", text: "星落", tone: "gold" },
    ])
  })

  it("stageEffectsFor_PlayerCriticalWithoutName_FallsBackToCriticalPlate", () => {
    expect(stageEffectsFor(attack({ critical: true, specialAttackName: null }))).toContainEqual({
      type: "plate",
      text: "CRITICAL",
      tone: "gold",
    })
  })

  it("stageEffectsFor_PlayerMiss_ShowsMissOverMonster", () => {
    expect(stageEffectsFor(attack({ hit: false, damage: 0 }))).toEqual([
      { type: "slash" },
      { type: "miss", side: "monster" },
    ])
  })

  it("stageEffectsFor_MonsterAttack_ShakesPlayer", () => {
    expect(stageEffectsFor(attack({ attacker: "Monster" }))).toEqual([
      { type: "damageNumber", side: "player", value: 30, critical: false },
      { type: "hitShake", side: "player" },
    ])
  })

  it("stageEffectsFor_MonsterMiss_ShowsMissOverPlayer", () => {
    expect(stageEffectsFor(attack({ attacker: "Monster", hit: false, damage: 0 }))).toEqual([
      { type: "miss", side: "player" },
    ])
  })

  it("stageEffectsFor_MonsterDefeat_PlaysImpactBurst", () => {
    expect(stageEffectsFor({ type: "defeat", loser: "Monster", atSeconds: 2 })).toEqual([
      { type: "impactBurst" },
    ])
  })

  it("stageEffectsFor_Timeout_StepsBackWithMutedPlate", () => {
    expect(
      stageEffectsFor({ type: "timeout", rounds: 100, damageDealt: 3, atSeconds: 80 }),
    ).toEqual([{ type: "standOff" }, { type: "plate", text: "膠著", tone: "muted" }])
  })

  it("stageEffectsFor_LevelUp_FlashesPlayerWithPlate", () => {
    const levelUp: BattleEvent = {
      type: "levelUp",
      newLevel: 2,
      growth: { str: 1, smart: 0, life: 0, vit: 0, agi: 0, au: 0, be: 0 },
      atSeconds: 2,
    }
    expect(stageEffectsFor(levelUp)).toEqual([
      { type: "levelUpFlash" },
      { type: "plate", text: "LEVEL UP", tone: "gold" },
    ])
  })

  it("stageEffectsFor_Rest_FadesStageOverWait", () => {
    const rest: BattleEvent = {
      type: "rest",
      cost: 0,
      expPenalty: false,
      waitSeconds: 15,
      atSeconds: 3,
    }
    expect(stageEffectsFor(rest)).toEqual([{ type: "fade", seconds: 15 }])
  })

  it("stageEffectsFor_EncounterRoundReward_HaveNoEffects", () => {
    const reward: BattleEvent = { type: "reward", exp: 5, money: 3, jobExp: 1, atSeconds: 2 }
    const round: BattleEvent = { type: "round", round: 1, atSeconds: 0 }
    for (const event of [encounter, round, reward]) expect(stageEffectsFor(event)).toEqual([])
  })
})
