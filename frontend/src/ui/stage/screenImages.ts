import type { TimeOfDay } from "../../game/timeOfDay"
import { contentArtCandidates, contentArtKeys } from "../../render/contentAssets"

// 各畫面用到的設計素材（含 CSS 背景）與內容圖網址，給 preloadImages 預先載入
const urlsOf = (modules: Record<string, string>) => Object.values(modules)

const townArt = import.meta.glob<string>("../../assets/town/*.webp", {
  eager: true,
  import: "default",
})
const innArt = import.meta.glob<string>("../../assets/inn/*.webp", {
  eager: true,
  import: "default",
})
const companionsArt = import.meta.glob<string>("../../assets/companions/*.webp", {
  eager: true,
  import: "default",
})
// 紅印章、對話框：旅館與同伴共用（錢袋在城鎮資料夾，進城時已載）
const sharedArt = import.meta.glob<string>(
  ["../../assets/title/seal.webp", "../../assets/title/bubble.webp"],
  { eager: true, import: "default" },
)

const firstArt = (category: Parameters<typeof contentArtKeys>[0], keys: readonly string[]) =>
  keys.flatMap((key) => contentArtCandidates(category, key).slice(0, 1))

// 城鎮：只載目前時段的全景，其他三張用不到
export const townImages = (time: TimeOfDay): string[] =>
  Object.entries(townArt)
    .filter(([path]) => !path.includes("/panorama-") || path.endsWith(`/panorama-${time}.webp`))
    .map(([, url]) => url)

export const innImages = (): string[] => [
  ...urlsOf(innArt),
  ...urlsOf(sharedArt),
  ...firstArt("room-backgrounds", contentArtKeys("room-backgrounds")),
  ...firstArt("room-badges", contentArtKeys("room-badges")),
]

// 勳章所兌換的兩枚勳章畫在傭兵所的名冊便條上
export const companionsImages = (): string[] => [
  ...urlsOf(companionsArt),
  ...urlsOf(innArt).filter((url) => /(parchment|pin|close|counter-|door-plate)/.test(url)),
  ...urlsOf(sharedArt),
  ...firstArt("items", ["item-1304", "item-1305"]),
]
