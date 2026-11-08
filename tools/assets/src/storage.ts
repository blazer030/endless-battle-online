export type Bucket = "content" | "originals"

export interface Storage {
  exists(bucket: Bucket, key: string): Promise<boolean>
  put(bucket: Bucket, key: string, body: Buffer, contentType: string): Promise<void>
}
