import { readdir, readFile } from "node:fs/promises"
import { join } from "node:path"
import { beforeAll, describe, expect, test } from "vitest"
import { fromTables, TableError, toTables, type ContentFiles, type Table } from "./tables.ts"
import { contentDirectory } from "./testing/paths.ts"

const tableOf = (tables: readonly Table[], name: string) => {
  const table = tables.find((candidate) => candidate.name === name)
  if (table === undefined) throw new Error(`沒有工作表 ${name}`)
  return table
}

const column = (table: Table, header: string) => {
  const index = table.headers.indexOf(header)
  if (index < 0) throw new Error(`${table.name} 沒有欄位 ${header}`)
  return index
}

const rowWhere = (table: Table, header: string, value: unknown) => {
  const row = table.rows.find((candidate) => candidate[column(table, header)] === value)
  if (row === undefined) throw new Error(`${table.name} 沒有 ${header}=${String(value)}`)
  return row
}

const changedLines = (before: string, after: string) => {
  const old = before.split("\n")
  return after.split("\n").filter((line, index) => line !== old[index])
}

let realFiles: ContentFiles

beforeAll(async () => {
  const names = (await readdir(contentDirectory)).filter((name) => name.endsWith(".json"))
  realFiles = new Map(
    await Promise.all(
      names.map(
        async (name) => [name, await readFile(join(contentDirectory, name), "utf8")] as const,
      ),
    ),
  )
})

describe("real content", () => {
  test("FromTables_UnchangedTablesReproduceEveryFileExactly", () => {
    const files = fromTables(toTables(realFiles), realFiles)

    expect([...files.keys()].sort()).toEqual([...realFiles.keys()].sort())
    for (const [name, text] of realFiles) expect(files.get(name), name).toBe(text)
  })

  // 沒有原檔時欄位順序照工作表，值要完全一樣
  test("FromTables_WithoutTheCurrentFilesRebuildsTheSameValues", () => {
    const files = fromTables(toTables(realFiles), new Map())

    for (const [name, text] of realFiles)
      expect(JSON.parse(files.get(name) ?? "null"), name).toStrictEqual(JSON.parse(text))
  })

  test("FromTables_ChangedCellRewritesOnlyThatRecord", () => {
    const tables = toTables(realFiles)
    const monsters = tableOf(tables, "monsters")
    rowWhere(monsters, "id", "monster-1")[column(monsters, "hp")] = 999

    const after = fromTables(tables, realFiles).get("monsters.json") ?? ""

    const changed = changedLines(realFiles.get("monsters.json") ?? "", after)
    expect(changed).toHaveLength(1)
    expect(changed[0]).toContain('"id": "monster-1"')
    expect(changed[0]).toContain('"hp": 999,')
  })

  test("ToTables_NameColumnsShowWhatTheIdsPointTo", () => {
    const drops = tableOf(toTables(realFiles), "monsters.drops")
    const first = rowWhere(drops, "$parent", "monster-1")

    expect(first[column(drops, "$parent（名稱）")]).toBe("史萊姆")
    expect(typeof first[column(drops, "itemId（名稱）")]).toBe("string")
  })

  test("FromTables_IgnoresEditedNameColumns", () => {
    const tables = toTables(realFiles)
    const drops = tableOf(tables, "monsters.drops")
    for (const row of drops.rows) row[column(drops, "itemId（名稱）")] = "改過的名字"

    expect(fromTables(tables, realFiles).get("monsters.json")).toBe(realFiles.get("monsters.json"))
  })

  test("FromTables_AddedChildRowJoinsItsParent", () => {
    const tables = toTables(realFiles)
    const drops = tableOf(tables, "monsters.drops")
    const row: (string | number | boolean | null)[] = drops.headers.map(() => null)
    row[column(drops, "$parent")] = "monster-1"
    row[column(drops, "itemId")] = "item-1"
    row[column(drops, "topr")] = 5
    drops.rows.push(row)

    const after = fromTables(tables, realFiles).get("monsters.json") ?? ""

    const monster = JSON.parse(after).monsters.find(
      (entry: { id: string }) => entry.id === "monster-1",
    )
    expect(monster.drops.at(-1)).toEqual({ itemId: "item-1", topr: 5 })
  })
})

describe("blank cells", () => {
  const files: ContentFiles = new Map([
    [
      "things.json",
      '{\n  "things": [\n' +
        '    {"id": "a", "note": "", "parentId": null, "tags": [], "parts": [{"n": 1}]},\n' +
        '    {"id": "b", "note": "x", "parentId": "a", "tags": ["t"], "parts": []},\n' +
        '    {"id": "c", "note": "", "parentId": null, "tags": ["u"], "parts": [{"n": 2}], "extra": ""}\n' +
        "  ]\n}\n",
    ],
  ])

  test("FromTables_BlankCellsKeepEmptyTextsNullsAndEmptyLists", () => {
    const rebuilt = fromTables(toTables(files), new Map()).get("things.json") ?? ""

    expect(JSON.parse(rebuilt)).toEqual(JSON.parse(files.get("things.json") ?? ""))
  })

  test("ToTables_WritesCountsForChildLists", () => {
    const things = tableOf(toTables(files), "things")

    expect(things.rows.map((row) => row[column(things, "parts")])).toEqual([1, 0, 1])
  })

  test("FromTables_NumberTypedIntoTextColumnStaysText", () => {
    const tables = toTables(files)
    const things = tableOf(tables, "things")
    rowWhere(things, "id", "b")[column(things, "note")] = 42

    const rebuilt = JSON.parse(fromTables(tables, new Map()).get("things.json") ?? "")

    expect(rebuilt.things[1].note).toBe("42")
  })

  test("FromTables_RejectsChildRowsWithoutAParent", () => {
    const tables = toTables(files)
    const parts = tableOf(tables, "things.parts")
    parts.rows.push(parts.headers.map((header) => (header === "$parent" ? "missing" : null)))

    expect(() => fromTables(tables, new Map())).toThrow(TableError)
  })

  test("FromTables_RejectsUnknownColumns", () => {
    const tables = toTables(files)
    tableOf(tables, "things").headers.push("typo")

    expect(() => fromTables(tables, new Map())).toThrow(/typo/)
  })
})
