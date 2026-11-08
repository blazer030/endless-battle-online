import sharp from "sharp"

// 測試用的合成圖：透明畫布上放一塊（或多塊）實心不透明矩形
export type Block = { left: number; top: number; width: number; height: number }

export const syntheticImage = async (
  width: number,
  height: number,
  blocks: readonly Block[],
): Promise<Buffer> => {
  const layers = await Promise.all(
    blocks.map(async (block) => ({
      input: await sharp({
        create: {
          width: block.width,
          height: block.height,
          channels: 4,
          background: { r: 120, g: 80, b: 40, alpha: 1 },
        },
      })
        .png()
        .toBuffer(),
      left: block.left,
      top: block.top,
    })),
  )
  return sharp({
    create: { width, height, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
  })
    .composite(layers)
    .png()
    .toBuffer()
}

export type Measured = {
  width: number
  height: number
  hasAlpha: boolean
  opaque: { left: number; top: number; right: number; bottom: number } | null
}

// 量輸出圖：尺寸、是否有透明、alpha > 24 的外框（right、bottom 是最後一個不透明像素的座標）
export const measure = async (image: Buffer): Promise<Measured> => {
  const { data, info } = await sharp(image)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true })
  let left = Infinity
  let top = Infinity
  let right = -1
  let bottom = -1
  let hasAlpha = false
  for (let y = 0; y < info.height; y++)
    for (let x = 0; x < info.width; x++) {
      const alpha = data[(y * info.width + x) * 4 + 3] ?? 0
      if (alpha < 255) hasAlpha = true
      if (alpha > 24) {
        left = Math.min(left, x)
        top = Math.min(top, y)
        right = Math.max(right, x)
        bottom = Math.max(bottom, y)
      }
    }
  return {
    width: info.width,
    height: info.height,
    hasAlpha,
    opaque: right < 0 ? null : { left, top, right, bottom },
  }
}
