// 建角可選的性別、屬性、出生地（來自 /api/public/settings），id 就是送給建角 API 的值
export type CreationOption = { readonly id: number; readonly name: string }

// strongAgainst／weakAgainst 是屬性 id：攻擊時剋制的對象、會被剋制的對象
export type ElementOption = CreationOption & {
  readonly strongAgainst: number
  readonly weakAgainst: number
}

type CreationOptionsVoParams = {
  sexes: CreationOption[]
  elements: ElementOption[]
  births: CreationOption[]
}

export class CreationOptionsVo {
  readonly sexes: readonly CreationOption[]
  readonly elements: readonly ElementOption[]
  readonly births: readonly CreationOption[]

  private constructor(params: CreationOptionsVoParams) {
    this.sexes = params.sexes
    this.elements = params.elements
    this.births = params.births
  }

  static create(params: CreationOptionsVoParams): CreationOptionsVo {
    return new CreationOptionsVo(params)
  }

  elementName(id: number): string | undefined {
    return this.elements.find((element) => element.id === id)?.name
  }
}
