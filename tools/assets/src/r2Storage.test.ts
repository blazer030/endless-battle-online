import { describe, expect, it } from "vitest"
import { R2Storage, type CommandSender } from "./r2Storage.ts"

type Sent = { name: string; input: Record<string, unknown> }

const clientReturning = (respond: (sent: Sent) => unknown) => {
  const sent: Sent[] = []
  const client: CommandSender = {
    send: async (command) => {
      const record = {
        name: command.constructor.name,
        input: command.input as unknown as Record<string, unknown>,
      }
      sent.push(record)
      return respond(record)
    },
  }
  return { client, sent }
}

describe("R2Storage", () => {
  it("內容圖上傳到 ebo-content 並帶正確的 Content-Type 與 Cache-Control", async () => {
    const { client, sent } = clientReturning(() => ({}))

    await new R2Storage(client).put(
      "content",
      "items/a.0123456789ab.webp",
      Buffer.from("x"),
      "image/webp",
    )

    expect(sent[0]?.name).toBe("PutObjectCommand")
    expect(sent[0]?.input).toMatchObject({
      Bucket: "ebo-content",
      Key: "items/a.0123456789ab.webp",
      ContentType: "image/webp",
      CacheControl: "public, max-age=31536000, immutable",
    })
  })

  it("原檔上傳到 ebo-originals", async () => {
    const { client, sent } = clientReturning(() => ({}))

    await new R2Storage(client).put(
      "originals",
      "items/a.0123456789ab.png",
      Buffer.from("x"),
      "image/png",
    )

    expect(sent[0]?.input).toMatchObject({ Bucket: "ebo-originals", ContentType: "image/png" })
  })

  it("物件不存在（404）時 exists 回傳 false", async () => {
    const { client } = clientReturning(() => {
      throw Object.assign(new Error("NotFound"), {
        name: "NotFound",
        $metadata: { httpStatusCode: 404 },
      })
    })

    expect(await new R2Storage(client).exists("content", "x")).toBe(false)
  })

  it("物件存在時 exists 回傳 true", async () => {
    const { client, sent } = clientReturning(() => ({}))

    expect(await new R2Storage(client).exists("content", "x")).toBe(true)
    expect(sent[0]).toMatchObject({
      name: "HeadObjectCommand",
      input: { Bucket: "ebo-content", Key: "x" },
    })
  })

  it("其他錯誤（例如權限）照樣丟出，不當成不存在", async () => {
    const { client } = clientReturning(() => {
      throw Object.assign(new Error("Forbidden"), {
        name: "Forbidden",
        $metadata: { httpStatusCode: 403 },
      })
    })

    await expect(new R2Storage(client).exists("content", "x")).rejects.toThrow("Forbidden")
  })
})
