import { describe, expect, it } from "vitest"
import { parseCredentials } from "./credentials.ts"
import { AssetError } from "./process.ts"

describe("parseCredentials", () => {
  it("缺 R2_SECRET_ACCESS_KEY 時錯誤訊息含欄位名且不含其他欄位的值", () => {
    const text = "R2_ENDPOINT=https://abc.r2.cloudflarestorage.com\nR2_ACCESS_KEY_ID=key-id-123\n"

    const error = (() => {
      try {
        parseCredentials(text)
      } catch (caught) {
        return caught
      }
    })()

    expect(error).toBeInstanceOf(AssetError)
    const message = (error as Error).message
    expect(message).toContain("R2_SECRET_ACCESS_KEY")
    expect(message).not.toContain("key-id-123")
    expect(message).not.toContain("abc.r2.cloudflarestorage.com")
  })

  it("沒有 .env.r2 時錯誤訊息指出檔案與三個欄位", () => {
    expect(() => parseCredentials(null)).toThrow(/\.env\.r2/)
  })

  it("留白的欄位也算缺少", () => {
    expect(() =>
      parseCredentials("R2_ENDPOINT=x\nR2_ACCESS_KEY_ID=\nR2_SECRET_ACCESS_KEY=y\n"),
    ).toThrow(/R2_ACCESS_KEY_ID/)
  })

  it("忽略註解並去掉前後空白與引號", () => {
    const text =
      '# 註解\nR2_ENDPOINT = "https://e"\nR2_ACCESS_KEY_ID=id\nR2_SECRET_ACCESS_KEY=secret\n'

    expect(parseCredentials(text)).toEqual({
      endpoint: "https://e",
      accessKeyId: "id",
      secretAccessKey: "secret",
    })
  })
})
