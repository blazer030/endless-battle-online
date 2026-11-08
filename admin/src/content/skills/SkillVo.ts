import type { ContentElement } from "../shared/elements"

export const skillSlotTypes = ["Active", "Passive"] as const
export type SkillSlotType = (typeof skillSlotTypes)[number]

export const skillTimings = ["BeforeAttack", "AfterHit", "RoundEnd"] as const
export type SkillTiming = (typeof skillTimings)[number]

export const skillStatChoices = [
  "Attack",
  "MagicAttack",
  "Defense",
  "MagicDefense",
  "Agility",
  "Charisma",
  "HitRate",
  "Evasion",
  "PhysicalCriticalChance",
  "MagicCriticalChance",
  "PhysicalCriticalCap",
  "MagicCriticalCap",
  "EscapeRate",
] as const
export type SkillStat = (typeof skillStatChoices)[number]

export const skillBases = [
  "Strength",
  "Intelligence",
  "Vitality",
  "Agility",
  "Charisma",
  "Faith",
  "Level",
  "Attack",
  "MagicAttack",
  "Defense",
  "SkillPoints",
] as const
export type SkillBasis = (typeof skillBases)[number]

export const skillTargets = ["Caster", "Opponent"] as const
export type SkillTarget = (typeof skillTargets)[number]

export const skillAttackKinds = ["Physical", "Magic", "Any"] as const
export type SkillAttackKind = (typeof skillAttackKinds)[number]

export const skillResources = ["HitPoints", "SkillPoints"] as const
export type SkillResource = (typeof skillResources)[number]

export type SkillScalingVo = { readonly basis: SkillBasis; readonly ratio: number }

export type SkillConditionVo =
  | { readonly kind: "Chance" | "HitPointsBelow"; readonly percent: number }
  | { readonly kind: "HitPointsBelowOpponent" }
  | { readonly kind: "StatAbove"; readonly stat: SkillBasis }

export type SkillCostVo =
  | { readonly kind: "HitPoints"; readonly amount?: number; readonly percent?: number }
  | {
      readonly kind: "SkillPoints"
      readonly amount?: number
      readonly percent?: number
      readonly all?: boolean
    }
  | { readonly kind: "Money"; readonly amount: number }

export type SkillEffectVo =
  | {
      readonly kind: "StatChange"
      readonly target: SkillTarget
      readonly stat: SkillStat
      readonly percent?: number
      readonly amount?: number
      readonly scaling?: readonly SkillScalingVo[]
      readonly chance?: number
      readonly rounds: number
    }
  | {
      readonly kind: "BonusDamage"
      readonly attackKind: SkillAttackKind
      readonly scaling: readonly SkillScalingVo[]
      readonly chance?: number
      readonly rounds: number
    }
  | {
      readonly kind: "MultiHit"
      readonly attackKind: SkillAttackKind
      readonly hits: number
      readonly rounds: number
    }
  | {
      readonly kind: "DamageReduction"
      readonly attackKind: SkillAttackKind
      readonly percent: number
      readonly rounds: number
    }
  | {
      readonly kind: "Restore"
      readonly resource: SkillResource
      readonly scaling?: readonly SkillScalingVo[]
      readonly percentOfDamage?: number
      readonly percentOfMaximum?: number
      readonly rounds: number
    }
  | {
      readonly kind: "Stun"
      readonly chance: number
      readonly chanceByStat?: { readonly stat: SkillBasis; readonly percent: number }
      readonly rounds: number
    }
  | { readonly kind: "Silence"; readonly chance: number; readonly rounds: number }
  | {
      readonly kind: "PercentDamage" | "ElementAdvantage"
      readonly percent: number
      readonly rounds: number
    }
  | {
      readonly kind: "DamageOverTime"
      readonly scaling: readonly SkillScalingVo[]
      readonly rounds: number
    }
  | {
      readonly kind: "ReduceSkillPoints"
      readonly amount?: number
      readonly percentOfMaximum?: number
      readonly rounds: number
    }
  | {
      readonly kind: "AttackElement" | "DefenseElement"
      readonly element: ContentElement
      readonly rounds: number
    }

export type SkillLevelVo = {
  readonly level: number
  readonly learningCost: number
  readonly proficiencyRequired: number
  readonly cooldown: number
  readonly conditions: readonly SkillConditionVo[]
  readonly costs: readonly SkillCostVo[]
  readonly effects: readonly SkillEffectVo[]
}

type SkillVoParams = {
  id: string
  name: string
  slotType: SkillSlotType
  timing: SkillTiming
  description: string
  maxLevel: number
  prerequisite: { readonly skillId: string; readonly level: number } | null
  jobIds: readonly string[]
  levels: readonly SkillLevelVo[]
}

export class SkillVo {
  readonly id: string
  readonly name: string
  readonly slotType: SkillSlotType
  readonly timing: SkillTiming
  readonly description: string
  readonly maxLevel: number
  readonly prerequisite: { readonly skillId: string; readonly level: number } | null
  readonly jobIds: readonly string[]
  readonly levels: readonly SkillLevelVo[]

  private constructor(params: SkillVoParams) {
    this.id = params.id
    this.name = params.name
    this.slotType = params.slotType
    this.timing = params.timing
    this.description = params.description
    this.maxLevel = params.maxLevel
    this.prerequisite = params.prerequisite
    this.jobIds = params.jobIds
    this.levels = params.levels
  }

  static create(params: SkillVoParams): SkillVo {
    return new SkillVo(params)
  }
}
