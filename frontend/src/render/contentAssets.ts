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

// 依序嘗試載入的網址：R2 的圖載入失敗時退回佔位圖
export const artCandidates = (asset: ContentAsset, placeholder: string): string[] =>
  asset.placeholder ? [asset.url] : [asset.url, placeholder]

// 依序嘗試每個網址，回傳第一個載入成功的結果；全部失敗回傳 null
export const loadFirst = async <T>(
  urls: readonly string[],
  load: (url: string) => Promise<T>,
): Promise<T | null> => {
  for (const url of urls) {
    try {
      return await load(url)
    } catch {
      continue
    }
  }
  return null
}

const isCategory = (text: string): text is ContentCategory =>
  (ContentCategories as readonly string[]).includes(text)

export const validateManifest = (manifest: ContentManifest): string[] => {
  const errors: string[] = []
  if (!/^https:\/\/[^/]+(\/.*[^/])?$/.test(manifest.baseUrl))
    errors.push(`baseUrl 要是 https 且結尾沒有斜線：${manifest.baseUrl}`)
  for (const [name, entry] of Object.entries(manifest.assets)) {
    const [category = "", ...rest] = name.split("/")
    const key = rest.join("/")
    if (!isCategory(category)) errors.push(`${name}：不認得的類別`)
    // 鍵限定小寫英數與連字號，放進正規表示式才不需要跳脫
    else if (!/^[a-z0-9-]+$/.test(key)) errors.push(`${name}：鍵只能用小寫英文、數字與連字號`)
    else if (!new RegExp(`^${name}\\.[0-9a-f]{12}\\.webp$`).test(entry.file))
      errors.push(`${name}：file 應為 ${name}.<12 位十六進位>.webp，實際是 ${entry.file}`)
    if (typeof entry.original !== "string") errors.push(`${name}：缺少 original`)
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

export const contentArtCandidates = (category: ContentCategory, key: string): string[] =>
  artCandidates(contentAsset(category, key), placeholders[category])
