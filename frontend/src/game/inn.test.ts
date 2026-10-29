import { expect, test } from "vitest"
import { lodgingLine, replaceHint, roomEffect } from "./inn"
import type { BuffKind, InnRoomView } from "./types"

const room = (overrides: Partial<InnRoomView> = {}): InnRoomView => ({
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Exp",
  multiplier: 1.1,
  seconds: 28_800,
  price: 600,
  ...overrides,
})

const now = new Date("2026-10-30T00:00:00Z")
const endsIn = (milliseconds: number) => new Date(now.getTime() + milliseconds).toISOString()

test("roomEffect_LabelsEachOfTheSixKinds", () => {
  const labels: [BuffKind, string][] = [
    ["Exp", "經驗 ×1.10"],
    ["JobExp", "熟練度 ×1.10"],
    ["Attack", "物攻 ×1.10"],
    ["Magic", "魔攻 ×1.10"],
    ["Defense", "物防 ×1.10"],
    ["MagicDefense", "魔防 ×1.10"],
  ]
  for (const [kind, text] of labels) expect(roomEffect(room({ kind }))).toBe(text)
  expect(roomEffect(room({ multiplier: 1.05 }))).toBe("經驗 ×1.05")
})

test("lodgingLine_ShowsRemainingHoursAndMinutes", () => {
  const lodging = { roomId: "apprentice-loft", endsAt: endsIn(7 * 3_600_000 + 5 * 60_000) }

  expect(lodgingLine(lodging, [room()], now)).toBe("學徒閣樓・經驗 ×1.10（剩 7:05）")
})

test("lodgingLine_RoundsRemainingMinutesUp", () => {
  const lodging = { roomId: "apprentice-loft", endsAt: endsIn(59 * 60_000 + 1) }

  expect(lodgingLine(lodging, [room()], now)).toBe("學徒閣樓・經驗 ×1.10（剩 1:00）")
})

test("lodgingLine_IsNullWithoutLodgingExpiredOrUnknownRoom", () => {
  const live = { roomId: "apprentice-loft", endsAt: endsIn(60_000) }

  expect(lodgingLine(null, [room()], now)).toBeNull()
  expect(lodgingLine({ ...live, endsAt: endsIn(0) }, [room()], now)).toBeNull()
  expect(lodgingLine({ ...live, roomId: "gone" }, [room()], now)).toBeNull()
})

test("replaceHint_NamesTheRoomBeingReplaced", () => {
  const lodging = { roomId: "apprentice-loft", endsAt: endsIn(60_000) }
  const rooms = [room(), room({ id: "bulwark-room", name: "鐵壁客房" })]

  expect(replaceHint(lodging, rooms, "bulwark-room", now)).toBe("會取代目前的學徒閣樓")
  expect(replaceHint(lodging, rooms, "apprentice-loft", now)).toBeNull()
  expect(replaceHint(null, rooms, "bulwark-room", now)).toBeNull()
})
