import type { BattleEvent } from "./types"

export type StageState = {
  monsterId: string | null
  monsterHealth: number
  monsterMaxHealth: number
  playerHealth: number
  playerMaxHealth: number
}

export type StageEffect =
  | { type: "slash" }
  | { type: "damageNumber"; side: "player" | "monster"; value: number; critical: boolean }
  | { type: "seal"; text: string }
  | { type: "hitShake"; side: "player" | "monster" }
  | { type: "inkSplash" }

export const initialStageState: StageState = {
  monsterId: null,
  monsterHealth: 0,
  monsterMaxHealth: 0,
  playerHealth: 0,
  playerMaxHealth: 0,
}

export const applyToStage = (state: StageState, event: BattleEvent): StageState => {
  switch (event.type) {
    case "encounter":
      return {
        monsterId: event.monsterId,
        monsterHealth: event.monsterMaxHealth,
        monsterMaxHealth: event.monsterMaxHealth,
        playerHealth: event.playerMaxHealth,
        playerMaxHealth: event.playerMaxHealth,
      }
    case "attack":
      return event.attacker === "Player"
        ? { ...state, monsterHealth: event.targetHealthAfter }
        : { ...state, playerHealth: event.targetHealthAfter }
    case "defeat":
    case "reward":
      return state
  }
}

export const stageEffectsFor = (event: BattleEvent): StageEffect[] => {
  switch (event.type) {
    case "attack": {
      if (event.attacker === "Monster") {
        return [
          { type: "damageNumber", side: "player", value: event.damage, critical: event.isCritical },
          { type: "hitShake", side: "player" },
        ]
      }
      const effects: StageEffect[] = [
        { type: "slash" },
        { type: "damageNumber", side: "monster", value: event.damage, critical: event.isCritical },
        { type: "hitShake", side: "monster" },
      ]
      if (event.isCritical) effects.push({ type: "seal", text: "會心" })
      return effects
    }
    case "defeat":
      return event.loser === "Monster" ? [{ type: "inkSplash" }] : []
    case "encounter":
    case "reward":
      return []
  }
}
