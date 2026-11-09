import { expect, test } from "vitest"
import { timeOfDay } from "./timeOfDay"

// spec title-and-character-creation 的 hour boundaries 範例表
test.each([
  [4, "night"],
  [5, "dawn"],
  [7, "dawn"],
  [8, "day"],
  [16, "day"],
  [17, "dusk"],
  [18, "dusk"],
  [19, "night"],
  [0, "night"],
  [23, "night"],
] as const)("timeOfDay_%i", (hour, expected) => {
  expect(timeOfDay(hour)).toBe(expected)
})
