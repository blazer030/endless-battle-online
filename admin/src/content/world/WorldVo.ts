export class AreaVo {
  readonly id: string
  readonly name: string
  readonly placeIds: readonly number[]

  private constructor(params: { id: string; name: string; placeIds: readonly number[] }) {
    this.id = params.id
    this.name = params.name
    this.placeIds = params.placeIds
  }

  static create(params: { id: string; name: string; placeIds: readonly number[] }): AreaVo {
    return new AreaVo(params)
  }
}

type PlaceVoParams = {
  id: number
  name: string
  subPlaceNames: readonly string[]
  recommendedMinimum: number
  recommendedMaximum: number
  restricted: boolean
}

export class PlaceVo {
  readonly id: number
  readonly name: string
  readonly subPlaceNames: readonly string[]
  readonly recommendedMinimum: number
  readonly recommendedMaximum: number
  readonly restricted: boolean

  private constructor(params: PlaceVoParams) {
    this.id = params.id
    this.name = params.name
    this.subPlaceNames = params.subPlaceNames
    this.recommendedMinimum = params.recommendedMinimum
    this.recommendedMaximum = params.recommendedMaximum
    this.restricted = params.restricted
  }

  static create(params: PlaceVoParams): PlaceVo {
    return new PlaceVo(params)
  }
}
