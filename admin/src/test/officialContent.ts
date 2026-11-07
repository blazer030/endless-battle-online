/// <reference types="node" />
import { readFileSync } from "node:fs"
import { resolve } from "node:path"

// 讀正式內容檔，給「全部內容都能來回轉換」的測試用；測試從 admin 資料夾執行
export const readOfficialContent = (fileName: string) =>
  JSON.parse(
    readFileSync(resolve(process.cwd(), "../backend/content", fileName), "utf8"),
  ) as unknown
