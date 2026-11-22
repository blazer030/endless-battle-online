// 技能頁的技能與轉職資料（來自 /api/skills、/api/jobs），由 api/skillData.ts 轉換
import type { GrowthRange, GrowthStatKey, SkillPrerequisite, StatRequirement } from "./jobBook"

// 轉職頁的能力需求：裸點有沒有達到 minimum
export type JobStatCheck = StatRequirement & { readonly met: boolean }

// 技能卡的前置技能：已學等級有沒有達到 level
export type SkillPrerequisiteCheck = SkillPrerequisite & { readonly met: boolean }

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
  readonly enoughMoney: boolean
  readonly enoughProficiency: boolean
}

// level 0 表示尚未學習；summary 是目前等級的效果文字（未學為 null）；next 在已滿級時為 null；
// taughtByCurrentJob、prerequisite.met、next 的 enoughMoney／enoughProficiency 是各學習條件有沒有達成
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
  readonly taughtByCurrentJob: boolean
  readonly prerequisite: SkillPrerequisiteCheck | null
  readonly next: SkillNextLevel | null
}

// 角色頁技能卡的一列數值：發動、消耗、效果、冷卻（列名由畫面決定）
export const skillDetailKinds = ["Trigger", "Cost", "Effect", "Cooldown"] as const
export type SkillDetailKind = (typeof skillDetailKinds)[number]
export type SkillDetailRow = { readonly kind: SkillDetailKind; readonly text: string }

export type LearnableSkillNextLevel = {
  readonly level: number
  readonly money: number
  readonly skillPoints: number
  readonly rows: readonly SkillDetailRow[]
  readonly canLearn: boolean
  readonly enoughMoney: boolean
  readonly enoughSkillPoints: boolean
}

// 角色頁技能頁的一個技能：rows 是目前等級的數值列（未學為 null），next 在滿級時為 null，都用所屬職業的技能點判斷
export type LearnableSkill = {
  readonly id: string
  readonly name: string
  readonly slotType: SkillType
  readonly timing: SkillTiming
  readonly description: string
  readonly icon: string | null
  readonly level: number
  readonly maxLevel: number
  readonly rows: readonly SkillDetailRow[] | null
  readonly prerequisite: SkillPrerequisiteCheck | null
  readonly next: LearnableSkillNextLevel | null
}

// 目前職業（列全部技能）或以前當過的職業（只列已學的）；skillPoints 是這個職業的技能點
export type SkillJob = {
  readonly jobId: string
  readonly jobName: string
  readonly tier: number
  readonly current: boolean
  readonly skillPoints: number
  readonly skills: readonly LearnableSkill[]
}

export type SkillSlot = { readonly slot: number; readonly skillId: string }

// 設定技能欄時送出的一欄；空欄不送
export type SkillSlotCommand = { readonly slot: number; readonly skillId: string }

type SkillOverviewVoParams = {
  jobId: string
  skillPoints: number
  skills: Skill[]
  slots: SkillSlot[]
  jobs: SkillJob[]
}

export class SkillOverviewVo {
  readonly jobId: string
  readonly skillPoints: number
  readonly skills: readonly Skill[]
  readonly slots: readonly SkillSlot[]
  readonly jobs: readonly SkillJob[]

  private constructor(params: SkillOverviewVoParams) {
    this.jobId = params.jobId
    this.skillPoints = params.skillPoints
    this.skills = params.skills
    this.slots = params.slots
    this.jobs = params.jobs
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
  levelMet: boolean
  proficiencyRequired: number
  parentProficiencyMet: boolean
  statRequirements: JobStatCheck[]
  growth: Record<GrowthStatKey, GrowthRange>
  current: boolean
  proficiencyPercent: number
  eligible: boolean
}

// 轉職頁的一個職業：目前職業、熟練度、每個轉職條件有沒有達成
export class JobChoiceVo {
  readonly id: string
  readonly name: string
  readonly tier: number
  readonly parentId: string | null
  readonly parentName: string | null
  readonly unlockLevel: number
  readonly levelMet: boolean
  readonly proficiencyRequired: number
  readonly parentProficiencyMet: boolean
  readonly statRequirements: readonly JobStatCheck[]
  readonly growth: Readonly<Record<GrowthStatKey, GrowthRange>>
  readonly current: boolean
  readonly proficiencyPercent: number
  readonly eligible: boolean

  private constructor(params: JobChoiceVoParams) {
    this.id = params.id
    this.name = params.name
    this.tier = params.tier
    this.parentId = params.parentId
    this.parentName = params.parentName
    this.unlockLevel = params.unlockLevel
    this.levelMet = params.levelMet
    this.proficiencyRequired = params.proficiencyRequired
    this.parentProficiencyMet = params.parentProficiencyMet
    this.statRequirements = params.statRequirements
    this.growth = params.growth
    this.current = params.current
    this.proficiencyPercent = params.proficiencyPercent
    this.eligible = params.eligible
  }

  static create(params: JobChoiceVoParams): JobChoiceVo {
    return new JobChoiceVo(params)
  }
}
