import { describe, expect, it } from "vitest"
import { nameLength } from "./names"

describe("nameLength", () => {
  it("以 code point 計長度，與後端的 rune 一致", () => {
    expect(nameLength("龍🐉")).toBe(2)
  })

  it("不計前後空白", () => {
    expect(nameLength("  小雪 ")).toBe(2)
  })
})
