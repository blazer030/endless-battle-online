import { createHash } from "node:crypto"
import { readManifest, writeManifest, type ManifestEntry } from "./manifest.ts"
import type { Category } from "./process.ts"
import type { Bucket, Storage } from "./storage.ts"

export type PublishRequest = {
  category: Category
  key: string
  webp: Buffer
  source: Buffer
  sourceExtension: string
  headY?: number
  storage: Storage
  manifestPath: string
}

const SourceTypes: Record<string, string> = {
  png: "image/png",
  jpg: "image/jpeg",
  jpeg: "image/jpeg",
  webp: "image/webp",
}

const shortHash = (bytes: Buffer): string =>
  createHash("sha256").update(bytes).digest("hex").slice(0, 12)

const putOnce = async (
  storage: Storage,
  bucket: Bucket,
  key: string,
  body: Buffer,
  contentType: string,
) => {
  // 物件永不覆寫：已經存在就不再上傳
  if (!(await storage.exists(bucket, key))) await storage.put(bucket, key, body, contentType)
}

// 先上傳原檔與內容圖，兩者都成功才寫清單；任何一步失敗清單都不變
export const publishAsset = async (request: PublishRequest): Promise<ManifestEntry> => {
  const extension = request.sourceExtension.toLowerCase()
  const prefix = `${request.category}/${request.key}`
  const entry: ManifestEntry = {
    file: `${prefix}.${shortHash(request.webp)}.webp`,
    original: `${prefix}.${shortHash(request.source)}.${extension}`,
    ...(request.headY === undefined ? {} : { headY: request.headY }),
  }
  const manifest = await readManifest(request.manifestPath)

  await putOnce(
    request.storage,
    "originals",
    entry.original,
    request.source,
    SourceTypes[extension] ?? "application/octet-stream",
  )
  await putOnce(request.storage, "content", entry.file, request.webp, "image/webp")

  manifest.assets[prefix] = entry
  await writeManifest(request.manifestPath, manifest)
  return entry
}
