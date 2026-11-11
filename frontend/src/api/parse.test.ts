import { expect, test } from "vitest"
import { ParseError, parseBoolean, parseNullableNumber, parseOneOf, parseTimestamp } from "./parse"

const failure = (parse: () => unknown): ParseError => {
  try {
    parse()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("parseBoolean_ReturnsTheBoolean", () => {
  expect(parseBoolean(true, "flag")).toBe(true)
  expect(parseBoolean(false, "flag")).toBe(false)
})

test("parseBoolean_RejectsOtherTypesWithThePath", () => {
  const error = failure(() => parseBoolean("true", "character.current"))
  expect(error.path).toBe("character.current")
  expect(error.message).toBe("API 回應的 character.current 應該是布林值")
})

test("parseNullableNumber_AcceptsNullAndNumbers", () => {
  expect(parseNullableNumber(null, "subPlace")).toBeNull()
  expect(parseNullableNumber(2, "subPlace")).toBe(2)
})

test("parseNullableNumber_RejectsMissingValue", () => {
  expect(failure(() => parseNullableNumber(undefined, "character.subPlace")).path).toBe(
    "character.subPlace",
  )
})

test("parseOneOf_ReturnsAKnownValue", () => {
  expect(parseOneOf("Magic", ["Physical", "Magic"] as const, "attack")).toBe("Magic")
})

test("parseOneOf_RejectsAnUnknownValue", () => {
  const error = failure(() => parseOneOf("Fire", ["Physical", "Magic"] as const, "event.kind"))
  expect(error.path).toBe("event.kind")
  expect(error.message).toBe("API 回應的 event.kind 應該是Physical、Magic 之一")
})

test("parseTimestamp_ReturnsTheDate", () => {
  expect(parseTimestamp("2026-11-11T08:30:00+00:00", "expiresAt").toISOString()).toBe(
    "2026-11-11T08:30:00.000Z",
  )
})

test("parseTimestamp_RejectsTextThatIsNotADate", () => {
  expect(failure(() => parseTimestamp("tomorrow", "lodging.endsAt")).path).toBe("lodging.endsAt")
  expect(failure(() => parseTimestamp(0, "lodging.endsAt")).path).toBe("lodging.endsAt")
})
