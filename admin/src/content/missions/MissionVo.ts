type MissionVoParams = { id: string; title: string; daily: boolean }

// 唯讀：只給怪物的「限定任務」選單用
export class MissionVo {
  readonly id: string
  readonly title: string
  readonly daily: boolean

  private constructor(params: MissionVoParams) {
    this.id = params.id
    this.title = params.title
    this.daily = params.daily
  }

  static create(params: MissionVoParams): MissionVo {
    return new MissionVo(params)
  }
}
