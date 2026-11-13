import { createHash } from "node:crypto"
import {
  formatContentFile,
  formatInline,
  parseContentFile,
  type ContentProperty,
  type ContentRecord,
  type Json,
  type JsonObject,
} from "./contentFile.ts"

export type Cell = string | number | boolean | null

export type Table = { name: string; headers: string[]; rows: Cell[][] }

// 檔名 → 檔案內容
export type ContentFiles = Map<string, string>

export class TableError extends Error {}

// 說明用的工作表：檔案由哪些屬性組成、每張工作表從哪裡來、每個欄位怎麼還原
export const FilesTable = "_files"
export const TablesTable = "_tables"
export const ColumnsTable = "_columns"
export const SettingsTable = "settings"
export const KeyColumn = "$key"
export const ParentColumn = "$parent"
export const NameSuffix = "（名稱）"

// 空白格代表的值；跟它不同的 null、空字串改寫成標記
const NullMarker = "null"
const EmptyMarker = '""'

type Kind = "string" | "number" | "boolean" | "mixed" | "json" | "count" | "object"
type Blank = "omit" | "null" | "empty"

type ColumnSpec = { path: string[]; kind: Kind; blank: Blank }

type TableSpec = {
  name: string
  parent: string | null
  field: string
  key: "id" | typeof KeyColumn | null
  columns: ColumnSpec[]
}

type Row = { key: string; parentKey: string | null; value: JsonObject }

const isObject = (value: Json | undefined): value is JsonObject =>
  typeof value === "object" && value !== null && !Array.isArray(value)

const headerOf = (path: readonly string[]) => path.join(".")

const isBlank = (cell: Cell | undefined) => cell === null || cell === undefined || cell === ""

const kindOf = (values: readonly Json[]): Kind => {
  const present = values.filter((value) => value !== null)
  if (present.length > 0 && present.every(isObject) && present.length === values.length)
    return "object"
  if (present.length > 0 && present.every(Array.isArray)) {
    const elements = present.flat()
    if (elements.length > 0 && elements.every(isObject) && present.length === values.length)
      return "count"
    if (elements.every((element) => !isObject(element) && !Array.isArray(element))) return "json"
  }
  if (present.some((value) => typeof value === "object")) return "json"
  const types = new Set(present.map((value) => typeof value))
  if (types.size !== 1) return "mixed"
  const [only] = types
  return only === "string" ? "string" : only === "number" ? "number" : "boolean"
}

// 欄位順序：照各筆第一次出現的位置合併
const mergedKeys = (objects: readonly JsonObject[]) => {
  const keys: string[] = []
  for (const object of objects) {
    let previous = -1
    for (const key of Object.keys(object)) {
      const at = keys.indexOf(key)
      if (at >= 0) previous = at
      else keys.splice(++previous, 0, key)
    }
  }
  return keys
}

const analyze = (objects: readonly JsonObject[], prefix: string[], table: string): ColumnSpec[] =>
  mergedKeys(objects).flatMap((key) => {
    if (key.includes(".") || key.startsWith("$"))
      throw new TableError(`${table} 的欄位名稱 ${key} 不能有「.」或以「$」開頭`)
    const path = [...prefix, key]
    const holders = objects.filter((object) => key in object)
    const values = holders.map((object) => object[key] as Json)
    const absent = objects.length - holders.length
    const kind = kindOf(values)
    if (kind === "object") {
      const nested = analyze(values.filter(isObject), path, table)
      return [{ path, kind, blank: absent > 0 ? "omit" : "null" } as ColumnSpec, ...nested]
    }
    if (kind === "json" || kind === "count") return [{ path, kind, blank: "omit" }]
    const nulls = values.filter((value) => value === null).length
    const empties = values.filter((value) => value === "").length
    for (const value of values)
      if (value === NullMarker || value === EmptyMarker)
        throw new TableError(`${table}.${headerOf(path)} 有文字 ${value}，跟空白標記衝突`)
    const blank: Blank =
      absent > 0 ? "omit" : nulls >= empties && nulls > 0 ? "null" : empties > 0 ? "empty" : "omit"
    return [{ path, kind, blank }]
  })

const valueAt = (object: JsonObject, path: readonly string[]): Json | undefined => {
  let current: Json | undefined = object
  for (const key of path) {
    if (!isObject(current) || !(key in current)) return undefined
    current = current[key]
  }
  return current
}

