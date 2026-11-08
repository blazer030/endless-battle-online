import { HeadObjectCommand, PutObjectCommand, S3Client } from "@aws-sdk/client-s3"
import type { Credentials } from "./credentials.ts"
import type { Bucket, Storage } from "./storage.ts"

export type CommandSender = {
  send: (command: HeadObjectCommand | PutObjectCommand) => Promise<unknown>
}

const BucketNames: Record<Bucket, string> = { content: "ebo-content", originals: "ebo-originals" }

// 內容圖檔名帶雜湊、內容永不變，可以讓瀏覽器與 CDN 長期快取
const ContentCacheControl = "public, max-age=31536000, immutable"

const isNotFound = (error: unknown): boolean =>
  typeof error === "object" &&
  error !== null &&
  ((error as { name?: unknown }).name === "NotFound" ||
    (error as { $metadata?: { httpStatusCode?: unknown } }).$metadata?.httpStatusCode === 404)

export class R2Storage implements Storage {
  private readonly client: CommandSender

  constructor(client: CommandSender) {
    this.client = client
  }

  async exists(bucket: Bucket, key: string): Promise<boolean> {
    try {
      await this.client.send(new HeadObjectCommand({ Bucket: BucketNames[bucket], Key: key }))
      return true
    } catch (error) {
      if (isNotFound(error)) return false
      throw error
    }
  }

  async put(bucket: Bucket, key: string, body: Buffer, contentType: string): Promise<void> {
    await this.client.send(
      new PutObjectCommand({
        Bucket: BucketNames[bucket],
        Key: key,
        Body: body,
        ContentType: contentType,
        ...(bucket === "content" ? { CacheControl: ContentCacheControl } : {}),
      }),
    )
  }
}

export const createR2Storage = (credentials: Credentials): R2Storage =>
  new R2Storage(
    new S3Client({
      region: "auto",
      endpoint: credentials.endpoint,
      credentials: {
        accessKeyId: credentials.accessKeyId,
        secretAccessKey: credentials.secretAccessKey,
      },
    }),
  )
