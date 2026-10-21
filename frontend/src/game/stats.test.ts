import { describe, expect, it } from "vitest"
import { baseStatsFromAllocation, deriveStats } from "./stats"

describe("deriveStats", () => {
  it("deriveStats_MatchesWogFormulas", () => {
    const derived = deriveStats({ str: 17, smart: 12, life: 15, vit: 10, agi: 14, au: 10, be: 10 })

    expect(derived).toEqual({ at: 17, mat: 12, df: 10, mdf: 10, hpMax: 425, spMax: 91 })
  })

  // 七項各不相同，換錯欄位就會被抓到
  it("deriveStats_UsesEachStatDistinctly", () => {
    const derived = deriveStats({ str: 1, smart: 2, life: 3, vit: 4, agi: 5, au: 6, be: 7 })

    expect(derived).toEqual({ at: 1, mat: 2, df: 4, mdf: 7, hpMax: 125, spMax: 52 })
  })
})

describe("baseStatsFromAllocation", () => {
  it("baseStatsFromAllocation_AddsStartingBonusAndKeepsUnallocatedAtTen", () => {
    const base = baseStatsFromAllocation({ str: 4, smart: 1, life: 3, agi: 2 })

    expect(base).toEqual({ str: 14, smart: 11, life: 13, vit: 10, agi: 12, au: 10, be: 10 })
  })
})
