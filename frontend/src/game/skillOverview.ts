// 技能頁的技能與轉職資料（來自 /api/skills、/api/jobs），由 api/skillData.ts 轉換
import type { GrowthRange, GrowthStatKey, SkillPrerequisite, StatRequirement } from "./jobBook"

export const skillTypes = ["Active", "Passive"] as const
export type SkillType = (typeof skillTypes)[number]

export const skillTimings = ["BeforeAttack", "AfterHit", "RoundEnd"] as const
export type SkillTiming = (typeof skillTimings)[number]

export type SkillNextLevel = {
  readonly level: number
  readonly money: number
  readonly proficiency: number
  readonly summary: string
  readonly canLearn: boolean
  readonly reasons: readonly string[]
}

// level 0 表示尚未學習；summary 是目前等級的效果文字（未學為 null）；next 在已滿級時為 null
export type Skill = {
  readonly id: string
  readonly name: string
  readonly slotType: SkillType
  readonly timing: SkillTiming
  readonly description: string
  readonly icon: string | null
  readonly level: number
  readonly maxLevel: number
  readonly summary: string | null
  readonly prerequisite: SkillPrerequisite | null
  readonly next: SkillNextLevel | null
}

export type SkillSlot = { readonly slot: number; readonly skillId: string }

// 設定技能欄時送出的一欄；空欄不送
export type SkillSlotCommand = { readonly slot: number; readonly skillId: string }

type SkillOverviewVoParams = {
  jobId: string
  skillPoints: number
  skills: Skill[]
  slots: SkillSlot[]
}

export class SkillOverviewVo {
  readonly jobId: string
  readonly skillPoints: number
  readonly skills: readonly Skill[]
  readonly slots: readonly SkillSlot[]

  private constructor(params: SkillOverviewVoParams) {
    this.jobId = params.jobId
    this.skillPoints = params.skillPoints
    this.skills = params.skills
    this.slots = params.slots
  }

  static create(params: SkillOverviewVoParams): SkillOverviewVo {
    return new SkillOverviewVo(params)
  }
}

type JobChoiceVoParams = {
  id: string
  name: string
  tier: number
  parentId: string | null
  parentName: string | null
  unlockLevel: number
  proficiencyRequired: number
  statRequirements: StatRequirement[]
  growth: Record<GrowthStatKey, GrowthRange>
  current: boolean
  proficiencyPercent: number
  eligible: boolean
  reasons: string[]
}

// 轉職頁的一個職業：目前職業、熟練度與能不能轉職的判定
export class JobChoiceVo {
  readonly id: string
  readonly name: string
  readonly tier: number
  readonly parentId: string | null
  readonly parentName: string | null
  readonly unlockLevel: number
  readonly proficiencyRequired: number
  readonly statRequirements: readonly StatRequirement[]
  readonly growth: Readonly<Record<GrowthStatKey, GrowthRange>>
  readonly current: boolean
  readonly proficiencyPercent: number
  readonly eligible: boolean
  readonly reasons: readonly string[]

  private constructor(params: JobChoiceVoParams) {
    this.id = params.id
    this.name = params.name
    this.tier = params.tier
    this.parentId = params.parentId
    this.parentName = params.parentName
    this.unlockLevel = params.unlockLevel
    this.proficiencyRequired = params.proficiencyRequired
    this.statRequirements = params.statRequirements
    this.growth = params.growth
    this.current = params.current
    this.proficiencyPercent = params.proficiencyPercent
    this.eligible = params.eligible
    this.reasons = params.reasons
  }

  static create(params: JobChoiceVoParams): JobChoiceVo {
    return new JobChoiceVo(params)
  }
}
