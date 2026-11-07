import type { AreaDto, PlaceDto } from "./WorldConverter"

export const placeDto = (overrides: Partial<PlaceDto> = {}): PlaceDto => ({
  id: 1,
  name: "中央平原",
  subPlaceNames: ["香茅綠地", "綠風草地"],
  recommendedLevel: { minimum: 1, maximum: 10 },
  restricted: false,
  ...overrides,
})

export const places = [
  placeDto(),
  placeDto({ id: 2, name: "灼熱荒漠", recommendedLevel: { minimum: 20, maximum: 30 } }),
  placeDto({ id: 3, name: "古代禁區", restricted: true }),
]

export const areas: AreaDto[] = [
  { id: "central", name: "中央大陸", placeIds: [1] },
  { id: "south", name: "南方大陸", placeIds: [2, 3] },
]
