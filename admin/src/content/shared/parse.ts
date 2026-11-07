// API 回應進到畫面前的檢查：缺必填欄位或型別不對時丟出含路徑的錯誤，畫面不顯示部分資料
export class ContentParseError extends Error {
  readonly path: string

  constructor(path: string, expected: string) {
    super(`API 回應的 ${path} 應該是${expected}`)
    this.name = "ContentParseError"
    this.path = path
  }
}

export function parseString(value: unknown, path: string): string {
  if (typeof value !== "string") {
    throw new ContentParseError(path, "文字")
  }
  return value
}

export function parseNumber(value: unknown, path: string): number {
  if (typeof value !== "number") {
    throw new ContentParseError(path, "數字")
  }
  return value
}

export function parseBoolean(value: unknown, path: string): boolean {
  if (typeof value !== "boolean") {
    throw new ContentParseError(path, "布林值")
  }
  return value
}

export function parseNullableString(value: unknown, path: string): string | null {
  return value === null ? null : parseString(value, path)
}

export function parseArray<T>(
  value: unknown,
  path: string,
  parseItem: (item: unknown, itemPath: string) => T,
): T[] {
  if (!Array.isArray(value)) {
    throw new ContentParseError(path, "陣列")
  }
  return value.map((item, index) => parseItem(item, `${path}[${index}]`))
}

export function parseObject(value: unknown, path: string): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new ContentParseError(path, "物件")
  }
  return value as Record<string, unknown>
}

export function parseOneOf<T extends string>(
  value: unknown,
  path: string,
  choices: readonly T[],
): T {
  if (!choices.includes(value as T)) {
    throw new ContentParseError(path, `${choices.join("、")}其中之一`)
  }
  return value as T
}
