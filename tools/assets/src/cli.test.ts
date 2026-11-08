import { randomBytes } from "node:crypto"
import { access, mkdir, mkdtemp, readFile, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import sharp from "sharp"
import { beforeEach, describe, expect, it } from "vitest"
import { runCli, type CliEnvironment } from "./cli.ts"
import { FakeStorage } from "./testing/fakeStorage.ts"
import { syntheticImage } from "./testing/images.ts"

const EmptyManifest = '{\n  "baseUrl": "https://ebo-assets.blazer030.dev",\n  "assets": {}\n}\n'

const capture = () => {
  const lines: string[] = []
  return {
    lines,
    output: { log: (line: string) => lines.push(line), error: (line: string) => lines.push(line) },
  }
}

let root: string
let storage: FakeStorage
let storageCreated: boolean
let environment: CliEnvironment
const manifestPath = () => join(root, "frontend/src/assets/content-manifest.json")

beforeEach(async () => {
  root = await mkdtemp(join(tmpdir(), "ebo-cli-"))
  await mkdir(join(root, "frontend/src/assets"), { recursive: true })
  await writeFile(manifestPath(), EmptyManifest)
  // import 會比對內容裡有沒有這個鍵
  await mkdir(join(root, "backend/content"), { recursive: true })
  const content: Record<string, unknown> = {
    "jobs.json": { jobs: [{ id: "job-6" }] },
    "skills.json": { skills: [] },
    "items.json": { items: [] },
    "monsters.json": { monsters: [{ id: "monster-1" }] },
    "world.json": { places: [{ id: 1 }] },
  }
  for (const [file, value] of Object.entries(content))
    await writeFile(join(root, "backend/content", file), JSON.stringify(value))
  await writeFile(
    join(root, "monster.png"),
    await syntheticImage(300, 300, [{ left: 50, top: 50, width: 200, height: 200 }]),
  )
  storage = new FakeStorage()
  storageCreated = false
  environment = {
    repositoryRoot: root,
    workingDirectory: root,
    createStorage: () => {
      storageCreated = true
      return storage
    },
  }
})

describe("runCli", () => {
  it("未知指令以非零 exit code 結束並印出可用指令", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(["unknown"], output, environment)

    expect(exitCode).not.toBe(0)
    const text = lines.join("\n")
    for (const command of ["import", "missing", "design"]) expect(text).toContain(command)
  })
})

describe("import", () => {
  it("沒有 .env.r2 時 --dry-run 產生預覽並以 0 結束、清單不變", async () => {
    const { output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "monster.png", "--dry-run"],
      output,
      environment,
    )

    expect(exitCode).toBe(0)
    await access(join(root, ".asset-preview/monsters/monster-1.png"))
    expect(await readFile(manifestPath(), "utf8")).toBe(EmptyManifest)
    expect(storageCreated).toBe(false)
  })

  it("非 dry-run 缺金鑰時非零結束且不上傳", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "monster.png"],
      output,
      environment,
    )

    expect(exitCode).not.toBe(0)
    expect(lines.join("\n")).toContain(".env.r2")
    expect(storageCreated).toBe(false)
    expect(await readFile(manifestPath(), "utf8")).toBe(EmptyManifest)
  })

  it("有金鑰時上傳並把內容圖寫進清單", async () => {
    await writeFile(
      join(root, ".env.r2"),
      "R2_ENDPOINT=https://e\nR2_ACCESS_KEY_ID=id\nR2_SECRET_ACCESS_KEY=secret\n",
    )
    const { output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "monster.png"],
      output,
      environment,
    )

    expect(exitCode).toBe(0)
    const manifest = JSON.parse(await readFile(manifestPath(), "utf8"))
    expect(manifest.assets["monsters/monster-1"].file).toMatch(
      /^monsters\/monster-1\.[0-9a-f]{12}\.webp$/,
    )
    expect(storage.puts).toHaveLength(2)
  })

  it("上傳失敗時非零結束且清單不變", async () => {
    await writeFile(
      join(root, ".env.r2"),
      "R2_ENDPOINT=https://e\nR2_ACCESS_KEY_ID=id\nR2_SECRET_ACCESS_KEY=secret\n",
    )
    storage.failingBucket = "content"
    const { output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "monster.png"],
      output,
      environment,
    )

    expect(exitCode).not.toBe(0)
    expect(await readFile(manifestPath(), "utf8")).toBe(EmptyManifest)
  })

  describe("有金鑰但輸入不合格時非零結束、不上傳、清單不變", () => {
    beforeEach(async () => {
      await writeFile(
        join(root, ".env.r2"),
        "R2_ENDPOINT=https://e\nR2_ACCESS_KEY_ID=id\nR2_SECRET_ACCESS_KEY=secret\n",
      )
      await writeFile(
        join(root, "portrait.png"),
        await syntheticImage(400, 1000, [{ left: 100, top: 0, width: 200, height: 1000 }]),
      )
      // 雜訊圖壓到品質 60 仍遠超過戰鬥背景的 150 KB
      const noise = randomBytes(1024 * 1024 * 3)
      await writeFile(
        join(root, "noise.png"),
        await sharp(noise, { raw: { width: 1024, height: 1024, channels: 3 } })
          .png()
          .toBuffer(),
      )
    })

    it.each([
      ["品質降到 60 仍超過上限", ["battlefields", "1", "noise.png"]],
      ["類別不認得", ["weapons", "sword-1", "monster.png"]],
      ["鍵含大寫", ["monsters", "Monster-1", "monster.png"]],
      ["鍵不在內容裡", ["monsters", "monster-l", "monster.png"]],
      ["立繪漏了 -male／-female", ["portraits", "job-6", "portrait.png"]],
    ])("%s", async (_, args) => {
      const { output } = capture()

      const exitCode = await runCli(["import", ...args], output, environment)

      expect(exitCode).not.toBe(0)
      expect(storage.puts).toEqual([])
      expect(await readFile(manifestPath(), "utf8")).toBe(EmptyManifest)
    })
  })

  it("--dry-run 時鍵不在內容裡只警告", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-l", "monster.png", "--dry-run"],
      output,
      environment,
    )

    expect(exitCode).toBe(0)
    expect(lines.join("\n")).toContain("內容裡沒有 monsters/monster-l")
  })

  it("--head-y 只能用在立繪", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "monster.png", "--head-y", "10", "--dry-run"],
      output,
      environment,
    )

    expect(exitCode).not.toBe(0)
    expect(lines.join("\n")).toContain("--head-y")
  })

  it("來源圖讀不到時非零結束", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(
      ["import", "monsters", "monster-1", "nope.png", "--dry-run"],
      output,
      environment,
    )

    expect(exitCode).not.toBe(0)
    expect(lines.join("\n")).toContain("nope.png")
  })
})