const encode = (spec: ColumnSpec, value: Json | undefined): Cell => {
  if (value === undefined) return null
  if (spec.kind === "count") return Array.isArray(value) ? value.length : null
  if (spec.kind === "json") return formatInline(value)
  if (value === null) return spec.blank === "null" ? null : NullMarker
  if (value === "") return spec.blank === "empty" ? null : EmptyMarker
  if (typeof value === "object") return formatInline(value)
  return value
}

const decode = (spec: ColumnSpec, cell: Cell, where: string): Json | undefined => {
  if (isBlank(cell)) return spec.blank === "omit" ? undefined : spec.blank === "null" ? null : ""
  if (spec.kind === "json") return typeof cell === "string" ? (JSON.parse(cell) as Json) : cell
  if (cell === NullMarker) return null
  if (cell === EmptyMarker) return ""
  if (spec.kind === "string") return String(cell)
  if (spec.kind === "number") {
    const number = typeof cell === "number" ? cell : Number(cell)
    if (typeof cell === "boolean" || Number.isNaN(number))
      throw new TableError(`${where} 應該是數字：${String(cell)}`)
    return number
  }
  if (spec.kind === "boolean") {
    if (typeof cell === "boolean") return cell
    const text = String(cell).toLowerCase()
    if (text === "true" || text === "false") return text === "true"
    throw new TableError(`${where} 應該是 TRUE 或 FALSE：${String(cell)}`)
  }
  return cell
}

// 名稱對照：有 id 的紀錄 → name、title，配方用成品的名稱；placeId → 地點名稱
type Names = { ids: Map<string, string>; places: Map<number, string> }

const nameOf = (names: Names, path: readonly string[], cell: Cell): string | undefined => {
  if (typeof cell === "number" && path.at(-1) === "placeId") return names.places.get(cell)
  // 孫表的 $parent 是「技能 id/第幾等級」：顯示技能名稱加上後面的編號
  if (typeof cell === "string" && path[0] === ParentColumn && cell.includes("/")) {
    const [root = "", ...rest] = cell.split("/")
    const name = names.ids.get(root)
    return name === undefined ? undefined : [name, ...rest].join("/")
  }
  return typeof cell === "string" ? names.ids.get(cell) : undefined
}

const collectNames = (roots: readonly { name: string; rows: readonly Row[] }[]): Names => {
  const names: Names = { ids: new Map(), places: new Map() }
  const records = roots.flatMap((root) => root.rows.map((row) => row.value))
  for (const record of records) {
    const label = record["name"] ?? record["title"]
    if (typeof record["id"] === "string" && typeof label === "string")
      names.ids.set(record["id"], label)
  }
  for (const record of records) {
    const result = record["resultItemId"]
    if (
      typeof record["id"] === "string" &&
      typeof result === "string" &&
      !names.ids.has(record["id"])
    )
      names.ids.set(record["id"], names.ids.get(result) ?? result)
  }
  for (const root of roots)
    if (root.name === "world.places")
      for (const { value } of root.rows)
        if (typeof value["id"] === "number" && typeof value["name"] === "string")
          names.places.set(value["id"], value["name"])
  return names
}

type Built = { spec: TableSpec; rows: Row[] }

const buildTables = (
  name: string,
  parent: string | null,
  field: string,
  rows: Row[],
  hasId: boolean,
): Built[] => {
  const columns = analyze(
    rows.map((row) => row.value),
    [],
    name,
  )
  const children = columns.filter((column) => column.kind === "count")
  // 有子表才需要列的鍵：有 id 用 id，否則另加 $key 欄
  const key = hasId ? "id" : children.length > 0 ? KeyColumn : null
  const spec: TableSpec = { name, parent, field, key, columns }
  return [
    { spec, rows },
    ...children.flatMap((column) => {
      const childRows = rows.flatMap((row) => {
        const items = valueAt(row.value, column.path)
        if (!Array.isArray(items)) return []
        return items.map((item, index) => ({
          key: `${row.key}/${index + 1}`,
          parentKey: row.key,
          value: item as JsonObject,
        }))
      })
      return buildTables(
        `${name}.${headerOf(column.path)}`,
        name,
        headerOf(column.path),
        childRows,
        false,
      )
    }),
  ]
}

