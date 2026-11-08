import { randomBytes } from "node:crypto"
import { access, mkdtemp, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import sharp from "sharp"
import { beforeEach, describe, expect, it } from "vitest"
import { runCli, type CliEnvironment } from "./cli.ts"
import { FakeStorage } from "./testing/fakeStorage.ts"
import { measure, syntheticImage } from "./testing/images.ts"

let root: string
let environment: CliEnvironment

beforeEach(async () => {
  root = await mkdtemp(join(tmpdir(), "ebo-design-"))
  environment = {
    repositoryRoot: root,
    workingDirectory: root,
    createStorage: () => new FakeStorage(),
  }
})

const run = (args: string[]) =>
  runCli(args, { log: () => undefined, error: () => undefined }, environment)

const output = (group: string, name: string) =>
  join(root, "frontend/src/assets", group, `${name}.webp`)

describe("design", () => {
  it("--max-width 0 時非零結束並說明要 1 以上", async () => {
    await writeFile(
      join(root, "strip.png"),
      await syntheticImage(100, 50, [{ left: 0, top: 0, width: 100, height: 50 }]),
    )
    const lines: string[] = []

    const exitCode = await runCli(
      ["design", "ui", "paper-strip", "strip.png", "--max-width", "0"],
      { log: (line) => lines.push(line), error: (line) => lines.push(line) },
      environment,
    )

    expect(exitCode).not.toBe(0)
    expect(lines.join("\n")).toContain("--max-width 要是 1 以上的整數")
  })

  it("1200 寬的來源加 --max-width 600 輸出 600 寬", async () => {
    await writeFile(
      join(root, "strip.png"),
      await syntheticImage(1200, 300, [{ left: 0, top: 0, width: 1200, height: 150 }]),
    )

    const exitCode = await run(["design", "ui", "paper-strip", "strip.png", "--max-width", "600"])

    expect(exitCode).toBe(0)
    const measured = await measure(await sharp(output("ui", "paper-strip")).toBuffer())
    expect([measured.width, measured.height]).toEqual([600, 150])
    expect(measured.hasAlpha).toBe(true)
  })

  it("比 --max-width 窄的來源維持原尺寸", async () => {
    await writeFile(
      join(root, "icon.png"),
      await syntheticImage(200, 100, [{ left: 0, top: 0, width: 50, height: 50 }]),
    )

    await run(["design", "icons", "bag", "icon.png", "--max-width", "600"])

    expect((await measure(await sharp(output("icons", "bag")).toBuffer())).width).toBe(200)
  })

  it("超過上限時不寫檔並非零結束", async () => {
    const size = 1600
    const noise = await sharp(randomBytes(size * size * 3), {
      raw: { width: size, height: size, channels: 3 },
    })
      .png()
      .toBuffer()
    await writeFile(join(root, "noise.png"), noise)

    const exitCode = await run(["design", "scenes", "noise", "noise.png"])

    expect(exitCode).not.toBe(0)
    await expect(access(output("scenes", "noise"))).rejects.toThrow()
  })

  it("分類名稱不合格時非零結束", async () => {
    await writeFile(
      join(root, "a.png"),
      await syntheticImage(10, 10, [{ left: 0, top: 0, width: 5, height: 5 }]),
    )

    expect(await run(["design", "UI", "a", "a.png"])).not.toBe(0)
  })
})
