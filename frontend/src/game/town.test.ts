import { describe, expect, test } from "vitest"
import { remainingText, type BuffVo } from "./buffs"
import type { InnRoom, Lodging } from "./inn"
import { timeOfDay } from "./timeOfDay"
import {
  bonusRows,
  expiredSince,
  isEndingSoon,
  panoramaOffsets,
  swipeTarget,
  type TownSegment,
} from "./town"

const now = new Date("2026-11-12T08:00:00Z")
const later = (seconds: number) => new Date(now.getTime() + seconds * 1000)

// 規格 Town panorama segments：offsets at the stage bounds
test.each([
  [624, [0, 452, 728]],
  [780, [0, 614, 1002]],
])("PanoramaOffsets_AtStageHeight%i", (stageHeight, expected) => {
  expect(panoramaOffsets(stageHeight)).toEqual(expected)
})

// 規格 Change segment by sign or swipe：swipe outcomes
test.each<[TownSegment, number, number, TownSegment]>([
  [0, -60, 10, 1],
  [1, -60, 10, 2],
  [2, -60, 10, 2],
  [1, 60, 10, 0],
  [0, 60, 10, 0],
  [0, -39, 0, 0],
  [0, -60, 70, 0],
])("SwipeTarget_From%iBy%iAnd%i", (segment, deltaX, deltaY, expected) => {
  expect(swipeTarget(segment, deltaX, deltaY)).toBe(expected)
})

// 規格 Bonus row and list：remaining time
test.each([
  [6130, "01:42", false],
  [3600, "01:00", false],
  [3599, "59:59", false],
  [600, "10:00", false],
  [599, "09:59", true],
  [30, "00:30", true],
])("RemainingText_%is", (seconds, text, endingSoon) => {
  expect(remainingText(later(seconds), now)).toBe(text)
  expect(isEndingSoon(later(seconds), now)).toBe(endingSoon)
})

// 規格 Town background follows the time of day：沿用標題的 timeOfDay（本機小時）
test.each([
  [4, 59, "night"],
  [5, 0, "dawn"],
  [8, 0, "day"],
  [16, 59, "day"],
  [17, 0, "dusk"],
  [19, 0, "night"],
])("TimeOfDay_At%i:%i", (hours, minutes, expected) => {
  expect(timeOfDay(new Date(2026, 10, 12, hours, minutes).getHours())).toBe(expected)
})

const elixir = (overrides: Partial<BuffVo>): BuffVo => ({
  kind: "Attack",
  multiplier: 1.05,
  startsAt: later(-1200),
  endsAt: later(6000),
  itemId: "item-2257",
  itemName: "神力秘藥LV1",
  ...overrides,
})

const loft: InnRoom = {
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Experience",
  multiplier: 1.1,
  seconds: 28800,
  price: 100,
}
const lodging: Lodging = {
  roomId: "apprentice-loft",
  startsAt: later(-14400),
  endsAt: later(14400),
}

describe("bonusRows", () => {
  test("BonusRows_NamesTheItemOrTheKind", () => {
    const rows = bonusRows(
      [elixir({}), elixir({ kind: "Defense", itemId: null, itemName: null })],
      null,
      [],
      now,
    )

    expect(rows.map(({ name, effect }) => ({ name, effect }))).toEqual([
      { name: "神力秘藥LV1", effect: "物攻 ×1.05" },
      { name: "物防 ×1.05", effect: null },
    ])
  })

  test("BonusRows_PutsLodgingFirstThenKindOrder", () => {
    const rows = bonusRows(
      [
        elixir({ kind: "MagicDefense", itemName: "精神秘藥LV1" }),
        elixir({}),
        elixir({ kind: "JobExperience", multiplier: 1.5, itemName: "技能熟練手冊LV1" }),
      ],
      lodging,
      [loft],
      now,
    )

    expect(rows.map(({ name, effect, isLodging }) => [name, effect, isLodging])).toEqual([
      ["學徒閣樓", "經驗 ×1.10", true],
      ["技能熟練手冊LV1", "熟練度 ×1.5", false],
      ["神力秘藥LV1", "物攻 ×1.05", false],
      ["精神秘藥LV1", "魔防 ×1.05", false],
    ])
  })

  test("BonusRows_ShowsRemainingAndProgress", () => {
    const [row] = bonusRows(
      [elixir({ startsAt: later(-5400), endsAt: later(1800) })],
      null,
      [],
      now,
    )

    expect(row.remaining).toBe("30:00")
    expect(row.endingSoon).toBe(false)
    expect(row.progress).toBe(0.25)
  })

  test("BonusRows_LeavesOutExpiredAndUnknownRooms", () => {
    const rows = bonusRows(
      [elixir({ endsAt: now })],
      { ...lodging, roomId: "gone-room" },
      [loft],
      now,
    )

    expect(rows).toEqual([])
    expect(bonusRows([], { ...lodging, endsAt: now }, [loft], now)).toEqual([])
  })
})

// 規格 Bonus countdown and refresh：讀進來時還有效、現在已到期的才要重新讀取；讀進來時就已到期的住宿不算
test("ExpiredSince_OnlyCountsBonusesActiveAtLoad", () => {
  const loadedAt = now
  const buffs = [elixir({ endsAt: later(60) })]
  const staleLodging = { ...lodging, endsAt: later(-60) }

  expect(expiredSince(buffs, staleLodging, loadedAt, later(30))).toBe(false)
  expect(expiredSince(buffs, staleLodging, loadedAt, later(60))).toBe(true)
  expect(expiredSince([], { ...lodging, endsAt: later(90) }, loadedAt, later(90))).toBe(true)
})
