import ExcelJS from "exceljs"
import type { Cell, Table } from "./tables.ts"

export class WorkbookError extends Error {}

const MaximumSheetName = 31
const MaximumColumnWidth = 40
const HeaderFill = "FFF3F3F3"

const widthOf = (table: Table, column: number) => {
  const lengths = [
    table.headers[column],
    ...table.rows.slice(0, 200).map((row) => row[column]),
  ].map((cell) =>
    cell === null || cell === undefined ? 0 : [...String(cell).split("\n")[0]!].length,
  )
  return Math.min(MaximumColumnWidth, Math.max(6, ...lengths) + 2)
}

export const writeWorkbook = async (tables: readonly Table[]): Promise<Buffer> => {
  const workbook = new ExcelJS.Workbook()
  for (const table of tables) {
    if (table.name.length > MaximumSheetName || /[[\]:*?/\\]/.test(table.name))
      throw new WorkbookError(`工作表名稱 ${table.name} 太長或有 Excel 不接受的字元`)
    const sheet = workbook.addWorksheet(table.name)
    sheet.addRow(table.headers)
    for (const row of table.rows) sheet.addRow(row)
    const header = sheet.getRow(1)
    header.font = { bold: true }
    header.fill = { type: "pattern", pattern: "solid", fgColor: { argb: HeaderFill } }
    sheet.views = [{ state: "frozen", ySplit: 1 }]
    table.headers.forEach((_, index) => {
      sheet.getColumn(index + 1).width = widthOf(table, index)
    })
  }
  return Buffer.from(await workbook.xlsx.writeBuffer())
}

const cellOf = (value: ExcelJS.CellValue, where: string): Cell => {
  if (value === null || value === undefined) return null
  if (typeof value === "string" || typeof value === "number" || typeof value === "boolean")
    return value
  if (value instanceof Date) return value.toISOString()
  if ("richText" in value) return value.richText.map((part) => part.text).join("")
  if ("formula" in value || "sharedFormula" in value) {
    const result = (value as ExcelJS.CellFormulaValue).result
    if (result !== null && typeof result === "object" && !(result instanceof Date))
      throw new WorkbookError(`${where} 的公式結果是錯誤`)
    return cellOf(result ?? null, where)
  }
  if ("hyperlink" in value) return value.text
  if ("error" in value) throw new WorkbookError(`${where} 是錯誤值 ${value.error}`)
  throw new WorkbookError(`${where} 的內容讀不懂`)
}

export const readWorkbook = async (buffer: Buffer): Promise<Table[]> => {
  const workbook = new ExcelJS.Workbook()
  await workbook.xlsx.load(buffer as unknown as ArrayBuffer)
  return workbook.worksheets.map((sheet) => {
    const width = sheet.getRow(1).cellCount
    const headers = Array.from({ length: width }, (_, index) =>
      String(cellOf(sheet.getRow(1).getCell(index + 1).value, `${sheet.name} 第 1 列`) ?? ""),
    )
    while (headers.length > 0 && headers.at(-1) === "") headers.pop()
    const rows: Cell[][] = []
    for (let number = 2; number <= sheet.rowCount; number++) {
      const row = sheet.getRow(number)
      rows.push(
        headers.map((_, index) =>
          cellOf(row.getCell(index + 1).value, `${sheet.name} 第 ${number} 列`),
        ),
      )
    }
    while (rows.length > 0 && rows.at(-1)!.every((cell) => cell === null)) rows.pop()
    return { name: sheet.name, headers, rows }
  })
}
