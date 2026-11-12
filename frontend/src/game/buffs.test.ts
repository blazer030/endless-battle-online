import { describe, expect, it } from "vitest"
import { buffLabel, buffLines, remainingMinutes } from "./buffs"
import type { BuffVo } from "./buffs"

const now = new Date("2026-10-23T03:00:00Z")
const endingIn = (milliseconds: number) => new Date(now.getTime() + milliseconds)
const buff = (overrides: Partial<BuffVo> = {}): BuffVo => ({
  kind: "Experience",
  multiplier: 1.5,
  startsAt: now,
  endsAt: endingIn(3_600_000),
  itemId: null,
  itemName: null,
  ...overrides,
})

describe("buffLabel", () => {
  it("六種密藥的效果名稱", () => {
    expect(
      (["Experience", "JobExperience", "Attack", "Magic", "Defense", "MagicDefense"] as const).map(
        buffLabel,
      ),
    ).toEqual(["經驗", "熟練度", "物攻", "魔攻", "物防", "魔防"])
  })
})

describe("remainingMinutes", () => {
  it("剩餘分鐘無條件進位", () => {
    expect(remainingMinutes(endingIn(3_600_000), now)).toBe(60)
    expect(remainingMinutes(endingIn(59 * 60_000 + 1), now)).toBe(60)
    expect(remainingMinutes(endingIn(1), now)).toBe(1)
    expect(remainingMinutes(endingIn(30 * 60_000), now)).toBe(30)
  })
})

describe("buffLines", () => {
  it("每列是效果、倍率與剩餘分鐘", () => {
    expect(
      buffLines(
        [buff(), buff({ kind: "MagicDefense", multiplier: 1.2, endsAt: endingIn(90_001) })],
        now,
      ),
    ).toEqual(["經驗 ×1.5（剩 60 分）", "魔防 ×1.2（剩 2 分）"])
  })

  it("到期的不列出：到期那一刻就算到期", () => {
    expect(
      buffLines(
        [buff({ endsAt: endingIn(0) }), buff({ kind: "Attack", endsAt: endingIn(-1) })],
        now,
      ),
    ).toEqual([])
  })
})
