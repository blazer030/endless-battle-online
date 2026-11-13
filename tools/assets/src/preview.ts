import sharp from "sharp"
import { Specs, type Category } from "./process.ts"

// 預覽圖：棋盤底（看得出透明）＋輔助線，給人眼確認對齊
const guides = (category: Category, width: number, height: number): string => {
  const line = (x1: number, y1: number, x2: number, y2: number, color: string) =>
    `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="2" stroke-dasharray="8 6"/>`
  switch (category) {
    case "portraits":
      return [
        line(0, 80, width, 80, "#d33"),
        line(0, 1180, width, 1180, "#d33"),
        line(400, 0, 400, height, "#36c"),
      ].join("")
    case "monsters":
      return line(0, 529, width, 529, "#d33")
    case "items":
    case "skills":
    case "job-icons":
    case "room-badges": {
      const inner = Math.round(width * 0.88)
      const offset = (width - inner) / 2
      return `<rect x="${offset}" y="${offset}" width="${inner}" height="${inner}" fill="none" stroke="#36c" stroke-width="1" stroke-dasharray="4 3"/>`
    }
    case "battlefields":
    case "room-backgrounds":
      return ""
  }
}

export const renderPreview = async (category: Category, webp: Buffer): Promise<Buffer> => {
  const { width, height } = Specs[category]
  const cell = Math.max(8, Math.round(width / 40))
  const checker = Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">` +
      `<defs><pattern id="c" width="${cell * 2}" height="${cell * 2}" patternUnits="userSpaceOnUse">` +
      `<rect width="${cell * 2}" height="${cell * 2}" fill="#eee"/>` +
      `<rect width="${cell}" height="${cell}" fill="#ccc"/><rect x="${cell}" y="${cell}" width="${cell}" height="${cell}" fill="#ccc"/>` +
      `</pattern></defs><rect width="100%" height="100%" fill="url(#c)"/></svg>`,
  )
  const overlay = Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">${guides(category, width, height)}</svg>`,
  )
  return sharp(checker)
    .composite([{ input: webp }, { input: overlay }])
    .png()
    .toBuffer()
}
