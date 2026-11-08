import { readFile } from "node:fs/promises"
import { AssetError } from "./process.ts"

export type Credentials = { endpoint: string; accessKeyId: string; secretAccessKey: string }

const Fields = {
  endpoint: "R2_ENDPOINT",
  accessKeyId: "R2_ACCESS_KEY_ID",
  secretAccessKey: "R2_SECRET_ACCESS_KEY",
} as const

const parseLines = (text: string): Map<string, string> => {
  const values = new Map<string, string>()
  for (const line of text.split(/\r?\n/)) {
    const trimmed = line.trim()
    if (trimmed === "" || trimmed.startsWith("#")) continue
    const equals = trimmed.indexOf("=")
    if (equals < 0) continue
    const value = trimmed.slice(equals + 1).trim()
    values.set(trimmed.slice(0, equals).trim(), value.replace(/^(["'])(.*)\1$/, "$2"))
  }
  return values
}

// 錯誤訊息只寫欄位名，絕不帶出任何值
export const parseCredentials = (text: string | null): Credentials => {
  const names = Object.values(Fields)
  if (text === null)
    throw new AssetError(`找不到 .env.r2：請依 .env.r2.example 建立並填入 ${names.join("、")}`)
  const values = parseLines(text)
  const missing = names.filter((name) => (values.get(name) ?? "") === "")
  if (missing.length > 0) throw new AssetError(`.env.r2 缺少 ${missing.join("、")}`)
  return {
    endpoint: values.get(Fields.endpoint) ?? "",
    accessKeyId: values.get(Fields.accessKeyId) ?? "",
    secretAccessKey: values.get(Fields.secretAccessKey) ?? "",
  }
}

export const loadCredentials = async (path: string): Promise<Credentials> =>
  parseCredentials(await readFile(path, "utf8").catch(() => null))