const sheetOf = ({ spec, rows }: Built, names: Names): Table => {
  const leaves = spec.columns.filter((column) => column.kind !== "object")
  const headers: string[] = []
  const cells: ((row: Row) => Cell)[] = []
  const addNamed = (header: string, path: string[], cellOf: (row: Row) => Cell) => {
    headers.push(header)
    cells.push(cellOf)
    if (header === "id" && spec.parent === null) return
    const named = rows.some((row) => nameOf(names, path, cellOf(row)) !== undefined)
    if (!named) return
    headers.push(`${header}${NameSuffix}`)
    cells.push((row) => nameOf(names, path, cellOf(row)) ?? null)
  }
  if (spec.key === KeyColumn) {
    headers.push(KeyColumn)
    cells.push((row) => row.key)
  }
  if (spec.parent !== null) addNamed(ParentColumn, [ParentColumn], (row) => row.parentKey)
  for (const column of leaves)
    addNamed(headerOf(column.path), column.path, (row) =>
      encode(column, valueAt(row.value, column.path)),
    )
  return { name: spec.name, headers, rows: rows.map((row) => cells.map((cellOf) => cellOf(row))) }
}

const isTableArray = (value: Json): value is JsonObject[] =>
  Array.isArray(value) && value.length > 0 && value.every(isObject)

export const toTables = (files: ContentFiles): Table[] => {
  const built: Built[] = []
  const fileRows: Cell[][] = []
  const settings: Cell[][] = []
  for (const [file, text] of [...files].sort(([a], [b]) => a.localeCompare(b))) {
    const root = JSON.parse(text) as Json
    if (!isObject(root)) throw new TableError(`${file} 的最外層必須是物件`)
    const entries = Object.entries(root)
    const base = file.replace(/\.json$/, "")
    for (const [property, value] of entries) {
      if (!isTableArray(value)) {
        fileRows.push([file, property, SettingsTable, fingerprintOf(text)])
        const scalar = value === null || typeof value !== "object"
        settings.push([
          file,
          property,
          scalar ? value : formatInline(value),
          scalar ? "value" : "json",
        ])
        continue
      }
      const name = entries.length === 1 ? base : `${base}.${property}`
      fileRows.push([file, property, name, fingerprintOf(text)])
      const hasId = value.every((record) => typeof record["id"] === "string")
      const rows = value.map((record, index) => ({
        key: hasId ? (record["id"] as string) : `#${index + 1}`,
        parentKey: null,
        value: record,
      }))
      built.push(...buildTables(name, null, property, rows, hasId))
    }
  }
  const names = collectNames(
    built
      .filter((table) => table.spec.parent === null)
      .map((table) => ({ name: table.spec.name, rows: table.rows })),
  )
  return [
    ...built.map((table) => sheetOf(table, names)),
    { name: SettingsTable, headers: ["file", "key", "value", "kind"], rows: settings },
    { name: FilesTable, headers: ["file", "property", "source", "fingerprint"], rows: fileRows },
    {
      name: TablesTable,
      headers: ["table", "parent", "field", "key"],
      rows: built.map(({ spec }) => [spec.name, spec.parent, spec.field, spec.key]),
    },
    {
      name: ColumnsTable,
      headers: ["table", "column", "kind", "blank"],
      rows: built.flatMap(({ spec }) =>
        spec.columns.map((column) => [spec.name, headerOf(column.path), column.kind, column.blank]),
      ),
    },
  ]
}

// 匯出當下內容檔的指紋；匯入時用來發現試算表匯出後內容檔又被改過
export const fingerprintOf = (text: string) =>
  createHash("sha256").update(text).digest("hex").slice(0, 16)

export const exportedFingerprints = (tables: readonly Table[]): Map<string, string> => {
  const files = tables.find((table) => table.name === FilesTable)
  if (files === undefined) throw new TableError(`缺少工作表 ${FilesTable}`)
  const file = files.headers.indexOf("file")
  const fingerprint = files.headers.indexOf("fingerprint")
  return new Map(
    files.rows
      .filter((row) => !isBlank(row[file]) && !isBlank(row[fingerprint]))
      .map((row) => [String(row[file]), String(row[fingerprint])]),
  )
}

// ---- 還原 ----

const text = (cell: Cell | undefined) => (isBlank(cell) ? null : String(cell))

