import { readFile, writeFile } from "node:fs/promises"
import { AssetError } from "./process.ts"

export type ManifestEntry = { file: string; original: string; headY?: number }

export type Manifest = { baseUrl: string; assets: Record<string, ManifestEntry> }

export const readManifest = async (path: string): Promise<Manifest> => {
  let text: string
  try {
    text = await readFile(path, "utf8")
  } catch {
    throw new AssetError(`找不到清單：${path}`)
  }
  return JSON.parse(text) as Manifest
}

// 鍵依字母排序、兩格縮排、結尾換行，diff 才好讀
export const serializeManifest = (manifest: Manifest): string => {
  const assets = Object.fromEntries(
    Object.keys(manifest.assets)
      .sort()
      .map((key) => [key, manifest.assets[key]]),
  )
  return `${JSON.stringify({ baseUrl: manifest.baseUrl, assets }, null, 2)}\n`
}

export const writeManifest = async (path: string, manifest: Manifest): Promise<void> => {
  const text = serializeManifest(manifest)
  const current = await readFile(path, "utf8").catch(() => null)
  if (current !== text) await writeFile(path, text)
}
