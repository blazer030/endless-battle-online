// 冒險推進的結果（來自 /api/adventure/advance），由 api/adventureData.ts 轉換
import type { AttackKind, BaseStats, CharacterVo } from "./character"

export const combatSides = ["Player", "Monster"] as const

export type CombatSide = (typeof combatSides)[number]

// atSeconds 是相對於這批事件起點的秒數
type EventBase<Type extends string> = { readonly type: Type; readonly atSeconds: number }

// petName、petMonsterId 只在帶寵物出戰時才有值
export type EncounterEvent = EventBase<"encounter"> & {
  readonly monsterId: string
  readonly monsterName: string
  readonly monsterHealthPoints: number
  readonly monsterMaxHealthPoints: number
  readonly playerHealthPoints: number
  readonly playerMaxHealthPoints: number
  readonly playerManaPoints: number
  readonly playerMaxManaPoints: number
  readonly petName: string | null
  readonly petMonsterId: string | null
}

export type RoundEvent = EventBase<"round"> & { readonly round: number }

// 未命中時 damage 為 0；stunned 表示攻方暈眩、這次沒有出手
export type AttackEvent = EventBase<"attack"> & {
  readonly attacker: CombatSide
  readonly kind: AttackKind
  readonly hit: boolean
  readonly critical: boolean
  readonly damage: number
  readonly elementMultiplier: number
  readonly targetHealthPointsAfter: number
  readonly stunned: boolean
}

// opponentHealthPointsAfter 在沒有傷害時為 null；lasting 表示持續傷害的結算
export type SkillEvent = EventBase<"skill"> & {
  readonly caster: CombatSide
  readonly skillId: string
  readonly skillName: string
  readonly level: number
  readonly manaPointsAfter: number
  readonly heal: number
  readonly damage: number
  readonly opponentHealthPointsAfter: number | null
  readonly healthPointsCost: number
  readonly manaPointsCost: number
  readonly moneyCost: number
  readonly opponentManaPointsLoss: number
  readonly manaPointsGain: number
  readonly lasting: boolean
}

// healthPoints、manaPoints 是實際回復量；remaining 是喝完後的剩餘數量
export type PotionEvent = EventBase<"potion"> & {
  readonly itemId: string
  readonly name: string
  readonly healthPoints: number
  readonly manaPoints: number
  readonly remaining: number
}

export type DefeatEvent = EventBase<"defeat"> & { readonly loser: CombatSide }

export type TimeoutEvent = EventBase<"timeout"> & {
  readonly rounds: number
  readonly damageDealt: number
}

// discarded 表示背包該分類已滿而被丟棄
export type Drop = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
  readonly discarded: boolean
}

export type RewardEvent = EventBase<"reward"> & {
  readonly experience: number
  readonly money: number
  readonly jobExperience: number
  readonly drops: readonly Drop[]
}

export type LevelUpEvent = EventBase<"levelUp"> & {
  readonly newLevel: number
  readonly growth: BaseStats
}

export type RestEvent = EventBase<"rest"> & {
  readonly cost: number
  readonly experiencePenalty: boolean
  readonly waitSeconds: number
}

export type MissionProgressEvent = EventBase<"mission"> & {
  readonly missionId: string
  readonly title: string
  readonly monsterId: string
  readonly monsterName: string
  readonly progress: number
  readonly required: number
  readonly requiresDelivery: boolean
  readonly completed: boolean
  readonly skillTooLow: boolean
}

// message 是原版訊息，可以沒有
export type NpcEncounterEvent = EventBase<"npc"> & {
  readonly monsterId: string
  readonly name: string
  readonly message: string | null
}

// retired 表示代擋量用完、寵物退出這一場
export type PetGuardEvent = EventBase<"petGuard"> & {
  readonly petName: string
  readonly damage: number
  readonly guardAfter: number
  readonly retired: boolean
}

export type PetAttackEvent = EventBase<"petAttack"> & {
  readonly petName: string
  readonly kind: AttackKind
  readonly hit: boolean
  readonly critical: boolean
  readonly damage: number
  readonly targetHealthPointsAfter: number
}

// capturedMonsterId、capturedMonsterName 為 null 表示沒捕到
export type CaptureEvent = EventBase<"capture"> & {
  readonly itemName: string
  readonly itemBroken: boolean
  readonly capturedMonsterId: string | null
  readonly capturedMonsterName: string | null
  readonly remaining: number
}

export type EscapeEvent = EventBase<"escape">

export type BattleEvent =
  | EncounterEvent
  | RoundEvent
  | AttackEvent
  | SkillEvent
  | PotionEvent
  | DefeatEvent
  | TimeoutEvent
  | RewardEvent
  | LevelUpEvent
  | RestEvent
  | MissionProgressEvent
  | NpcEncounterEvent
  | PetGuardEvent
  | PetAttackEvent
  | CaptureEvent
  | EscapeEvent

export type AdvanceSummary = {
  readonly fights: number
  readonly wins: number
  readonly losses: number
  readonly timeouts: number
  readonly experience: number
  readonly money: number
  readonly jobExperience: number
  readonly levelsGained: number
  readonly skillsCast: number
  readonly drops: number
  readonly potionsUsed: number
  readonly missionKills: number
  readonly petsCaptured: number
  readonly escapes: number
}

export type MercenaryFight = {
  readonly at: Date
  readonly monsterId: string
  readonly monsterName: string
  readonly won: boolean
  readonly experience: number
  readonly money: number
}

// fights 是這次推進的場數；recent 是最後 5 場；dismissed 表示次數用完、已解除僱用
export type MercenaryReport = {
  readonly name: string
  readonly fights: number
  readonly wins: number
  readonly experience: number
  readonly money: number
  readonly levelsGained: number
  readonly remaining: number
  readonly dismissed: boolean
  readonly recent: readonly MercenaryFight[]
}

type AdvanceVoParams = {
  events: BattleEvent[]
  summary: AdvanceSummary
  eventsCoverAll: boolean
  character: CharacterVo
  mercenary: MercenaryReport | null
  areaPassExpiresAt: Date | null
}

export class AdvanceVo {
  // 只含最後 30 秒開始的場次與住宿，其餘只計入 summary
  readonly events: readonly BattleEvent[]
  readonly summary: AdvanceSummary
  // false 表示有戰鬥被略去
  readonly eventsCoverAll: boolean
  readonly character: CharacterVo
  // 只在這次推進有傭兵戰鬥時才有值
  readonly mercenary: MercenaryReport | null
  // 只在目前地區是禁區且有通行證時有值（可能已過期）
  readonly areaPassExpiresAt: Date | null

  private constructor(params: AdvanceVoParams) {
    this.events = params.events
    this.summary = params.summary
    this.eventsCoverAll = params.eventsCoverAll
    this.character = params.character
    this.mercenary = params.mercenary
    this.areaPassExpiresAt = params.areaPassExpiresAt
  }

  static create(params: AdvanceVoParams): AdvanceVo {
    return new AdvanceVo(params)
  }
}