const readSchema = (byName: Map<string, Table>) => {
  const need = (name: string) => {
    const table = byName.get(name)
    if (table === undefined) throw new TableError(`缺少工作表 ${name}`)
    return table
  }
  const records = (name: string) => {
    const table = need(name)
    return table.rows
      .filter((row) => !row.every(isBlank))
      .map((row) =>
        Object.fromEntries(table.headers.map((header, index) => [header, row[index] ?? null])),
      )
  }
  const specs = records(TablesTable).map((row): TableSpec => ({
    name: String(row["table"]),
    parent: text(row["parent"]),
    field: String(row["field"]),
    key: (text(row["key"]) as TableSpec["key"]) ?? null,
    columns: [],
  }))
  const byTable = new Map(specs.map((spec) => [spec.name, spec]))
  for (const row of records(ColumnsTable)) {
    const spec = byTable.get(String(row["table"]))
    if (spec === undefined)
      throw new TableError(`${ColumnsTable} 提到不存在的工作表 ${String(row["table"])}`)
    spec.columns.push({
      path: String(row["column"]).split("."),
      kind: String(row["kind"]) as Kind,
      blank: String(row["blank"]) as Blank,
    })
  }
  return { specs, files: records(FilesTable), settings: records(SettingsTable), need }
}

const buildObject = (
  columns: readonly ColumnSpec[],
  prefix: readonly string[],
  cellOf: (column: ColumnSpec) => Json | undefined,
): JsonObject | undefined => {
  const object: JsonObject = {}
  let filled = false
  const depth = prefix.length
  const direct = columns.filter(
    (column) =>
      column.path.length === depth + 1 && prefix.every((key, index) => column.path[index] === key),
  )
  for (const column of direct) {
    const key = column.path[depth] as string
    if (column.kind === "object") {
      const nested = buildObject(columns, column.path, cellOf)
      if (nested !== undefined) {
        object[key] = nested
        filled = true
      } else if (column.blank !== "omit") object[key] = {}
      continue
    }
    const value = cellOf(column)
    if (value === undefined) continue
    object[key] = value
    if (value !== null && value !== "" && !(Array.isArray(value) && value.length === 0))
      filled = true
  }
  return filled || depth === 0 ? object : Object.keys(object).length > 0 ? object : undefined
}

const canonical = (value: Json): string =>
  Array.isArray(value)
    ? `[${value.map(canonical).join(",")}]`
    : isObject(value)
      ? `{${Object.keys(value)
          .sort()
          .map((key) => `${JSON.stringify(key)}:${canonical(value[key] as Json)}`)
          .join(",")}}`
      : JSON.stringify(value)

// 改過的紀錄沿用原本的欄位順序，新欄位放在最後
const orderLike = (value: Json, original: Json | undefined): Json => {
  if (Array.isArray(value))
    return value.map((item, index) =>
      orderLike(item, Array.isArray(original) ? original[index] : undefined),
    )
  if (!isObject(value)) return value
  const reference = isObject(original) ? original : {}
  const keys = [
    ...Object.keys(reference).filter((key) => key in value),
    ...Object.keys(value).filter((key) => !(key in reference)),
  ]
  return Object.fromEntries(keys.map((key) => [key, orderLike(value[key] as Json, reference[key])]))
}

const withOriginalText = (
  records: readonly JsonObject[],
  original: readonly ContentRecord[],
): ContentRecord[] => {
  const byId = new Map(
    original
      .filter((record) => typeof record.value["id"] === "string")
      .map((record) => [record.value["id"] as string, record]),
  )
  return records.map((value, index) => {
    const id = value["id"]
    const match = (typeof id === "string" ? byId.get(id) : undefined) ?? original[index]
    if (match !== undefined && canonical(match.value) === canonical(value)) return match
    return { value: orderLike(value, match?.value) as JsonObject, raw: null }
  })
}

