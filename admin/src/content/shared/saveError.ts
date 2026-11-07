import { AdminApiError } from "../../api/dataProvider"
import { parseObject, parseString } from "./parse"

// API 拒絕時的訊息：有指出欄位就帶上欄位路徑
export function saveErrorMessage(error: Error): string {
  if (error instanceof AdminApiError) {
    try {
      const body = parseObject(error.body, "body")
      const message = parseString(body.message, "message")
      const field = parseString(body.field, "field")
      return field === "" ? message : `${field}：${message}`
    } catch {
      return error.message
    }
  }
  return error.message
}
