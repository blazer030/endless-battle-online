import { describe, expect, it } from "vitest"
import { makePet, makePets } from "../test/fixtures"
import {
  canFeed,
  feedOptionLabel,
  personalityLabel,
  petCountText,
  petErrorMessage,
  petTitle,
} from "./pets"

describe("personalityLabel", () => {
  it("uses the WOG personality names", () => {
    expect(personalityLabel("Aggressive")).toBe("積極")
    expect(personalityLabel("Cold")).toBe("冷酷")
    expect(personalityLabel("Ironwall")).toBe("鐵壁")
    expect(personalityLabel("Desperate")).toBe("危急")
  })
})

describe("petTitle", () => {
  it("shows the name, species and age", () => {
    expect(petTitle(makePet({ name: "小黏", species: "草原史萊姆", age: 2 }))).toBe(
      "小黏－草原史萊姆 2歲",
    )
  })
})

describe("petCountText", () => {
  it("counts pets against the limit", () => {
    expect(petCountText(makePets([makePet(), makePet({ id: 2 })]))).toBe("寵物 2/3")
  })
})

describe("feedOptionLabel", () => {
  it("names the raised stat, amount and cost", () => {
    const foods = makePets().foods
    expect(foods.map(feedOptionLabel)).toEqual([
      "初級活力果實（AT+2，20 金）",
      "初級香氣果實（MT+2，20 金）",
      "初級甜味果實（DEF+2，20 金）",
      "初級野性果實（出擊值+2，20 金）",
      "中級活力果實（AT+4，50 金）",
      "中級香氣果實（MT+4，50 金）",
      "中級甜味果實（DEF+4，50 金）",
      "中級野性果實（出擊值+4，50 金）",
    ])
  })
})

describe("canFeed", () => {
  it("waits until the next feed time", () => {
    const pet = makePet({ nextFeedAt: new Date("2026-10-21T00:00:10Z") })
    expect(canFeed(pet, new Date("2026-10-21T00:00:09Z"))).toBe(false)
    expect(canFeed(pet, new Date("2026-10-21T00:00:10Z"))).toBe(true)
  })
})

describe("petErrorMessage", () => {
  it("maps every pet code to the WOG text", () => {
    expect(
      petErrorMessage(
        [
          "petUnknown",
          "foodUnknown",
          "tooSoon",
          "full",
          "overOwner",
          "nameInvalid",
          "notEnoughMoney",
        ],
        "x",
      ),
    ).toBe(
      "沒有寵物資料、沒有選擇項目、10 秒內無法餵食、飽腹大於100無法餵食、寵物屬性不能超過玩家、名稱長度不符、金錢不足",
    )
  })

  it("falls back to the detail for unknown or missing codes", () => {
    expect(petErrorMessage(["somethingNew"], "無法餵食")).toBe("無法餵食")
    expect(petErrorMessage([], "無法餵食")).toBe("無法餵食")
  })
})
