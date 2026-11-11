import type { Recommendation, WorldPlace } from "./types"
import type { CharacterVo } from "./character"

export type RecommendationTone = "recommended" | "too-hard" | "too-easy"
export type RecommendationLabel = { text: string; tone: RecommendationTone }

const labels: Record<Recommendation, RecommendationLabel | null> = {
  Recommended: { text: "推薦", tone: "recommended" },
  TooHard: { text: "偏難", tone: "too-hard" },
  TooEasy: { text: "偏易", tone: "too-easy" },
  None: null,
}

export const recommendationLabel = (recommendation: Recommendation) => labels[recommendation]

const moveReasonTexts: Record<string, string> = {
  unknownPlace: "地區不存在",
  restricted: "需用鑰匙開啟地區",
  subPlaceNotAllowed: "此地區沒有子地區",
  subPlaceRequired: "請選擇子地區",
  subPlaceOutOfRange: "子地區不存在",
  samePlace: "你已經在這裡",
}

export const moveReasonText = (code: string) => moveReasonTexts[code] ?? code

export const locationTitle = ({
  placeName,
  subPlaceName,
}: Pick<CharacterVo, "placeName" | "subPlaceName">) =>
  subPlaceName === null ? placeName : `${placeName}・${subPlaceName}`

const MillisecondsPerMinute = 60_000

export const passActive = (passExpiresAt: Date | null, now: Date) =>
  passExpiresAt !== null && passExpiresAt.getTime() > now.getTime()

const twoDigits = (value: number) => String(value).padStart(2, "0")

// 禁區：通行證未到期免費進入，否則持有鑰匙才能進入（進入時扣一把）
export const placeAccess = (
  place: Pick<WorldPlace, "restricted" | "keys" | "passExpiresAt">,
  now: Date,
): { open: boolean; label: string | null } => {
  if (!place.restricted) return { open: true, label: null }
  const until = place.passExpiresAt === null ? null : new Date(place.passExpiresAt)
  if (until !== null && passActive(until, now)) {
    return {
      open: true,
      label: `通行至 ${twoDigits(until.getHours())}:${twoDigits(until.getMinutes())}`,
    }
  }
  return place.keys > 0
    ? { open: true, label: `需要鑰匙（持有 ${place.keys}）` }
    : { open: false, label: "需要鑰匙" }
}

// 冒險頁地點標題下的禁區通行狀態；剩餘分鐘無條件進位
export const passStatus = (areaPassExpiresAt: Date | null, now: Date): string | null => {
  if (areaPassExpiresAt === null) return null
  if (!passActive(areaPassExpiresAt, now)) return "禁區通行時間已結束，請到地圖用鑰匙再次進入"
  const remaining = areaPassExpiresAt.getTime() - now.getTime()
  return `禁區通行剩 ${Math.ceil(remaining / MillisecondsPerMinute)} 分`
}
