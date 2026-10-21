import { describe, expect, it } from "vitest"
import { makeCharacter } from "../test/fixtures"
import { locationTitle, moveReasonText, recommendationLabel } from "./world"

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
    ["restricted", "此地區需要鑰匙"],
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
