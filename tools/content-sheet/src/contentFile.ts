export type Json = string | number | boolean | null | Json[] | { [key: string]: Json }

export type JsonObject = { [key: string]: Json }

// raw 是檔案裡的原文；null 表示這筆是新的或改過的，寫出時重新排版
export type ContentRecord = { value: JsonObject; raw: string | null }

// 頂層屬性：有資料的物件陣列一筆一行（records），其他值寫成單行（value）
export type ContentProperty = {
  name: string
  value: Json
  raw: string | null
  records: ContentRecord[] | null
}

export type ContentFile = { properties: ContentProperty[] }

export class ContentFileError extends Error {}

const isObject = (value: Json): value is JsonObject =>
  typeof value === "object" && value !== null && !Array.isArray(value)

// 內容檔的固定格式（後端 ContentFileFormatter）：兩格縮排、頂層陣列一筆一行，其餘寫成單行
const recordLine = /^ {4}(.*?),?$/
const propertyLine = /^ {2}("(?:[^"\\]|\\.)*"): (.*?),?$/

export const parseContentFile = (text: string): ContentFile => {
  const root = JSON.parse(text) as Json
  if (!isObject(root)) throw new ContentFileError("內容檔的最外層必須是物件")
  const lines = text.split("\n")
  const properties: ContentProperty[] = []
  let index = 1
  for (const [name, value] of Object.entries(root)) {
    const match = propertyLine.exec(lines[index] ?? "")
    if (match === null || JSON.parse(match[1] ?? "") !== name)
      throw new ContentFileError(`第 ${index + 1} 行不是屬性 ${name}`)
    const rest = match[2] ?? ""
    index++
    if (rest !== "[" || !Array.isArray(value)) {
      properties.push({ name, value, raw: rest, records: null })
      continue
    }
    const records: ContentRecord[] = value.map((record, position) => {
      const raw = recordLine.exec(lines[index] ?? "")?.[1]
      if (
        raw === undefined ||
        !isObject(record) ||
        JSON.stringify(JSON.parse(raw)) !== JSON.stringify(record)
      )
        throw new ContentFileError(`${name} 第 ${position + 1} 筆不是一行一筆`)
      index++
      return { value: record, raw }
    })
    if (lines[index]?.startsWith("  ]") !== true)
      throw new ContentFileError(`${name} 的陣列沒有在第 ${index + 1} 行結束`)
    index++
    properties.push({ name, value: null, raw: null, records })
  }
  return { properties }
}

export const formatInline = (value: Json): string => {
  if (Array.isArray(value)) return `[${value.map(formatInline).join(", ")}]`
  if (isObject(value))
    return `{${Object.entries(value)
      .map(([key, item]) => `${JSON.stringify(key)}: ${formatInline(item)}`)
      .join(", ")}}`
  return JSON.stringify(value)
}

export const formatContentFile = (properties: readonly ContentProperty[]): string => {
  const lines = properties.map(({ name, value, raw, records }, position) => {
    const separator = position < properties.length - 1 ? "," : ""
    const head = `  ${JSON.stringify(name)}: `
    if (records === null || records.length === 0)
      return `${head}${raw ?? formatInline(records === null ? value : [])}${separator}`
    const body = records.map(
      (record, at) =>
        `    ${record.raw ?? formatInline(record.value)}${at < records.length - 1 ? "," : ""}`,
    )
    return [`${head}[`, ...body, `  ]${separator}`].join("\n")
  })
  return `{\n${lines.join("\n")}\n}\n`
}
