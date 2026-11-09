// 職業介紹冊用的職業資料（來自 /api/public/jobs）
export const growthStatKeys = [
  "strength",
  "intelligence",
  "life",
  "vitality",
  "agility",
  "charisma",
  "faith",
] as const

export type GrowthStatKey = (typeof growthStatKeys)[number]

export type GrowthRange = { readonly minimum: number; readonly maximum: number }

export type StatRequirement = { readonly stat: string; readonly minimum: number }

export type SkillPrerequisite = {
  readonly skillId: string
  readonly name: string
  readonly level: number
}

export type SkillLevelSummary = { readonly level: number; readonly summary: string }

export type JobSkill = {
  readonly id: string
  readonly name: string
  readonly description: string
  readonly maxLevel: number
  // 沒有前置技能時為 null
  readonly prerequisite: SkillPrerequisite | null
  // 從 Lv1 到最高級
  readonly levels: readonly SkillLevelSummary[]
}

type JobVoParams = {
  id: string
  name: string
  tier: number
  parentId: string | null
  unlockLevel: number
  proficiencyRequired: number
  statRequirements: StatRequirement[]
  growth: Record<GrowthStatKey, GrowthRange>
  skills: JobSkill[]
}

export class JobVo {
  readonly id: string
  readonly name: string
  readonly tier: number
  // 初階職業為 null
  readonly parentId: string | null
  readonly unlockLevel: number
  readonly proficiencyRequired: number
  readonly statRequirements: readonly StatRequirement[]
  readonly growth: Readonly<Record<GrowthStatKey, GrowthRange>>
  readonly skills: readonly JobSkill[]

  private constructor(params: JobVoParams) {
    this.id = params.id
    this.name = params.name
    this.tier = params.tier
    this.parentId = params.parentId
    this.unlockLevel = params.unlockLevel
    this.proficiencyRequired = params.proficiencyRequired
    this.statRequirements = params.statRequirements
    this.growth = params.growth
    this.skills = params.skills
  }

  static create(params: JobVoParams): JobVo {
    return new JobVo(params)
  }
}
