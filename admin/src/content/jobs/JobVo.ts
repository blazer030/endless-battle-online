import type { ContentStatKey, ContentStatValue } from "../shared/stats"

export type JobGrowthRowVo = {
  readonly stat: ContentStatKey
  readonly minimum: number
  readonly maximum: number
}

export type JobStatRequirementVo = { readonly stat: ContentStatValue; readonly minimum: number }

type JobVoParams = {
  id: string
  name: string
  tier: number
  parentJobId: string | null
  unlockLevel: number
  proficiencyRequired: number
  statRequirements: readonly JobStatRequirementVo[]
  growth: readonly JobGrowthRowVo[]
}

export class JobVo {
  readonly id: string
  readonly name: string
  readonly tier: number
  readonly parentJobId: string | null
  readonly unlockLevel: number
  readonly proficiencyRequired: number
  readonly statRequirements: readonly JobStatRequirementVo[]
  // 七項能力各一列，順序固定
  readonly growth: readonly JobGrowthRowVo[]

  private constructor(params: JobVoParams) {
    this.id = params.id
    this.name = params.name
    this.tier = params.tier
    this.parentJobId = params.parentJobId
    this.unlockLevel = params.unlockLevel
    this.proficiencyRequired = params.proficiencyRequired
    this.statRequirements = params.statRequirements
    this.growth = params.growth
  }

  static create(params: JobVoParams): JobVo {
    return new JobVo(params)
  }
}
