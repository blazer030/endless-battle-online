import sharp, { type Sharp } from "sharp"

export class AssetError extends Error {}

export const Categories = [
  "portraits",
  "job-icons",
  "skills",
  "items",
  "monsters",
  "battlefields",
] as const

export type Category = (typeof Categories)[number]

type Spec = { width: number; height: number; quality: number; maxBytes: number; alpha: boolean }

// 規格依 2026-11-08 試轉結果，見 docs/art/README.md
export const Specs: Record<Category, Spec> = {
  portraits: { width: 800, height: 1200, quality: 80, maxBytes: 163_840, alpha: true },
  "job-icons": { width: 96, height: 96, quality: 85, maxBytes: 12_288, alpha: true },
  skills: { width: 96, height: 96, quality: 85, maxBytes: 12_288, alpha: true },
  items: { width: 160, height: 160, quality: 85, maxBytes: 20_480, alpha: true },
  monsters: { width: 540, height: 540, quality: 80, maxBytes: 61_440, alpha: true },
  battlefields: { width: 1024, height: 1024, quality: 75, maxBytes: 153_600, alpha: false },
}

const IconFill = 0.88
const MonsterMaxHeight = 486
const MonsterMaxWidth = 513
const MonsterFeetY = 529
const OpaqueAlpha = 24
const LowestQuality = 60
const QualityStep = 5

export const parseCategory = (text: string): Category => {
  const category = Categories.find((name) => name === text)
  if (category === undefined)
    throw new AssetError(`不認得的類別：${text}（可用：${Categories.join("、")}）`)
  return category
}

export const validateKey = (key: string): void => {
  if (!/^[a-z0-9-]+$/.test(key)) throw new AssetError(`鍵只能用小寫英文、數字與連字號：${key}`)
}

export const encodeWithinLimit = async (
  encode: (quality: number) => Promise<Buffer>,
  quality: number,
  maxBytes: number,
): Promise<{ bytes: Buffer; quality: number }> => {
  for (let current = quality; current >= LowestQuality; current -= QualityStep) {
    const bytes = await encode(current)
    if (bytes.length <= maxBytes) return { bytes, quality: current }
  }
  throw new AssetError(`品質降到 ${LowestQuality} 仍超過上限 ${maxBytes} bytes`)
}

export type Raw = { data: Buffer; width: number; height: number }

export const readRaw = async (source: Buffer): Promise<Raw> => {
  const { data, info } = await sharp(source)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true })
  return { data, width: info.width, height: info.height }
}

export type Box = { left: number; top: number; width: number; height: number }

export const opaqueBox = (raw: Raw): Box => {
  let left = Infinity
  let top = Infinity
  let right = -1
  let bottom = -1
  for (let y = 0; y < raw.height; y++)
    for (let x = 0; x < raw.width; x++)
      if ((raw.data[(y * raw.width + x) * 4 + 3] ?? 0) > OpaqueAlpha) {
        left = Math.min(left, x)
        top = Math.min(top, y)
        right = Math.max(right, x)
        bottom = Math.max(bottom, y)
      }
  if (right < 0) throw new AssetError("來源圖沒有不透明的部分")
  return { left, top, width: right - left + 1, height: bottom - top + 1 }
}

const scaledPiece = async (raw: Raw, box: Box, scale: number) => {
  const width = Math.max(1, Math.round(box.width * scale))
  const height = Math.max(1, Math.round(box.height * scale))
  const input = await sharp(raw.data, {
    raw: { width: raw.width, height: raw.height, channels: 4 },
  })
    .extract(box)
    .resize(width, height, { fit: "fill", kernel: "lanczos3" })
    .png()
    .toBuffer()
  return { input, width, height }
}

type Composed = { image: Sharp; warnings: string[] }

const enlargeWarning = (scale: number): string[] =>
  scale > 1 ? [`原圖比規格小，已放大 ${scale.toFixed(2)} 倍`] : []

const blankCanvas = (spec: Spec) =>
  sharp({
    create: {
      width: spec.width,
      height: spec.height,
      channels: 4,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    },
  })

const composeIcon = async (raw: Raw, spec: Spec): Promise<Composed> => {
  const box = opaqueBox(raw)
  const scale = Math.round(spec.width * IconFill) / Math.max(box.width, box.height)
  const piece = await scaledPiece(raw, box, scale)
  const image = blankCanvas(spec).composite([
    {
      input: piece.input,
      left: Math.floor((spec.width - piece.width) / 2),
      top: Math.floor((spec.height - piece.height) / 2),
    },
  ])
  return { image, warnings: enlargeWarning(scale) }
}

const composeMonster = async (raw: Raw, spec: Spec): Promise<Composed> => {
  const box = opaqueBox(raw)
  const scale = Math.min(MonsterMaxHeight / box.height, MonsterMaxWidth / box.width)
  const piece = await scaledPiece(raw, box, scale)
  const image = blankCanvas(spec).composite([
    {
      input: piece.input,
      left: Math.floor((spec.width - piece.width) / 2),
      top: MonsterFeetY + 1 - piece.height,
    },
  ])
  return { image, warnings: enlargeWarning(scale) }
}

const composeBattlefield = async (raw: Raw, spec: Spec): Promise<Composed> => {
  const scale = Math.max(spec.width / raw.width, spec.height / raw.height)
  const image = sharp(raw.data, {
    raw: { width: raw.width, height: raw.height, channels: 4 },
  }).resize(spec.width, spec.height, { fit: "cover", position: "centre", kernel: "lanczos3" })
  return { image, warnings: enlargeWarning(scale) }
}

const compose = async (category: Category, raw: Raw, spec: Spec): Promise<Composed> => {
  switch (category) {
    case "items":
    case "skills":
    case "job-icons":
      return composeIcon(raw, spec)
    case "monsters":
      return composeMonster(raw, spec)
    case "battlefields":
      return composeBattlefield(raw, spec)
    default:
      throw new AssetError(`尚未支援的類別：${category}`)
  }
}

export type Processed = { webp: Buffer; quality: number; warnings: string[] }

export const processImage = async (category: Category, source: Buffer): Promise<Processed> => {
  const spec = Specs[category]
  const raw = await readRaw(source)
  const composed = await compose(category, raw, spec)
  // composite 要先落成像素，才能用不同品質重複編碼
  const flattened = await composed.image.png().toBuffer()
  const { bytes, quality } = await encodeWithinLimit(
    (current) =>
      sharp(flattened).webp({ quality: current, alphaQuality: 100, effort: 6 }).toBuffer(),
    spec.quality,
    spec.maxBytes,
  )
  return { webp: bytes, quality, warnings: composed.warnings }
}
