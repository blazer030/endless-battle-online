import { describe, expect, it } from "vitest"
import { AssetError, processImage } from "./process.ts"
import { measure, rowSpan, syntheticImage } from "./testing/images.ts"

const centerAt = async (image: Buffer, y: number) => {
  const span = (await rowSpan(image, y))!
  return (span.left + span.right) / 2
}

describe("立繪對齊", () => {
  it("兩張不同尺寸的合成立繪腳底都在 1180、中線都在 400", async () => {
    // A：身體在 150～350，右上方多一把只到 30% 高度的武器，不應該影響中線
    const first = await syntheticImage(500, 1000, [
      { left: 150, top: 0, width: 200, height: 1000 },
      { left: 350, top: 0, width: 120, height: 300 },
    ])
    const second = await syntheticImage(300, 800, [{ left: 100, top: 50, width: 100, height: 700 }])

    for (const source of [first, second]) {
      const webp = (await processImage("portraits", source)).webp
      const measured = await measure(webp)
      expect([measured.width, measured.height]).toEqual([800, 1200])
      expect(measured.opaque!.bottom).toBe(1180)
      expect(Math.abs((await centerAt(webp, 900)) - 400)).toBeLessThanOrEqual(1)
    }
  })

  it("高 1000 的原圖頂端落在 y=80", async () => {
    const source = await syntheticImage(400, 1000, [
      { left: 100, top: 0, width: 200, height: 1000 },
    ])

    const opaque = (await measure((await processImage("portraits", source)).webp)).opaque!

    expect(opaque.top).toBe(80)
    expect(opaque.bottom).toBe(1180)
  })

  it("headY 40 把原圖第 40 列放到 y=80", async () => {
    // 0～39 列是一頂窄帽子，40 列起才是身體
    const source = await syntheticImage(400, 1000, [
      { left: 190, top: 0, width: 20, height: 40 },
      { left: 100, top: 40, width: 200, height: 960 },
    ])

    const webp = (await processImage("portraits", source, { headY: 40 })).webp

    let bodyTop = -1
    for (let y = 0; y < 1200 && bodyTop < 0; y++) {
      const span = await rowSpan(webp, y)
      if (span !== null && span.right - span.left > 100) bodyTop = y
    }
    expect(Math.abs(bodyTop - 80)).toBeLessThanOrEqual(1)
    expect((await measure(webp)).opaque!.bottom).toBe(1180)
  })

  it("左邊超出時拒絕並寫『左側超出畫布』", async () => {
    const source = await syntheticImage(1200, 1000, [
      { left: 900, top: 0, width: 100, height: 1000 },
      { left: 0, top: 0, width: 900, height: 100 },
    ])

    const error = await processImage("portraits", source).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(AssetError)
    expect((error as Error).message).toContain("左側超出畫布")
  })

  it("headY 不在人物範圍內時拒絕", async () => {
    const source = await syntheticImage(400, 1000, [
      { left: 100, top: 100, width: 200, height: 800 },
    ])

    await expect(processImage("portraits", source, { headY: 950 })).rejects.toThrow(AssetError)
  })
})
