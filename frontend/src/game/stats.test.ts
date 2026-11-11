import { describe, expect, it } from "vitest"
import { baseStatsFromAllocation, deriveStats, describeTopGrowth } from "./stats"

describe("deriveStats", () => {
  it("deriveStats_MatchesWogFormulas", () => {
    const derived = deriveStats({
      strength: 17,
      intelligence: 12,
      life: 15,
      vitality: 10,
      agility: 14,
      charisma: 10,
      faith: 10,
    })

    expect(derived).toEqual({
      attack: 17,
      magicAttack: 12,
      defense: 10,
      magicDefense: 10,
      maxHealthPoints: 425,
      maxManaPoints: 91,
    })
  })

  // 七項各不相同，換錯欄位就會被抓到
  it("deriveStats_UsesEachStatDistinctly", () => {
    const derived = deriveStats({
      strength: 1,
      intelligence: 2,
      life: 3,
      vitality: 4,
      agility: 5,
      charisma: 6,
      faith: 7,
    })

    expect(derived).toEqual({
      attack: 1,
      magicAttack: 2,
      defense: 4,
      magicDefense: 7,
      maxHealthPoints: 125,
      maxManaPoints: 52,
    })
  })
})

describe("baseStatsFromAllocation", () => {
  it("baseStatsFromAllocation_AddsStartingBonusAndKeepsUnallocatedAtTen", () => {
    const base = baseStatsFromAllocation({ strength: 4, intelligence: 1, life: 3, agility: 2 })

    expect(base).toEqual({
      strength: 14,
      intelligence: 11,
      life: 13,
      vitality: 10,
      agility: 12,
      charisma: 10,
      faith: 10,
    })
  })
})

describe("describeTopGrowth", () => {
  const flat = {
    str: [0, 1],
    smart: [0, 1],
    life: [1, 2],
    vit: [0, 1],
    agi: [0, 1],
    au: [0, 1],
    be: [0, 1],
  }

  it("describeTopGrowth_NamesTheStatWithHighestAverage", () => {
    expect(describeTopGrowth({ ...flat, agi: [2, 3] })).toBe("敏捷 2～3")
  })

  it("describeTopGrowth_GroupsTiedStatsSharingARange", () => {
    expect(describeTopGrowth({ ...flat, str: [2, 3], vit: [2, 3] })).toBe("力量／體質 2～3")
  })

  it("describeTopGrowth_KeepsEachRangeWhenTiedRangesDiffer", () => {
    expect(describeTopGrowth({ ...flat, str: [1, 4], be: [2, 3] })).toBe("力量 1～4／信仰 2～3")
  })
})
