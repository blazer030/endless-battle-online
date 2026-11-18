// 後端錯誤只回原因代碼，玩家看到的字由前端決定；這張表是各畫面共用的說法（照搬原本後端的訊息）
// 各畫面自己的代碼表沒有的代碼才查這裡；認不得的代碼用通用句
export const GenericErrorText = "發生錯誤，請稍後再試"

const texts: Record<string, string> = {
  USERNAME_INVALID: "帳號必須是 3～32 個英數字或底線",
  PASSWORD_TOO_SHORT: "密碼至少要 8 個字元",
  USERNAME_TAKEN: "帳號已被使用",
  AUTHENTICATION_FAILED: "帳號或密碼錯誤",
  ACCOUNT_NOT_FOUND: "找不到帳號",
  CHARACTER_NAME_LENGTH: "角色名稱必須是 2～12 個字",
  CHARACTER_NAME_TOO_COMPLEX: "角色名稱含有過多組合字元，請改用較簡單的字",
  JOB_UNKNOWN: "找不到職業",
  JOB_NOT_FIRST_TIER: "只能選擇初階職業",
  STAT_BELOW_ONE: "每項能力至少要分配 1 點",
  POINTS_NOT_SPENT: "能力點數必須剛好分完 10 點",
  SEX_INVALID: "性別選項無效",
  ELEMENT_INVALID: "屬性選項無效",
  BIRTH_INVALID: "出生地選項無效",
  CHARACTER_NOT_FOUND: "尚未建立角色",
  SKILL_UNKNOWN: "不存在的技能",
  RECIPE_UNKNOWN: "找不到配方",
  ITEM_NOT_FOUND: "找不到這件道具",
  CONCURRENT_UPDATE: "資料正被其他請求使用，請重試",
}

export const errorText = (code: string) => texts[code] ?? GenericErrorText

export const errorTextOf = (reasons: readonly string[]) =>
  reasons.length === 0 ? GenericErrorText : errorText(reasons[0]!)
