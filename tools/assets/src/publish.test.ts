import { mkdtemp, readFile, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { beforeEach, describe, expect, it } from "vitest"
import { publishAsset } from "./publish.ts"
import { FakeStorage } from "./testing/fakeStorage.ts"

const EmptyManifest = '{\n  "baseUrl": "https://ebo-assets.blazer030.dev",\n  "assets": {}\n}\n'

let manifestPath: string
let storage: FakeStorage

beforeEach(async () => {
  manifestPath = join(await mkdtemp(join(tmpdir(), "ebo-assets-")), "content-manifest.json")
  await writeFile(manifestPath, EmptyManifest)
  storage = new FakeStorage()
})

const publish = (webp: string, source = "source-a", key = "monster-1") =>
  publishAsset({
    category: "monsters",
    key,
    webp: Buffer.from(webp),
    source: Buffer.from(source),
    sourceExtension: "png",
    storage,
    manifestPath,
  })

const manifestOf = async () => JSON.parse(await readFile(manifestPath, "utf8"))

describe("publishAsset", () => {
  it("檔名是類別／鍵＋SHA-256 前 12 碼，清單記下內容圖與原檔", async () => {
    await publish("webp-a")

    // sha256("webp-a") 與 sha256("source-a") 的前 12 碼（用 shasum -a 256 另外算出）
    expect((await manifestOf()).assets["monsters/monster-1"]).toEqual({
      file: "monsters/monster-1.b50cbe71c819.webp",
      original: "monsters/monster-1.0f9f5ce47831.png",
    })
    expect(storage.objects.get("content:monsters/monster-1.b50cbe71c819.webp")?.contentType).toBe(
      "image/webp",
    )
    expect(storage.objects.get("originals:monsters/monster-1.0f9f5ce47831.png")?.contentType).toBe(
      "image/png",
    )
  })

  it("換圖時上傳新檔名、清單指向新檔、舊檔沒被刪", async () => {
    await publish("webp-a")
    const first = (await manifestOf()).assets["monsters/monster-1"].file

    await publish("webp-b", "source-b")

    const second = (await manifestOf()).assets["monsters/monster-1"].file
    expect(second).not.toBe(first)
    expect(storage.objects.has(`content:${first}`)).toBe(true)
    expect(storage.objects.has(`content:${second}`)).toBe(true)
  })

  it("內容圖上傳失敗時清單位元組不變", async () => {
    storage.failingBucket = "content"

    await expect(publish("webp-a")).rejects.toThrow()

    expect(await readFile(manifestPath, "utf8")).toBe(EmptyManifest)
  })

  it("原檔上傳失敗時清單位元組不變", async () => {
    storage.failingBucket = "originals"

    await expect(publish("webp-a")).rejects.toThrow()

    expect(await readFile(manifestPath, "utf8")).toBe(EmptyManifest)
  })

  it("同一張圖再匯入不上傳、清單不變", async () => {
    await publish("webp-a")
    const before = await readFile(manifestPath, "utf8")
    const putsBefore = storage.puts.length

    await publish("webp-a")

    expect(storage.puts.length).toBe(putsBefore)
    expect(await readFile(manifestPath, "utf8")).toBe(before)
  })

  it("寫出的清單鍵是排序過的", async () => {
    await publish("webp-z", "source-z", "monster-9")
    await publish("webp-a", "source-a", "monster-1")

    expect(Object.keys((await manifestOf()).assets)).toEqual([
      "monsters/monster-1",
      "monsters/monster-9",
    ])
  })

  it("立繪的 headY 記進清單", async () => {
    await publishAsset({
      category: "portraits",
      key: "job-6-male",
      webp: Buffer.from("webp-p"),
      source: Buffer.from("source-p"),
      sourceExtension: "png",
      headY: 40,
      storage,
      manifestPath,
    })

    expect((await manifestOf()).assets["portraits/job-6-male"].headY).toBe(40)
  })
})
