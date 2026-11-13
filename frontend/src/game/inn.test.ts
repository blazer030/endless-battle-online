import { expect, test } from "vitest"
import {
  innCounterTop,
  innkeeperLine,
  innRevealOrigin,
  lodgingLine,
  remainingClock,
  stayRemainingClock,
  replaceNotice,
  restFeeText,
  roomEffect,
  stayLength,
  stepThreshold,
} from "./inn"
import type { InnRoom } from "./inn"
import type { BuffKind } from "./buffs"

const room = (overrides: Partial<InnRoom> = {}): InnRoom => ({
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Experience",
  multiplier: 1.1,
  seconds: 28_800,
  price: 600,
  ...overrides,
})

const now = new Date("2026-10-30T00:00:00Z")
const endsIn = (milliseconds: number) => new Date(now.getTime() + milliseconds)

test("roomEffect_LabelsEachOfTheSixKinds", () => {
  const labels: [BuffKind, string][] = [
    ["Experience", "經驗 ×1.10"],
    ["JobExperience", "熟練度 ×1.10"],
    ["Attack", "物攻 ×1.10"],
    ["Magic", "魔攻 ×1.10"],
    ["Defense", "物防 ×1.10"],
    ["MagicDefense", "魔防 ×1.10"],
  ]
  for (const [kind, text] of labels) expect(roomEffect(room({ kind }))).toBe(text)
  expect(roomEffect(room({ multiplier: 1.05 }))).toBe("經驗 ×1.05")
})

// 規格 inn-screen：stay length 表
test.each([
  [28_800, "8 小時"],
  [27_000, "7 小時 30 分"],
  [2_700, "45 分"],
  [86_400, "24 小時"],
])("stayLength_%i", (seconds, text) => {
  expect(stayLength(seconds)).toBe(text)
})

test("lodgingLine_ShowsRemainingHoursAndMinutes", () => {
  const lodging = {
    roomId: "apprentice-loft",
    startsAt: now,
    endsAt: endsIn(7 * 3_600_000 + 5 * 60_000),
  }

  expect(lodgingLine(lodging, [room()], now)).toBe("學徒閣樓・經驗 ×1.10（剩 7:05）")
})

test("lodgingLine_RoundsRemainingMinutesUp", () => {
  const lodging = { roomId: "apprentice-loft", startsAt: now, endsAt: endsIn(59 * 60_000 + 1) }

  expect(lodgingLine(lodging, [room()], now)).toBe("學徒閣樓・經驗 ×1.10（剩 1:00）")
})

test("lodgingLine_IsNullWithoutLodgingExpiredOrUnknownRoom", () => {
  const live = { roomId: "apprentice-loft", startsAt: now, endsAt: endsIn(60_000) }

  expect(lodgingLine(null, [room()], now)).toBeNull()
  expect(lodgingLine({ ...live, endsAt: endsIn(0) }, [room()], now)).toBeNull()
  expect(lodgingLine({ ...live, roomId: "gone" }, [room()], now)).toBeNull()
})

// 規格 inn-screen：innkeeper lines 表
test.each([
  [null, false, "歡迎光臨暖爐亭！今晚想住哪一間？"],
  [null, true, "房間還幫你留著呢。要換一間，還是續住？"],
  [{ kind: "booked", roomName: "學徒閣樓" } as const, true, "學徒閣樓收拾好了，好好睡一覺吧！"],
  [{ kind: "notEnoughMoney" } as const, false, "哎呀，這間的錢好像還差一點呢。"],
  [{ kind: "bookFailed" } as const, false, "咦，登記簿沒寫上去……等會兒再來吧。"],
  [{ kind: "thresholdFailed" } as const, true, "剛剛那個沒記下來，再調一次好嗎？"],
  [{ kind: "loadFailed" } as const, false, "哎呀，今天的房帳一時找不到……"],
])("innkeeperLine_%#", (event, hasStay, line) => {
  expect(innkeeperLine(event, hasStay)).toBe(line)
})

// 規格 inn-screen：threshold steps 表（null＝不能按）
test.each([
  [-1, 0, null],
  [0, 5, -1],
  [33, 35, 30],
  [35, 40, 30],
  [100, null, 95],
])("stepThreshold_%i", (value, up, down) => {
  expect(stepThreshold(value, 1)).toBe(up)
  expect(stepThreshold(value, -1)).toBe(down)
})

// 規格 inn-screen：rest fee text 表
test.each([
  [0, "免費"],
  [1200, "1,200 起"],
])("restFeeText_%i", (fee, text) => {
  expect(restFeeText(fee)).toBe(text)
})

// 規格 inn-screen：reveal origin 表
test.each([
  [624, 135, 241],
  [780, 120, 296],
])("innRevealOrigin_%i", (height, x, y) => {
  const origin = innRevealOrigin(height)
  expect(origin.x).toBeCloseTo(x, 0)
  expect(origin.y).toBeCloseTo(y, 0)
})

// 規格 inn-screen：counter top 表
test.each([
  [624, 162],
  [683, 186],
  [720, 200],
  [780, 224],
])("innCounterTop_%i", (height, top) => {
  expect(innCounterTop(height)).toBe(top)
})

// 規格 inn-screen：Banner for an active stay（08:00–16:00、現在 08:01 → 剩 7:59）
test("remainingClock_ShowsHoursAndMinutes", () => {
  expect(remainingClock(endsIn(479 * 60_000), now)).toBe("7:59")
  expect(remainingClock(endsIn(102 * 60_000), now)).toBe("1:42")
})

// 規格 inn-screen：Replacing another room／同一間
test("replaceNotice_NamesTheRoomOrSaysItIsTheSame", () => {
  const lodging = { roomId: "fighters-bunk", startsAt: now, endsAt: endsIn(102 * 60_000) }
  const rooms = [
    room(),
    room({ id: "fighters-bunk", name: "鬥士通鋪", kind: "Attack", multiplier: 1.05 }),
  ]

  expect(replaceNotice(lodging, rooms, "apprentice-loft", now)).toEqual({
    text: "會取代目前的鬥士通鋪（物攻 ×1.05，剩 1:42）",
    replacing: true,
  })
  expect(replaceNotice(lodging, rooms, "fighters-bunk", now)).toEqual({
    text: "正在住這間，剩 1:42",
    replacing: false,
  })
  expect(replaceNotice(null, rooms, "apprentice-loft", now)).toBeNull()
})

// 剛入住時畫面的「現在」可能比住宿開始還早（每 30 秒才更新、或伺服器時鐘略快）：剩餘不超過整段時長
test("stayRemainingClock_NeverExceedsTheStay", () => {
  const lodging = {
    roomId: "fighters-bunk",
    startsAt: endsIn(30_000),
    endsAt: endsIn(30_000 + 8 * 3_600_000),
  }

  expect(stayRemainingClock(lodging, now)).toBe("8:00")
})