export const fromTables = (tables: readonly Table[], current: ContentFiles): ContentFiles => {
  const byName = new Map(tables.map((table) => [table.name, table]))
  const { specs, files, settings, need } = readSchema(byName)
  // 子表先建：$parent → 該列的子清單
  const groups = new Map<string, Map<string, JsonObject[]>>()
  const roots = new Map<string, JsonObject[]>()
  for (const spec of [...specs].reverse()) {
    const table = need(spec.name)
    const leaves = spec.columns.filter((column) => column.kind !== "object")
    const allowed = new Set([
      ...leaves.map((column) => headerOf(column.path)),
      ...(spec.key === KeyColumn ? [KeyColumn] : []),
      ...(spec.parent !== null ? [ParentColumn] : []),
    ])
    for (const header of table.headers)
      if (
        !allowed.has(header) &&
        !(header.endsWith(NameSuffix) && allowed.has(header.slice(0, -NameSuffix.length)))
      )
        throw new TableError(`${spec.name} 有不認得的欄位 ${header}`)
    for (const header of allowed)
      if (!table.headers.includes(header)) throw new TableError(`${spec.name} 少了欄位 ${header}`)
    const indexOf = new Map(table.headers.map((header, index) => [header, index]))
    const own = new Map<string, JsonObject[]>()
    const keys = new Set<string>()
    const built: JsonObject[] = []
    table.rows.forEach((row, rowIndex) => {
      if (row.every(isBlank)) return
      const where = `${spec.name} 第 ${rowIndex + 2} 列`
      const cell = (header: string) => row[indexOf.get(header) ?? -1] ?? null
      const key = spec.key === null ? null : text(cell(spec.key))
      if (spec.key !== null) {
        if (key === null) throw new TableError(`${where} 的 ${spec.key} 是空的`)
        if (keys.has(key)) throw new TableError(`${where} 的 ${spec.key} ${key} 重複`)
        keys.add(key)
      }
      const value = buildObject(spec.columns, [], (column) => {
        const header = headerOf(column.path)
        if (column.kind === "count") {
          const children = groups.get(`${spec.name}.${header}`)
          const items = key === null ? undefined : children?.get(key)
          if (items !== undefined) {
            children?.delete(key as string)
            return items
          }
          return isBlank(cell(header)) ? undefined : []
        }
        return decode(column, cell(header), `${where} 的 ${header}`)
      }) as JsonObject
      if (spec.parent === null) {
        built.push(value)
        return
      }
      const parentKey = text(cell(ParentColumn))
      if (parentKey === null) throw new TableError(`${where} 的 ${ParentColumn} 是空的`)
      own.set(parentKey, [...(own.get(parentKey) ?? []), value])
    })
    for (const child of specs.filter((candidate) => candidate.parent === spec.name)) {
      const left = groups.get(child.name)
      if (left !== undefined && left.size > 0)
        throw new TableError(
          `${child.name} 的 ${ParentColumn} 找不到：${[...left.keys()].join("、")}`,
        )
    }
    if (spec.parent === null) roots.set(spec.name, built)
    else groups.set(spec.name, own)
  }

  const output: ContentFiles = new Map()
  const fileNames = [...new Set(files.map((row) => String(row["file"])))]
  for (const file of fileNames) {
    let originals = new Map<string, ContentProperty>()
    const currentText = current.get(file)
    if (currentText !== undefined) {
      try {
        originals = new Map(
          parseContentFile(currentText).properties.map((property) => [property.name, property]),
        )
      } catch {
        originals = new Map()
      }
    }
    const properties = files
      .filter((row) => row["file"] === file)
      .map((row): ContentProperty => {
        const name = String(row["property"])
        const source = String(row["source"])
        const original = originals.get(name)
        if (source === SettingsTable) {
          const setting = settings.find((entry) => entry["file"] === file && entry["key"] === name)
          if (setting === undefined)
            throw new TableError(`${SettingsTable} 少了 ${file} 的 ${name}`)
          const cell = setting["value"] ?? null
          const value = (
            setting["kind"] === "json" && typeof cell === "string" ? JSON.parse(cell) : cell
          ) as Json
          const same =
            original !== undefined &&
            original.records === null &&
            canonical(original.value) === canonical(value)
          return { name, value, raw: same ? original.raw : null, records: null }
        }
        const records = roots.get(source)
        if (records === undefined)
          throw new TableError(`${FilesTable} 提到不存在的工作表 ${source}`)
        return {
          name,
          value: null,
          raw: null,
          records: withOriginalText(records, original?.records ?? []),
        }
      })
    output.set(file, formatContentFile(properties))
  }
  return output
}

// 匯出前自我檢查：不看原檔重建，值要跟原本一樣（欄位順序可以不同，匯入時會沿用原檔的順序）
export const assertLossless = (files: ContentFiles) => {
  const rebuilt = fromTables(toTables(files), new Map())
  for (const [name, original] of files) {
    if (
      canonical(JSON.parse(rebuilt.get(name) ?? "null") as Json) !==
      canonical(JSON.parse(original) as Json)
    )
      throw new TableError(`${name} 轉成試算表會遺失資料`)
  }
}
