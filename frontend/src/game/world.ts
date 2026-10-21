import type { CharacterView, Recommendation } from "./types"

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
  restricted: "此地區需要鑰匙",
  subPlaceNotAllowed: "此地區沒有子地區",
  subPlaceRequired: "請選擇子地區",
  subPlaceOutOfRange: "子地區不存在",
  samePlace: "你已經在這裡",
}

export const moveReasonText = (code: string) => moveReasonTexts[code] ?? code

export const locationTitle = ({
  placeName,
  subPlaceName,
}: Pick<CharacterView, "placeName" | "subPlaceName">) =>
  subPlaceName === null ? placeName : `${placeName}・${subPlaceName}`
