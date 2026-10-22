import { describe, expect, it } from "vitest"
import { makeCharacter, makePlace } from "../test/fixtures"
import {
  locationTitle,
  moveReasonText,
  passStatus,
  placeAccess,
  recommendationLabel,
} from "./world"

describe("recommendationLabel", () => {
  it.each([
    ["Recommended", { text: "推薦", tone: "recommended" }],
    ["TooHard", { text: "偏難", tone: "too-hard" }],
    ["TooEasy", { text: "偏易", tone: "too-easy" }],
  ] as const)("labels %s", (recommendation, label) => {
    expect(recommendationLabel(recommendation)).toEqual(label)
  })

  it("has no label for None", () => {
    expect(recommendationLabel("None")).toBeNull()
  })
})

describe("moveReasonText", () => {
  it.each([
    ["restricted", "需用鑰匙開啟地區"],
    ["subPlaceRequired", "請選擇子地區"],
    ["subPlaceNotAllowed", "此地區沒有子地區"],
    ["subPlaceOutOfRange", "子地區不存在"],
    ["samePlace", "你已經在這裡"],
    ["unknownPlace", "地區不存在"],
  ])("translates %s", (code, text) => {
    expect(moveReasonText(code)).toBe(text)
  })

  it("falls back to the code when it is unknown", () => {
    expect(moveReasonText("somethingNew")).toBe("somethingNew")
  })
})

describe("locationTitle", () => {
  it("joins the place and sub-place names", () => {
    const character = makeCharacter({ placeName: "試鍊洞窟", subPlaceName: "燈影隧道" })
    expect(locationTitle(character)).toBe("試鍊洞窟・燈影隧道")
  })

  it("shows only the place name when there is no sub-place", () => {
    const character = makeCharacter({
      placeId: 20,
      placeName: "黑曜神廟",
      subPlace: null,
      subPlaceName: null,
    })
    expect(locationTitle(character)).toBe("黑曜神廟")
  })
})

describe("placeAccess", () => {
  const now = new Date(2026, 9, 21, 8, 0)
  const forbidden = makePlace({ id: 17, name: "廢棄洞窟", restricted: true })

  it("leaves ordinary places open without a mark", () => {
    expect(placeAccess(makePlace({ id: 1, name: "中央平原" }), now)).toEqual({
      open: true,
      label: null,
    })
  })

  it("shows until when an unexpired pass lasts", () => {
    const passExpiresAt = new Date(2026, 9, 21, 9, 5).toISOString()
    expect(placeAccess({ ...forbidden, passExpiresAt, keys: 1 }, now)).toEqual({
      open: true,
      label: "通行至 09:05",
    })
  })

  it("opens with a held key once the pass has expired", () => {
    const passExpiresAt = now.toISOString()
    expect(placeAccess({ ...forbidden, passExpiresAt, keys: 2 }, now)).toEqual({
      open: true,
      label: "需要鑰匙（持有 2）",
    })
  })

  it("stays shut without a pass or a key", () => {
    expect(placeAccess(forbidden, now)).toEqual({ open: false, label: "需要鑰匙" })
  })
})

describe("passStatus", () => {
  const now = new Date("2026-10-21T00:00:00Z")

  it("has nothing to say without a pass", () => {
    expect(passStatus(null, now)).toBeNull()
  })

  it("counts the remaining minutes up", () => {
    expect(passStatus("2026-10-21T00:30:00Z", now)).toBe("禁區通行剩 30 分")
    expect(passStatus("2026-10-21T00:00:30Z", now)).toBe("禁區通行剩 1 分")
  })

  it("tells the player to come back with a key once it expires", () => {
    expect(passStatus("2026-10-21T00:00:00Z", now)).toBe(
      "禁區通行時間已結束，請到地圖用鑰匙再次進入",
    )
  })
})
