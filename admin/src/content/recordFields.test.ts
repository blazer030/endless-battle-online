import { describe, expect, it } from "vitest"
import { fromFields, toFields } from "./recordFields"

const sprite = {
  id: "test-sprite",
  name: "測試雪精",
  level: 1,
  boss: false,
  missionId: null,
  subPlaces: [1, 2],
  drops: [{ itemId: "small-supply", topr: 3 }],
}

describe("toFields", () => {
  it("picks an input kind from each value's type and keeps the field order", () => {
    expect(toFields(sprite)).toEqual([
      { name: "id", kind: "text", value: "test-sprite" },
      { name: "name", kind: "text", value: "測試雪精" },
      { name: "level", kind: "number", value: 1 },
      { name: "boss", kind: "boolean", value: false },
      { name: "missionId", kind: "json", value: "null" },
      { name: "subPlaces", kind: "json", value: "[1,2]" },
      { name: "drops", kind: "json", value: '[{"itemId":"small-supply","topr":3}]' },
    ])
  })
})

describe("fromFields", () => {
  it("changes only the edited values and keeps every other field as it was", () => {
    const values = Object.fromEntries(toFields(sprite).map((field) => [field.name, field.value]))

    const result = fromFields(sprite, { ...values, level: 9 })

    expect(result).toEqual({ record: { ...sprite, level: 9 } })
  })

  it("keeps fields the form did not send", () => {
    expect(fromFields(sprite, { level: 2 })).toEqual({ record: { ...sprite, level: 2 } })
  })

  it("parses JSON fields back into values", () => {
    expect(fromFields(sprite, { subPlaces: "[3]", missionId: '"m1"' })).toEqual({
      record: { ...sprite, subPlaces: [3], missionId: "m1" },
    })
  })

  it("reports JSON fields that are not valid JSON", () => {
    expect(fromFields(sprite, { drops: "[{" })).toEqual({ errors: { drops: "不是合法的 JSON" } })
  })

  it("reports number fields that are empty", () => {
    expect(fromFields(sprite, { level: null })).toEqual({ errors: { level: "必須是數字" } })
  })
})
