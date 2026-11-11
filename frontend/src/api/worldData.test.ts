import { expect, test } from "vitest"
import { ParseError } from "./parse"
import { WorldConverter } from "./worldData"

const plainDto = {
  id: 1,
  name: "中央平原",
  subPlaces: [
    { index: 1, name: "香茅綠地" },
    { index: 2, name: "綠風草地" },
  ],
  recommended: { minimum: 1, maximum: 10 },
  restricted: false,
  current: true,
  recommendation: "Recommended",
  keys: 0,
  passExpiresAt: null,
}

const restrictedDto = {
  ...plainDto,
  id: 17,
  name: "禁區",
  subPlaces: [],
  recommended: { minimum: 80, maximum: 120 },
  restricted: true,
  current: false,
  recommendation: "None",
  keys: 2,
  passExpiresAt: "2026-11-11T09:00:00Z",
}

const areaDto = { id: "central", name: "中央大陸", places: [plainDto, restrictedDto] }

const parseFailure = (value: unknown): ParseError => {
  try {
    WorldConverter.toVo(value)
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("WorldConverter_ConvertsTheArea", () => {
  const area = WorldConverter.toVo(areaDto)

  expect(area.id).toBe("central")
  expect(area.places[0]).toEqual(plainDto)
  expect(area.places[1].recommended).toEqual({ minimum: 80, maximum: 120 })
  expect(area.places[1].passExpiresAt?.toISOString()).toBe("2026-11-11T09:00:00.000Z")
})

test("WorldConverter_MissingMinimumNamesThePath", () => {
  const place = { ...plainDto, recommended: { maximum: 10 } }

  expect(parseFailure({ ...areaDto, places: [place] }).path).toBe(
    "area.places[0].recommended.minimum",
  )
})

test("WorldConverter_IgnoresAnUnknownFieldOnAPlace", () => {
  const area = WorldConverter.toVo({ ...areaDto, places: [{ ...plainDto, weather: "rain" }] })

  expect(area.places[0]).toEqual(plainDto)
  expect(area.places[0]).not.toHaveProperty("weather")
})
