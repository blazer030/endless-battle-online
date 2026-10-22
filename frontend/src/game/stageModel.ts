import type { AttackEvent, BattleEvent } from "./types"

export type StageState = {
  monsterId: string | null
  monsterHp: number
  monsterHpMax: number
  playerHp: number
  playerHpMax: number
}

export type StageSide = "player" | "monster"

export type StageEffect =
  | { type: "slash" }
  | { type: "beam" }
  | { type: "damageNumber"; side: StageSide; value: number; critical: boolean }
  | { type: "miss"; side: StageSide }
  // over 省略時名牌落在舞台中央
  | { type: "plate"; text: string; tone: "gold" | "muted" | "cyan"; over?: StageSide }
  | { type: "hitShake"; side: StageSide }
  | { type: "impactBurst" }
  | { type: "standOff" }
  | { type: "levelUpFlash" }
  | { type: "skillFlash" }
  | { type: "potionFlash" }
  | { type: "dropFloat"; text: string }
  | { type: "fade"; seconds: number }

export const initialStageState: StageState = {
  monsterId: null,
  monsterHp: 0,
  monsterHpMax: 0,
  playerHp: 0,
  playerHpMax: 0,
}

// 必殺技名缺漏時的備用字
const CriticalPlateText = "CRITICAL"

export const applyToStage = (state: StageState, event: BattleEvent): StageState => {
  switch (event.type) {
    case "encounter":
      return {
        monsterId: event.monsterId,
        monsterHp: event.monsterHp,
        monsterHpMax: event.monsterHpMax,
        playerHp: event.playerHp,
        playerHpMax: event.playerHpMax,
      }
    case "attack":
      return event.attacker === "Player"
        ? { ...state, monsterHp: event.targetHpAfter }
        : { ...state, playerHp: event.targetHpAfter }
    case "skill":
      return event.caster === "Player" && event.heal > 0
        ? { ...state, playerHp: Math.min(state.playerHpMax, state.playerHp + event.heal) }
        : state
    case "potion":
      return { ...state, playerHp: Math.min(state.playerHpMax, state.playerHp + event.hp) }
    case "rest":
      return { ...state, playerHp: state.playerHpMax }
    case "round":
    case "defeat":
    case "timeout":
    case "reward":
    case "levelUp":
      return state
  }
}

const attackEffects = (event: AttackEvent): StageEffect[] => {
  if (event.attacker === "Monster") {
    if (event.stunned) return [{ type: "plate", text: "暈眩", tone: "muted", over: "monster" }]
    return event.hit
      ? [
          { type: "damageNumber", side: "player", value: event.damage, critical: event.critical },
          { type: "hitShake", side: "player" },
        ]
      : [{ type: "miss", side: "player" }]
  }
  const stroke: StageEffect = event.kind === "Magic" ? { type: "beam" } : { type: "slash" }
  if (!event.hit) return [stroke, { type: "miss", side: "monster" }]
  const effects: StageEffect[] = [
    stroke,
    { type: "damageNumber", side: "monster", value: event.damage, critical: event.critical },
    { type: "hitShake", side: "monster" },
  ]
  if (event.critical) {
    effects.push({
      type: "plate",
      text: event.specialAttackName ?? CriticalPlateText,
      tone: "gold",
    })
  }
  return effects
}

export const stageEffectsFor = (event: BattleEvent): StageEffect[] => {
  switch (event.type) {
    case "attack":
      return attackEffects(event)
    case "skill":
      return event.caster === "Player"
        ? [{ type: "skillFlash" }, { type: "plate", text: event.skillName, tone: "cyan" }]
        : []
    case "defeat":
      return event.loser === "Monster" ? [{ type: "impactBurst" }] : []
    case "timeout":
      return [{ type: "standOff" }, { type: "plate", text: "膠著", tone: "muted" }]
    case "levelUp":
      return [{ type: "levelUpFlash" }, { type: "plate", text: "LEVEL UP", tone: "gold" }]
    case "rest":
      return [{ type: "fade", seconds: event.waitSeconds }]
    case "potion":
      return [{ type: "potionFlash" }]
    case "reward":
      return (event.drops ?? [])
        .filter((drop) => !drop.discarded)
        .map((drop) => ({ type: "dropFloat", text: drop.name }))
    case "encounter":
    case "round":
      return []
  }
}
