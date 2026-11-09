export type TimeOfDay = "dawn" | "day" | "dusk" | "night"

// 依裝置的當地小時換標題背景：5～7 清晨、8～16 白天、17～18 黃昏，其餘夜晚
export const timeOfDay = (hour: number): TimeOfDay => {
  if (hour >= 5 && hour < 8) return "dawn"
  if (hour >= 8 && hour < 17) return "day"
  if (hour >= 17 && hour < 19) return "dusk"
  return "night"
}
