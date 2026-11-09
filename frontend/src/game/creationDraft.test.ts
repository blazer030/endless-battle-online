import { describe, expect, test } from "vitest"
import {
  acceptName,
  adjustStat,
  canConfirmStats,
  confirmStats,
  emptyDraft,
  isReady,
  pointsLeft,
  previewStats,
  toCommand,
  type CreationDraft,
} from "./creationDraft"

const twelve = "一二三四五六七八九十甲乙"

// spec title-and-character-creation 的 name length 範例表
describe("acceptName", () => {
  test.each([
    ["艾", null],
    ["  艾倫  ", "艾倫"],
    [twelve, twelve],
    [`${twelve}丙`, null],
  ])("CreationDraft_Name %s", (input, expected) => {
    const draft = acceptName(emptyDraft, input)
    expect(draft?.name ?? null).toBe(expected)
  })
})

const allocate = (draft: CreationDraft, stat: "str" | "smart" | "life" | "agi", times: number) =>
  Array.from({ length: times }).reduce<CreationDraft>((next) => adjustStat(next, stat, 1), draft)

test("CreationDraft_StartsWithOnePointEachAndSixLeft", () => {
  expect(emptyDraft.allocation).toEqual({ str: 1, smart: 1, life: 1, agi: 1 })
  expect(pointsLeft(emptyDraft)).toBe(6)
})

test("CreationDraft_StatsConfirmableOnlyWhenAllPointsAllocated", () => {
  const partial = allocate(emptyDraft, "str", 2)
  expect(pointsLeft(partial)).toBe(4)
  expect(canConfirmStats(partial)).toBe(false)
  expect(confirmStats(partial).statsConfirmed).toBe(false)

  const full = allocate(partial, "life", 4)
  expect(pointsLeft(full)).toBe(0)
  expect(canConfirmStats(full)).toBe(true)
  expect(confirmStats(full).statsConfirmed).toBe(true)
})

test("CreationDraft_StatsStayWithinOneAndThePointsLeft", () => {
  expect(adjustStat(emptyDraft, "str", -1).allocation.str).toBe(1)
  const full = allocate(emptyDraft, "agi", 6)
  expect(adjustStat(full, "str", 1).allocation).toEqual(full.allocation)
})

test("CreationDraft_ChangingStatsAfterConfirmingNeedsConfirmingAgain", () => {
  const confirmed = confirmStats(allocate(emptyDraft, "str", 6))

  const changed = adjustStat(confirmed, "str", -1)

  expect(changed.statsConfirmed).toBe(false)
})

test("CreationDraft_ReadyOnlyWhenAllSixRowsFilled", () => {
  const filled: CreationDraft = {
    ...confirmStats(allocate(emptyDraft, "smart", 6)),
    name: "艾倫",
    sex: 1,
    birth: 2,
    element: 3,
    jobId: "job-7",
  }
  expect(isReady(filled)).toBe(true)

  for (const missing of ["name", "sex", "birth", "element", "jobId"] as const)
    expect(isReady({ ...filled, [missing]: null }), missing).toBe(false)
  expect(isReady({ ...filled, statsConfirmed: false })).toBe(false)

  expect(toCommand(filled)).toEqual({
    name: "艾倫",
    sex: 1,
    birth: 2,
    element: 3,
    jobId: "job-7",
    allocation: { str: 1, smart: 7, life: 1, agi: 1 },
  })
})

// 4/2/3/1：生命 13、智力 12 → HP 375、SP 91（同後端 CharacterEndpointsTests 的全貌角色）
test("CreationDraft_PreviewsHpAndSpWithDeriveStats", () => {
  const draft = allocate(allocate(allocate(emptyDraft, "str", 3), "smart", 1), "life", 2)
  expect(draft.allocation).toEqual({ str: 4, smart: 2, life: 3, agi: 1 })

  expect(previewStats(draft)).toEqual({ hpMax: 375, spMax: 91 })
})
