// API 回應進到遊戲型別前的檢查：缺必要欄位或型別不對時丟出含路徑的錯誤；不認得的欄位直接忽略
export class ParseError extends Error {
  readonly path: string

  constructor(path: string, expected: string) {
    super(`API 回應的 ${path} 應該是${expected}`)
    this.name = "ParseError"
    this.path = path
  }
}

export const parseString = (value: unknown, path: string): string => {
  if (typeof value !== "string") throw new ParseError(path, "文字")
  return value
}

export const parseNumber = (value: unknown, path: string): number => {
  if (typeof value !== "number" || !Number.isFinite(value)) throw new ParseError(path, "數字")
  return value
}

export const parseNullableString = (value: unknown, path: string): string | null =>
  value === null ? null : parseString(value, path)

export const parseObject = (value: unknown, path: string): Record<string, unknown> => {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    throw new ParseError(path, "物件")
  return value as Record<string, unknown>
}

export const parseArray = <T>(
  value: unknown,
  path: string,
  parseItem: (item: unknown, itemPath: string) => T,
): T[] => {
  if (!Array.isArray(value)) throw new ParseError(path, "陣列")
  return value.map((item, index) => parseItem(item, `${path}[${index}]`))
}

export const parseBoolean = (value: unknown, path: string): boolean => {
  if (typeof value !== "boolean") throw new ParseError(path, "布林值")
  return value
}

export const parseNullableNumber = (value: unknown, path: string): number | null =>
  value === null ? null : parseNumber(value, path)

export const parseOneOf = <const T extends string>(
  value: unknown,
  allowed: readonly T[],
  path: string,
): T => {
  const found = allowed.find((candidate) => candidate === value)
  if (found === undefined) throw new ParseError(path, `${allowed.join("、")} 之一`)
  return found
}

// ISO 8601 時間字串轉成 Date；格式錯誤或不是文字都丟出 ParseError
export const parseTimestamp = (value: unknown, path: string): Date => {
  const text = parseString(value, path)
  const date = new Date(text)
  if (Number.isNaN(date.getTime())) throw new ParseError(path, "時間")
  return date
}
