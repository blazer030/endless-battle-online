import { describe, expect, it } from "vitest"
import { makeMercenaryOverview, makeWorld } from "../test/fixtures"
import type { MercenaryContract } from "./types"
import {
  catalogueRow,
  contractStatus,
  deployablePlaces,
  mercenaryErrorMessage,
} from "./mercenaries"

const [mercenaryF, mercenaryA] = makeMercenaryOverview().catalogue

const contract: MercenaryContract = {
  mercenaryId: "mercenary-f",
  name: "F級傭兵",
  remaining: 97,
  placeId: null,
  placeName: null,
  fighting: false,
  nextFightAt: null,
}

describe("catalogueRow", () => {
  it("formats the price and percentages", () => {
    expect(catalogueRow(mercenaryF)).toEqual({
      name: "F級傭兵",
      price: "3,000",
      fights: "100",
      money: "30%",
      drop: "30%",
      experience: "65%",
      deathCost: "5",
      hireable: true,
    })
  })

  it("points honour-only mercenaries to the honour shop", () => {
    expect(catalogueRow(mercenaryA)).toMatchObject({ price: "勳章所兌換", hireable: false })
  })
})

describe("contractStatus", () => {
  it("shows resting or fighting with the remaining fights", () => {
    expect(contractStatus(contract)).toEqual(["狀態：休息中", "剩餘次數 97"])
    expect(
      contractStatus({ ...contract, fighting: true, placeId: 1, placeName: "中央平原" }),
    ).toEqual(["狀態：戰鬥中", "冒險地：中央平原", "剩餘次數 97"])
  })
})

describe("mercenaryErrorMessage", () => {
  it("maps every mercenary code", () => {
    expect(
      mercenaryErrorMessage(
        [
          "mercenaryUnknown",
          "honourOnly",
          "mercenaryHired",
          "noMercenary",
          "placeUnknown",
          "placeRestricted",
          "nameInvalid",
          "notEnoughMoney",
        ],
        "x",
      ),
    ).toBe(
      "沒有這位傭兵、這位傭兵只能在勳章所兌換、只能僱用一位傭兵、還沒有僱用傭兵、沒有選擇冒險地點、傭兵不能前往禁區、名稱長度不符、金錢不足",
    )
  })

  it("falls back to the detail for unknown or missing codes", () => {
    expect(mercenaryErrorMessage(["somethingNew"], "無法僱用")).toBe("無法僱用")
    expect(mercenaryErrorMessage([], "無法僱用")).toBe("無法僱用")
  })
})

describe("deployablePlaces", () => {
  it("lists every unrestricted place in world order", () => {
    expect(deployablePlaces(makeWorld()).map((place) => place.name)).toEqual([
      "中央平原",
      "黑暗沼澤",
      "試鍊洞窟",
      "黑曜神廟",
    ])
  })
})
