import { expect, test } from "vitest"
import { companionsImages, innImages, townImages } from "./screenImages"

// 城鎮只預載目前時段的全景（四張都載太浪費）
test("TownImages_OnlyTheCurrentPanorama", () => {
  const panoramas = townImages("dusk").filter((url) => url.includes("panorama-"))

  expect(panoramas).toHaveLength(1)
  expect(panoramas[0]).toContain("panorama-dusk")
  expect(townImages("dusk").some((url) => url.includes("purse"))).toBe(true)
})

// 旅館的房型圖、傭兵所的兩枚勳章是內容圖，也一起預載
test("InnerPageImages_IncludeContentArt", () => {
  expect(innImages().some((url) => url.includes("room-backgrounds/apprentice-loft"))).toBe(true)
  expect(companionsImages().some((url) => url.includes("items/item-1304"))).toBe(true)
  expect(companionsImages().some((url) => url.includes("stall-gate"))).toBe(true)
})
