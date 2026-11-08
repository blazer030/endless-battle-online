import { describe, expect, it } from "vitest"
import {
  AssetError,
  encodeWithinLimit,
  parseCategory,
  processImage,
  validateKey,
} from "./process.ts"
import { measure, syntheticImage } from "./testing/images.ts"

describe("processImage 圖示類", () => {
  it("152×546 的道具圖輸出 160×160 且圖案長邊 141 並置中", async () => {
    const source = await syntheticImage(400, 700, [{ left: 30, top: 50, width: 152, height: 546 }])

    const result = await processImage("items", source)

    const measured = await measure(result.webp)
    expect(measured.width).toBe(160)
    expect(measured.height).toBe(160)
    expect(measured.hasAlpha).toBe(true)
    const opaque = measured.opaque!
    expect(opaque.bottom - opaque.top + 1).toBe(141)
    expect(Math.abs(opaque.top - (159 - opaque.bottom))).toBeLessThanOrEqual(1)
    expect(Math.abs(opaque.left - (159 - opaque.right))).toBeLessThanOrEqual(1)
  })

  it.each(["skills", "job-icons"] as const)("%s 輸出 96×96", async (category) => {
    const source = await syntheticImage(300, 200, [{ left: 10, top: 10, width: 200, height: 150 }])

    const measured = await measure((await processImage(category, source)).webp)

    expect([measured.width, measured.height]).toEqual([96, 96])
    const opaque = measured.opaque!
    expect(opaque.right - opaque.left + 1).toBe(84)
  })

  it("放大時有警告", async () => {
    const source = await syntheticImage(40, 40, [{ left: 5, top: 5, width: 20, height: 30 }])

    const result = await processImage("items", source)

    expect(result.warnings.join("")).toContain("放大")
  })

  it("不需要放大時沒有警告", async () => {
    const source = await syntheticImage(400, 400, [{ left: 0, top: 0, width: 300, height: 300 }])

    expect((await processImage("items", source)).warnings).toEqual([])
  })
})

describe("encodeWithinLimit", () => {
  it("上限設很小時依序嘗試 85、80…60 後拒絕", async () => {
    const tried: number[] = []
    const encode = async (quality: number) => {
      tried.push(quality)
      return Buffer.alloc(1000)
    }

    await expect(encodeWithinLimit(encode, 85, 999)).rejects.toThrow(AssetError)
    expect(tried).toEqual([85, 80, 75, 70, 65, 60])
  })

  it("降到符合上限就停下並回傳那個品質", async () => {
    const encode = async (quality: number) => Buffer.alloc(quality * 10)

    const result = await encodeWithinLimit(encode, 80, 700)

    expect(result.quality).toBe(70)
    expect(result.bytes.length).toBe(700)
  })
})

describe("輸入檢查", () => {
  it("鍵含大寫時拒絕", () => {
    expect(() => validateKey("Monster-1")).toThrow(AssetError)
  })

  it("鍵只有小寫英數與連字號時通過", () => {
    expect(() => validateKey("job-6-male")).not.toThrow()
  })

  it("未知類別時拒絕", () => {
    expect(() => parseCategory("weapons")).toThrow(AssetError)
  })

  it("六個類別都認得", () => {
    for (const name of ["portraits", "job-icons", "skills", "items", "monsters", "battlefields"])
      expect(parseCategory(name)).toBe(name)
  })
})
