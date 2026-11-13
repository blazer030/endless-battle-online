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

describe("processImage 怪物與戰鬥背景", () => {
  it("怪物腳底在 y=529 且水平置中，高度縮到 486", async () => {
    const source = await syntheticImage(300, 400, [{ left: 40, top: 30, width: 200, height: 300 }])

    const measured = await measure((await processImage("monsters", source)).webp)

    expect([measured.width, measured.height]).toEqual([540, 540])
    const opaque = measured.opaque!
    expect(opaque.bottom).toBe(529)
    expect(opaque.bottom - opaque.top + 1).toBe(486)
    expect(Math.abs(opaque.left - (539 - opaque.right))).toBeLessThanOrEqual(1)
  })

  it("扁長的怪物以寬度 513 為限", async () => {
    const source = await syntheticImage(500, 200, [{ left: 0, top: 50, width: 400, height: 100 }])

    const opaque = (await measure((await processImage("monsters", source)).webp)).opaque!

    expect(opaque.right - opaque.left + 1).toBe(513)
    expect(opaque.bottom).toBe(529)
  })

  it("1600×900 的背景輸出 1024×1024 且沒有透明像素", async () => {
    const source = await syntheticImage(1600, 900, [{ left: 0, top: 0, width: 1600, height: 900 }])

    const measured = await measure((await processImage("battlefields", source)).webp)

    expect([measured.width, measured.height]).toEqual([1024, 1024])
    expect(measured.hasAlpha).toBe(false)
  })

  it("來源有透明區時輸出仍不透明", async () => {
    const source = await syntheticImage(1600, 900, [{ left: 0, top: 0, width: 800, height: 900 }])

    const measured = await measure((await processImage("battlefields", source)).webp)

    expect(measured.hasAlpha).toBe(false)
  })

  it("比 1024 小的背景會放大並警告", async () => {
    const source = await syntheticImage(800, 600, [{ left: 0, top: 0, width: 800, height: 600 }])

    expect((await processImage("battlefields", source)).warnings.join("")).toContain("放大")
  })
})

describe("processImage 房型", () => {
  // 規格 content-assets：Room badge is centered on a square canvas
  it("273×272 的房型徽章輸出 128×128、長邊 113 並置中", async () => {
    const source = await syntheticImage(300, 300, [{ left: 10, top: 12, width: 273, height: 272 }])

    const measured = await measure((await processImage("room-badges", source)).webp)

    expect([measured.width, measured.height]).toEqual([128, 128])
    expect(measured.hasAlpha).toBe(true)
    const opaque = measured.opaque!
    expect(opaque.right - opaque.left + 1).toBe(113)
    expect(Math.abs(opaque.left - (127 - opaque.right))).toBeLessThanOrEqual(1)
    expect(Math.abs(opaque.top - (127 - opaque.bottom))).toBeLessThanOrEqual(1)
  })

  // 規格 content-assets：Room background covers its canvas
  it("1280×720 的房間背景輸出 800×450 且沒有透明像素", async () => {
    const source = await syntheticImage(1280, 720, [{ left: 0, top: 0, width: 1280, height: 720 }])

    const measured = await measure((await processImage("room-backgrounds", source)).webp)

    expect([measured.width, measured.height]).toEqual([800, 450])
    expect(measured.hasAlpha).toBe(false)
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

  it("八個類別都認得", () => {
    for (const name of [
      "portraits",
      "job-icons",
      "skills",
      "items",
      "monsters",
      "battlefields",
      "room-badges",
      "room-backgrounds",
    ])
      expect(parseCategory(name)).toBe(name)
  })
})
