import ExcelJS from "exceljs"
import { expect, test } from "vitest"
import type { Table } from "./tables.ts"
import { readWorkbook, WorkbookError, writeWorkbook } from "./workbook.ts"

const tables: Table[] = [
  {
    name: "monsters",
    headers: ["id", "name", "hp", "boss", "subPlaces"],
    rows: [
      ["monster-1", "史萊姆", 32, false, "[1]"],
      ["monster-2", "多行\n說明", 1.05, null, null],
    ],
  },
  { name: "_columns", headers: ["table", "column"], rows: [["monsters", "id"]] },
]

test("ReadWorkbook_ReadsBackWhatWasWritten", async () => {
  const buffer = await writeWorkbook(tables)

  expect(await readWorkbook(buffer)).toEqual(tables)
})

test("ReadWorkbook_TakesFormulaResultsAndRichText", async () => {
  const workbook = new ExcelJS.Workbook()
  const sheet = workbook.addWorksheet("monsters")
  sheet.addRow(["id", "hp", "name"])
  sheet.addRow([
    "monster-1",
    { formula: "=10*3", result: 30 },
    { richText: [{ text: "史" }, { text: "萊姆" }] },
  ])

  const read = await readWorkbook(Buffer.from(await workbook.xlsx.writeBuffer()))

  expect(read[0]?.rows).toEqual([["monster-1", 30, "史萊姆"]])
})

test("WriteWorkbook_RejectsSheetNamesExcelCannotHold", async () => {
  await expect(
    writeWorkbook([{ name: "a".repeat(32), headers: ["id"], rows: [] }]),
  ).rejects.toThrow(WorkbookError)
})
