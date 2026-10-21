import { describe, expect, it } from "vitest"
import { applyToStage, initialStageState, stageEffectsFor } from "./stageModel"
import type { StageState } from "./stageModel"
import type { AttackEvent, BattleEvent } from "./types"

const encounter: BattleEvent = {
  type: "encounter",
  monsterId: "snow-sprite",
  monsterMaxHealth: 100,
  playerMaxHealth: 200,
  atSeconds: 0,
}

const attack = (attacker: AttackEvent["attacker"], isCritical = false): BattleEvent => ({
  type: "attack",
  attacker,
  damage: 30,
  isCritical,
  targetHealthAfter: 70,
  atSeconds: 1,
})

const midFight: StageState = {
  monsterId: "frost-wolf",
  monsterHealth: 5,
  monsterMaxHealth: 50,
  playerHealth: 12,
  playerMaxHealth: 80,
}

describe("stageModel", () => {
  it("applyToStage_EncounterResetsBothHealthBars", () => {
    expect(applyToStage(midFight, encounter)).toEqual({
      monsterId: "snow-sprite",
      monsterHealth: 100,
      monsterMaxHealth: 100,
      playerHealth: 200,
      playerMaxHealth: 200,
    })
  })

  it("applyToStage_AttackUpdatesTargetHealth", () => {
    const started = applyToStage(initialStageState, encounter)

    const afterPlayer = applyToStage(started, attack("Player"))
    expect(afterPlayer.monsterHealth).toBe(70)
    expect(afterPlayer.playerHealth).toBe(200)

    const afterMonster = applyToStage(started, attack("Monster"))
    expect(afterMonster.playerHealth).toBe(70)
    expect(afterMonster.monsterHealth).toBe(100)
    expect(started.monsterHealth).toBe(100)
  })

  it("stageEffectsFor_PlayerCritical_IncludesPlate", () => {
    expect(stageEffectsFor(attack("Player", true))).toEqual([
      { type: "slash" },
      { type: "damageNumber", side: "monster", value: 30, critical: true },
      { type: "hitShake", side: "monster" },
      { type: "plate", text: "CRITICAL" },
    ])
  })

  it("stageEffectsFor_MonsterAttack_ShakesPlayer", () => {
    expect(stageEffectsFor(attack("Monster"))).toEqual([
      { type: "damageNumber", side: "player", value: 30, critical: false },
      { type: "hitShake", side: "player" },
    ])
  })

  it("stageEffectsFor_MonsterDefeat_PlaysImpactBurst", () => {
    expect(stageEffectsFor({ type: "defeat", loser: "Monster", atSeconds: 2 })).toEqual([
      { type: "impactBurst" },
    ])
  })

  it("stageEffectsFor_Reward_HasNoEffects", () => {
    const reward: BattleEvent = { type: "reward", experience: 5, spiritStones: 3, atSeconds: 2 }
    expect(stageEffectsFor(reward)).toEqual([])
  })
})
