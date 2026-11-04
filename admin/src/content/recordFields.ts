// 內容紀錄是無型別的 JSON 物件；表單依每個欄位現有值的型別決定輸入方式
export type JsonValue =
  string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue }

export type JsonRecord = { [key: string]: JsonValue }

export type FieldKind = "text" | "number" | "boolean" | "json"

export type RecordField = { name: string; kind: FieldKind; value: string | number | boolean }

export type FieldValues = Record<string, unknown>

export type FromFieldsResult = { record: JsonRecord } | { errors: Record<string, string> }

function kindOf(value: JsonValue): FieldKind {
  if (typeof value === "string") return "text"
  if (typeof value === "number") return "number"
  if (typeof value === "boolean") return "boolean"
  // null、物件、陣列都以 JSON 文字編輯，才能保留原本的結構
  return "json"
}

export function toFields(record: JsonRecord): RecordField[] {
  return Object.entries(record).map(([name, value]) =>
    typeof value === "string" || typeof value === "number" || typeof value === "boolean"
      ? { name, kind: kindOf(value), value }
      : { name, kind: "json", value: JSON.stringify(value) },
  )
}

// 以原本的整筆紀錄為底：表單沒送的欄位原樣保留，順序不變
export function fromFields(base: JsonRecord, values: FieldValues): FromFieldsResult {
  const record: JsonRecord = {}
  const errors: Record<string, string> = {}
  for (const [name, original] of Object.entries(base)) {
    if (!(name in values)) {
      record[name] = original
      continue
    }

    const value = values[name]
    switch (kindOf(original)) {
      case "text":
        record[name] = typeof value === "string" ? value : String(value ?? "")
        break
      case "number":
        if (typeof value === "number" && Number.isFinite(value)) {
          record[name] = value
        } else {
          errors[name] = "必須是數字"
        }
        break
      case "boolean":
        record[name] = value === true
        break
      case "json":
        try {
          record[name] = JSON.parse(String(value)) as JsonValue
        } catch {
          errors[name] = "不是合法的 JSON"
        }
        break
    }
  }

  return Object.keys(errors).length > 0 ? { errors } : { record }
}
