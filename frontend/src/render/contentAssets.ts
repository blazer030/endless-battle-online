import manifestJson from "../assets/content-manifest.json"
import battlefieldsPlaceholder from "../assets/placeholders/battlefields.svg"
import itemsPlaceholder from "../assets/placeholders/items.svg"
import jobIconsPlaceholder from "../assets/placeholders/job-icons.svg"
import monstersPlaceholder from "../assets/placeholders/monsters.svg"
import portraitsPlaceholder from "../assets/placeholders/portraits.svg"
import skillsPlaceholder from "../assets/placeholders/skills.svg"

// 內容圖（道具、怪物、技能、職業立繪與圖示、戰鬥背景）放在 R2，清單由 tools/assets 的 import 指令維護
export const ContentCategories = [
  "portraits",
  "job-icons",
  "skills",
  "items",
  "monsters",
  "battlefields",
] as const

export type ContentCategory = (typeof ContentCategories)[number]

export type ContentAsset = { url: string; placeholder: boolean }

export type ContentManifest = {
  baseUrl: string
  assets: Record<string, { file: string; original: string; headY?: number }>
}

export const createContentAssets =
  (manifest: ContentManifest, placeholders: Record<ContentCategory, string>) =>
  (category: ContentCategory, key: string): ContentAsset => {
    const entry = manifest.assets[`${category}/${key}`]
    return entry === undefined
      ? { url: placeholders[category], placeholder: true }
      : { url: `${manifest.baseUrl}/${entry.file}`, placeholder: false }
  }

const isCategory = (text: string): text is ContentCategory =>
  (ContentCategories as readonly string[]).includes(text)

export const validateManifest = (manifest: ContentManifest): string[] => {
  const errors: string[] = []
  if (!/^https:\/\/[^/]+(\/.*[^/])?$/.test(manifest.baseUrl))
    errors.push(`baseUrl 要是 https 且結尾沒有斜線：${manifest.baseUrl}`)
  for (const [name, entry] of Object.entries(manifest.assets)) {
    const category = name.split("/")[0] ?? ""
    if (!isCategory(category)) errors.push(`${name}：不認得的類別`)
    else if (!new RegExp(`^${name}\\.[0-9a-f]{12}\\.webp$`).test(entry.file))
      errors.push(`${name}：file 應為 ${name}.<12 位十六進位>.webp，實際是 ${entry.file}`)
  }
  return errors
}

const placeholders: Record<ContentCategory, string> = {
  portraits: portraitsPlaceholder,
  "job-icons": jobIconsPlaceholder,
  skills: skillsPlaceholder,
  items: itemsPlaceholder,
  monsters: monstersPlaceholder,
  battlefields: battlefieldsPlaceholder,
}

export const contentAsset = createContentAssets(manifestJson as ContentManifest, placeholders)
