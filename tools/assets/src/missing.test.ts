import { mkdtemp, mkdir, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { beforeEach, describe, expect, it } from "vitest"
import { runCli, type CliEnvironment } from "./cli.ts"
import { FakeStorage } from "./testing/fakeStorage.ts"

let root: string
let environment: CliEnvironment

const writeJson = (path: string, value: unknown) =>
  writeFile(join(root, path), JSON.stringify(value))

beforeEach(async () => {
  root = await mkdtemp(join(tmpdir(), "ebo-missing-"))
  await mkdir(join(root, "backend/content"), { recursive: true })
  await mkdir(join(root, "frontend/src/assets"), { recursive: true })
  const jobIds = Array.from({ length: 12 }, (_, index) => `job-${index + 1}`)
  await writeJson("backend/content/jobs.json", { jobs: jobIds.map((id) => ({ id })) })
  await writeJson("backend/content/skills.json", { skills: [{ id: "skill-1" }, { id: "skill-2" }] })
  await writeJson("backend/content/items.json", { items: [{ id: "item-1" }] })
  await writeJson("backend/content/monsters.json", {
    monsters: [{ id: "monster-1" }, { id: "monster-2" }],
  })
  await writeJson("backend/content/world.json", { places: [{ id: 1 }, { id: 2 }, { id: 3 }] })
  await writeJson("backend/content/inn.json", {
    rooms: ["loft", "bunk", "tower", "bulwark", "chapel", "chamber"].map((id) => ({ id })),
  })
  // 清單有 job-1～job-4 的男女立繪（8 張）與 monster-1
  const assets: Record<string, { file: string; original: string }> = {}
  for (const id of jobIds.slice(0, 4))
    for (const variant of ["male", "female"])
      assets[`portraits/${id}-${variant}`] = { file: "f", original: "o" }
  assets["monsters/monster-1"] = { file: "f", original: "o" }
  await writeJson("frontend/src/assets/content-manifest.json", { baseUrl: "https://x", assets })
  environment = {
    repositoryRoot: root,
    workingDirectory: root,
    createStorage: () => new FakeStorage(),
  }
})

const run = async (args: string[]) => {
  const lines: string[] = []
  const exitCode = await runCli(
    args,
    { log: (line) => lines.push(line), error: (line) => lines.push(line) },
    environment,
  )
  return { exitCode, text: lines.join("\n") }
}

describe("missing", () => {
  it("清單有 8 張立繪、內容有 12 個職業時顯示 8／24 並列出 16 個鍵", async () => {
    const { exitCode, text } = await run(["missing", "portraits"])

    expect(exitCode).toBe(0)
    expect(text).toContain("portraits：有圖 8／應有 24")
    const missing = ["male", "female"].flatMap((variant) =>
      Array.from({ length: 8 }, (_, index) => `job-${index + 5}-${variant}`),
    )
    for (const key of missing) expect(text).toContain(key)
    expect(text).not.toContain("job-1-male")
  })

  // 規格 content-assets：Room keys come from the inn
  it("房型兩類的鍵來自 inn.json 的房間", async () => {
    const { exitCode, text } = await run(["missing", "room-badges"])

    expect(exitCode).toBe(0)
    expect(text).toContain("room-badges：有圖 0／應有 6")
    expect(text).toContain("loft、bunk、tower、bulwark、chapel、chamber")
  })

  it("沒有指定類別時八類都列出", async () => {
    const { exitCode, text } = await run(["missing"])

    expect(exitCode).toBe(0)
    for (const line of [
      "portraits：有圖 8／應有 24",
      "job-icons：有圖 0／應有 12",
      "skills：有圖 0／應有 2",
      "items：有圖 0／應有 1",
      "monsters：有圖 1／應有 2",
      "battlefields：有圖 0／應有 3",
      "room-badges：有圖 0／應有 6",
      "room-backgrounds：有圖 0／應有 6",
    ])
      expect(text).toContain(line)
  })

  it("只列出還沒有圖的怪物", async () => {
    const { text } = await run(["missing", "monsters"])

    expect(text).toContain("monster-2")
    expect(text).not.toContain("monster-1、")
  })

  it("清單有、內容沒有的鍵另外列出", async () => {
    await writeJson("frontend/src/assets/content-manifest.json", {
      baseUrl: "https://x",
      assets: {
        "monsters/monster-1": { file: "f", original: "o" },
        "monsters/monster-l": { file: "f", original: "o" },
      },
    })

    const { text } = await run(["missing", "monsters"])

    expect(text).toContain("monsters：有圖 1／應有 2")
    expect(text).toContain("多：monster-l")
  })

  it("類別不認得時非零結束", async () => {
    expect((await run(["missing", "weapons"])).exitCode).not.toBe(0)
  })
})
