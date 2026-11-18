import { describe, expect, it } from "vitest"
import { errorText, errorTextOf, GenericErrorText } from "./errorText"

// design「新代碼」表：前端文字照搬原本後端的訊息
describe("errorText", () => {
  it.each([
    ["USERNAME_INVALID", "帳號必須是 3～32 個英數字或底線"],
    ["PASSWORD_TOO_SHORT", "密碼至少要 8 個字元"],
    ["USERNAME_TAKEN", "帳號已被使用"],
    ["AUTHENTICATION_FAILED", "帳號或密碼錯誤"],
    ["ACCOUNT_NOT_FOUND", "找不到帳號"],
    ["CHARACTER_NAME_LENGTH", "角色名稱必須是 2～12 個字"],
    ["CHARACTER_NAME_TOO_COMPLEX", "角色名稱含有過多組合字元，請改用較簡單的字"],
    ["JOB_UNKNOWN", "找不到職業"],
    ["JOB_NOT_FIRST_TIER", "只能選擇初階職業"],
    ["STAT_BELOW_ONE", "每項能力至少要分配 1 點"],
    ["POINTS_NOT_SPENT", "能力點數必須剛好分完 10 點"],
    ["SEX_INVALID", "性別選項無效"],
    ["ELEMENT_INVALID", "屬性選項無效"],
    ["BIRTH_INVALID", "出生地選項無效"],
    ["CHARACTER_NOT_FOUND", "尚未建立角色"],
    ["SKILL_UNKNOWN", "不存在的技能"],
    ["RECIPE_UNKNOWN", "找不到配方"],
    ["ITEM_NOT_FOUND", "找不到這件道具"],
    ["CONCURRENT_UPDATE", "資料正被其他請求使用，請重試"],
    ["REQUEST_INVALID", "發生錯誤，請稍後再試"],
  ])("%s → %s", (code, text) => {
    expect(errorText(code)).toBe(text)
  })

  it("認不得的代碼用通用句", () => {
    expect(errorText("SOMETHING_NEW")).toBe("發生錯誤，請稍後再試")
    expect(GenericErrorText).toBe("發生錯誤，請稍後再試")
  })
})

describe("errorTextOf", () => {
  it("取第一個代碼", () => {
    expect(errorTextOf(["USERNAME_TAKEN", "SOMETHING_NEW"])).toBe("帳號已被使用")
  })

  it("沒有代碼用通用句", () => {
    expect(errorTextOf([])).toBe("發生錯誤，請稍後再試")
  })
})
