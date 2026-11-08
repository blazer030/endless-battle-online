import type { Bucket, Storage } from "../storage.ts"

// 假的儲存體：記住放了哪些物件，可以指定某個 bucket 上傳失敗
export class FakeStorage implements Storage {
  readonly objects = new Map<string, { body: Buffer; contentType: string }>()
  readonly puts: string[] = []
  failingBucket: Bucket | null = null

  async exists(bucket: Bucket, key: string): Promise<boolean> {
    return this.objects.has(`${bucket}:${key}`)
  }

  async put(bucket: Bucket, key: string, body: Buffer, contentType: string): Promise<void> {
    if (this.failingBucket === bucket) throw new Error(`上傳到 ${bucket} 失敗`)
    this.puts.push(`${bucket}:${key}`)
    this.objects.set(`${bucket}:${key}`, { body, contentType })
  }
}
