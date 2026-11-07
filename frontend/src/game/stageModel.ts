import type { AttackEvent, BattleEvent, PetAttackEvent, PetGuardEvent } from "./types"

export type StageState = {
  monsterId: string | null
  monsterHp: number
  monsterHpMax: number
  playerHp: number
  playerHpMax: number
  petName: string | null
  petMonsterId: string | null
  petActive: boolean
}

export type StageSide = "player" | "monster" | "pet"

export type StageEffect =
  | { type: "slash" }
  | { type: "beam" }
  | { type: "damageNumber"; side: StageSide; value: number; critical: boolean }
  | { type: "miss"; side: StageSide }
  // over 省略時名牌落在舞台中央；指定時落在該方頭頂，standing 時落在怪物站的高度
  | {
      type: "plate"
      text: string
      tone: "gold" | "muted" | "cyan"
      over?: StageSide
      standing?: boolean
    }
  | { type: "hitShake"; side: StageSide }
  | { type: "impactBurst" }
  | { type: "standOff" }
  | { type: "levelUpFlash" }
  | { type: "skillFlash" }
  | { type: "potionFlash" }
  | { type: "dropFloat"; text: string }
  | { type: "petRetire" }
  | { type: "monsterFade" }
  | { type: "clearMonster" }
  | { type: "fade"; seconds: number }

export const initialStageState: StageState = {
  monsterId: null,
  monsterHp: 0,
  monsterHpMax: 0,
  playerHp: 0,
  playerHpMax: 0,
  petName: null,
  petMonsterId: null,
  petActive: false,
}

export const applyToStage = (state: StageState, event: BattleEvent): StageState => {
  switch (event.type) {
    case "encounter":
      return {
        monsterId: event.monsterId,
        monsterHp: event.monsterHp,
        monsterHpMax: event.monsterHpMax,
        playerHp: event.playerHp,
        playerHpMax: event.playerHpMax,
        petName: event.petName ?? null,
        petMonsterId: event.petMonsterId ?? null,
        petActive: event.petName != null,
      }
    case "attack":
      return event.attacker === "Player"
        ? { ...state, monsterHp: event.targetHpAfter }
        : { ...state, playerHp: event.targetHpAfter }
    case "petGuard":
      return event.retired ? { ...state, petActive: false } : state
    case "petAttack":
      return { ...state, monsterHp: event.targetHpAfter }
    case "skill": {
      // 施放者扣消耗、加回復；削減 HP、持續傷害時對手改成剩下的 HP
      const change = event.heal - event.hpCost
      const casterChanged =
        change === 0
          ? state
          : event.caster === "Player"
            ? { ...state, playerHp: Math.min(state.playerHpMax, state.playerHp + change) }
            : { ...state, monsterHp: Math.min(state.monsterHpMax, state.monsterHp + change) }
      if (event.opponentHpAfter === null || event.opponentHpAfter === undefined)
        return casterChanged
      return event.caster === "Player"
        ? { ...casterChanged, monsterHp: event.opponentHpAfter }
        : { ...casterChanged, playerHp: event.opponentHpAfter }
    }
    case "potion":
      return { ...state, playerHp: Math.min(state.playerHpMax, state.playerHp + event.hp) }
    case "rest":
      return { ...state, playerHp: state.playerHpMax }
    case "round":
    case "defeat":
    case "timeout":
    case "reward":
    case "levelUp":
    case "mission":
    case "capture":
    case "escape":
      return state
    case "npc":
      return { ...state, monsterId: null, monsterHp: 0, monsterHpMax: 0 }
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
  if (event.stunned) return [{ type: "plate", text: "暈眩", tone: "muted", over: "player" }]
  const stroke: StageEffect = event.kind === "Magic" ? { type: "beam" } : { type: "slash" }
  if (!event.hit) return [stroke, { type: "miss", side: "monster" }]
  return [
    stroke,
    { type: "damageNumber", side: "monster", value: event.damage, critical: event.critical },
    { type: "hitShake", side: "monster" },
  ]
}

const petGuardEffects = (event: PetGuardEvent): StageEffect[] => {
  const effects: StageEffect[] = [
    { type: "damageNumber", side: "pet", value: event.damage, critical: false },
    { type: "hitShake", side: "pet" },
  ]
  if (event.retired) effects.push({ type: "petRetire" })
  return effects
}

const petAttackEffects = (event: PetAttackEvent): StageEffect[] =>
  event.hit
    ? [
        { type: "damageNumber", side: "monster", value: event.damage, critical: event.critical },
        { type: "hitShake", side: "monster" },
      ]
    : [{ type: "miss", side: "monster" }]

export const stageEffectsFor = (event: BattleEvent): StageEffect[] => {
  switch (event.type) {
    case "attack":
      return attackEffects(event)
    case "skill":
      // 持續傷害的結算不是施放，只扣對手 HP；怪物施放只有名牌，全畫面閃光留給玩家
      if (event.lasting) return []
      return event.caster === "Player"
        ? [{ type: "skillFlash" }, { type: "plate", text: event.skillName, tone: "cyan" }]
        : [{ type: "plate", text: event.skillName, tone: "cyan", over: "monster" }]
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
    case "mission":
      // 還有其他目標、要交付或技能等級不足的任務，這個目標滿了不算達成
      return event.completed && !event.requiresDelivery && !event.skillTooLow
        ? [{ type: "plate", text: "任務達成", tone: "gold" }]
        : []
    case "npc":
      return [
        { type: "clearMonster" },
        { type: "plate", text: event.name, tone: "cyan", over: "monster", standing: true },
      ]
    case "petGuard":
      return petGuardEffects(event)
    case "petAttack":
      return petAttackEffects(event)
    case "capture":
      return event.capturedMonsterId === null
        ? []
        : [{ type: "plate", text: "捕捉成功", tone: "gold" }]
    case "escape":
      return [{ type: "monsterFade" }]
    case "encounter":
    case "round":
      return []
  }
}
