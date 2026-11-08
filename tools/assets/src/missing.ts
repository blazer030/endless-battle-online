import { readFile } from "node:fs/promises"
import { join } from "node:path"
import { readManifest } from "./manifest.ts"
import { AssetError, Categories, type Category } from "./process.ts"

type Identified = { id: string | number }

const readList = async (
  contentDirectory: string,
  file: string,
  property: string,
): Promise<string[]> => {
  const path = join(contentDirectory, file)
  const parsed = JSON.parse(await readFile(path, "utf8").catch(() => "{}")) as Record<
    string,
    unknown
  >
  const list = parsed[property]
  if (!Array.isArray(list)) throw new AssetError(`${path} 沒有 ${property} 陣列`)
  return (list as Identified[]).map((entry) => String(entry.id))
}

// 每一類應該有圖的鍵，從 backend/content 推出來
export const expectedKeys = async (
  contentDirectory: string,
): Promise<Record<Category, string[]>> => {
  const jobs = await readList(contentDirectory, "jobs.json", "jobs")
  return {
    portraits: jobs.flatMap((id) => [`${id}-male`, `${id}-female`]),
    "job-icons": jobs,
    skills: await readList(contentDirectory, "skills.json", "skills"),
    items: await readList(contentDirectory, "items.json", "items"),
    monsters: await readList(contentDirectory, "monsters.json", "monsters"),
    battlefields: await readList(contentDirectory, "world.json", "places"),
  }
}

export const missingReport = async (
  contentDirectory: string,
  manifestPath: string,
  categories: readonly Category[] = Categories,
): Promise<string[]> => {
  const expected = await expectedKeys(contentDirectory)
  const present = Object.keys((await readManifest(manifestPath)).assets)
  return categories.flatMap((category) => {
    const keys = expected[category]
    const prefix = `${category}/`
    const presentKeys = new Set(
      present.filter((name) => name.startsWith(prefix)).map((name) => name.slice(prefix.length)),
    )
    const missing = keys.filter((key) => !presentKeys.has(key))
    // 清單有、內容沒有：多半是匯入時打錯鍵
    const expectedSet = new Set(keys)
    const extra = [...presentKeys].filter((key) => !expectedSet.has(key))
    return [
      `${category}：有圖 ${keys.length - missing.length}／應有 ${keys.length}`,
      ...(missing.length === 0 ? [] : [`  缺：${missing.join("、")}`]),
      ...(extra.length === 0 ? [] : [`  多：${extra.join("、")}`]),
    ]
  })
}
