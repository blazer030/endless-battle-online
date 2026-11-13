import { mkdir, mkdtemp, readFile, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { beforeEach, expect, test } from "vitest"
import { runCli } from "./cli.ts"
import { readWorkbook, writeWorkbook } from "./workbook.ts"

const monsters = '{\n  "monsters": [\n    {"id": "monster-1", "name": "史萊姆", "hp": 32}\n  ]\n}\n'
const inn = '{\n  "seconds": 28800,\n  "rooms": [\n    {"id": "loft", "multiplier": 1.10}\n  ]\n}\n'

let root: string
let lines: string[]
let errors: string[]

const run = (...args: string[]) =>
  runCli(
    args,
    { log: (line) => lines.push(line), error: (line) => errors.push(line) },
    { repositoryRoot: root, workingDirectory: root },
  )

const content = (name: string) => readFile(join(root, "backend/content", name), "utf8")

beforeEach(async () => {
  root = await mkdtemp(join(tmpdir(), "content-sheet-"))
  await mkdir(join(root, "backend/content"), { recursive: true })
  await writeFile(join(root, "backend/content/monsters.json"), monsters)
  await writeFile(join(root, "backend/content/inn.json"), inn)
  lines = []
  errors = []
})

test("Export_WritesOneSheetPerTable", async () => {
  expect(await run("export", "out.xlsx")).toBe(0)

  const tables = await readWorkbook(await readFile(join(root, "out.xlsx")))
  expect(tables.map((table) => table.name)).toEqual([
    "inn.rooms",
    "monsters",
    "settings",
    "_files",
    "_tables",
    "_columns",
  ])
})

test("Import_UnchangedWorkbookLeavesFilesAsTheyWere", async () => {
  await run("export", "out.xlsx")

  expect(await run("import", "out.xlsx")).toBe(0)

  expect(await content("monsters.json")).toBe(monsters)
  expect(await content("inn.json")).toBe(inn)
  expect(lines.at(-1)).toBe("內容檔沒有變更")
})

test("Import_WritesChangedFilesAndListsThem", async () => {
  await run("export", "out.xlsx")
  const tables = await readWorkbook(await readFile(join(root, "out.xlsx")))
  const sheet = tables.find((table) => table.name === "monsters")!
  sheet.rows[0]![sheet.headers.indexOf("hp")] = 40
  await writeFile(join(root, "edited.xlsx"), await writeWorkbook(tables))

  expect(await run("import", "edited.xlsx")).toBe(0)

  expect(await content("monsters.json")).toContain('"hp": 40}')
  expect(await content("inn.json")).toBe(inn)
  expect(lines).toContain("已更新 monsters.json")
})

test("Import_ReportsProblemsWithoutWriting", async () => {
  await run("export", "out.xlsx")
  const tables = await readWorkbook(await readFile(join(root, "out.xlsx")))
  const sheet = tables.find((table) => table.name === "monsters")!
  sheet.rows[0]![sheet.headers.indexOf("hp")] = "很多"
  await writeFile(join(root, "edited.xlsx"), await writeWorkbook(tables))

  expect(await run("import", "edited.xlsx")).toBe(1)

  expect(errors.join("\n")).toContain("hp 應該是數字")
  expect(await content("monsters.json")).toBe(monsters)
})

// 匯出後內容檔又被改過（例如 admin 發布或 git pull），舊試算表不能蓋掉那些修改
test("Import_RefusesFilesChangedSinceExport", async () => {
  await run("export", "out.xlsx")
  const tables = await readWorkbook(await readFile(join(root, "out.xlsx")))
  const sheet = tables.find((table) => table.name === "monsters")!
  sheet.rows[0]![sheet.headers.indexOf("hp")] = 40
  await writeFile(join(root, "edited.xlsx"), await writeWorkbook(tables))
  const newer = monsters.replace('"hp": 32', '"hp": 35')
  await writeFile(join(root, "backend/content/monsters.json"), newer)

  expect(await run("import", "edited.xlsx")).toBe(1)

  expect(errors.join("\n")).toContain("monsters.json 在匯出後改過")
  expect(await content("monsters.json")).toBe(newer)
})

test("Import_ForceOverwritesFilesChangedSinceExport", async () => {
  await run("export", "out.xlsx")
  await writeFile(
    join(root, "backend/content/monsters.json"),
    monsters.replace('"hp": 32', '"hp": 35'),
  )

  expect(await run("import", "out.xlsx", "--force")).toBe(0)

  expect(await content("monsters.json")).toBe(monsters)
})

test("Cli_UnknownCommandShowsUsage", async () => {
  expect(await run("upload")).toBe(1)

  expect(errors.join("\n")).toContain("用法")
})
