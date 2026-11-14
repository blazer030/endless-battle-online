import { HonourMedalKeys } from "../../game/companions"
import { MedalKeys } from "../../game/honour"
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
const honourArt = import.meta.glob<string>("../../assets/honour/*.webp", {
  eager: true,
  import: "default",
})
// 勳章所沿用標題的榮譽廳、莉瑟、僱用中紙條，以及同伴的分區銘牌
const honourSharedArt = import.meta.glob<string>(
  [
    "../../assets/title/hall.webp",
    "../../assets/title/clerk.webp",
    "../../assets/title/book-page.webp",
    "../../assets/companions/name-plate.webp",
  ],
  { eager: true, import: "default" },
)
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
  ...firstArt("items", HonourMedalKeys),
]

// 勳章所：櫃檯、陳列板、羊皮紙與五枚勳章（道具佔位圖是很小的 SVG，不預載）
export const honourImages = (): string[] => [
  ...urlsOf(honourArt),
  ...urlsOf(honourSharedArt),
  ...urlsOf(innArt).filter((url) => /(parchment|pin|close|counter-)/.test(url)),
  ...urlsOf(sharedArt),
  ...firstArt("items", MedalKeys),
]
