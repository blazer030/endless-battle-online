import { buffKinds, buffLabel, type BuffKind, type BuffVo } from "./buffs"
import { roomEffect, type InnRoom, type Lodging } from "./inn"

// 城鎮全景三段：0 城門口、1 市集、2 公會前
export type TownSegment = 0 | 1 | 2
export const townSegments: readonly TownSegment[] = [0, 1, 2]

// 城鎮上可以點的設施；點了接到舊外殼的對應分頁
export type TownFacility =
  | "character"
  | "bag"
  | "companions"
  | "adventure"
  | "inn"
  | "shop"
  | "forge"
  | "honour"
  | "missions"

// 全景原圖排成 1512×844，以舞台高等比縮放
const PanoramaHeight = 844
const PanoramaWidth = 1512
const StageWidth = 390

export const panoramaScale = (stageHeight: number) => stageHeight / PanoramaHeight

// 每段要往左平移多少：市集原範圍中線（875）對舞台中線；公會前中線（1295）對中，但不超過全景右緣
export const panoramaOffsets = (stageHeight: number): readonly [number, number, number] => {
  const scale = panoramaScale(stageHeight)
  const half = StageWidth / 2
  return [
    0,
    Math.round(875 * scale - half),
    Math.round(Math.min(1295 * scale - half, PanoramaWidth * scale - StageWidth)),
  ]
}

// 水平距離（舞台 px）至少 40 且大於垂直距離才算滑動；往左滑到下一段，兩端不動
const SwipeThreshold = 40

export const isSwipe = (deltaX: number, deltaY: number) =>
  Math.abs(deltaX) >= SwipeThreshold && Math.abs(deltaX) > Math.abs(deltaY)

export const swipeTarget = (segment: TownSegment, deltaX: number, deltaY: number): TownSegment => {
  if (!isSwipe(deltaX, deltaY)) return segment
  const next = segment + (deltaX < 0 ? 1 : -1)
  return next < 0 || next > 2 ? segment : (next as TownSegment)
}

const secondsLeft = (endsAt: Date, now: Date) => (endsAt.getTime() - now.getTime()) / 1000

// 滿 1 小時寫 H:MM（分鐘捨去），不到 1 小時寫「N 分」（無條件進位）
export const remainingText = (endsAt: Date, now: Date) => {
  const seconds = secondsLeft(endsAt, now)
  if (seconds >= 3600) {
    const minutes = Math.floor(seconds / 60)
    return `${Math.floor(minutes / 60)}:${String(minutes % 60).padStart(2, "0")}`
  }
  return `${Math.ceil(seconds / 60)} 分`
}

// 不到 10 分鐘就提醒
export const isEndingSoon = (endsAt: Date, now: Date) => secondsLeft(endsAt, now) < 600

export type BonusRow = {
  readonly key: string
  readonly kind: BuffKind
  readonly isLodging: boolean
  readonly name: string
  // 第二行；舊資料不知道喝了哪瓶時名稱已是「種類 ×倍率」，沒有第二行
  readonly effect: string | null
  readonly itemId: string | null
  readonly roomId: string | null
  readonly remaining: string
  readonly endingSoon: boolean
  // 剩餘比例 0～1
  readonly progress: number
}

const isActive = (endsAt: Date, now: Date) => endsAt.getTime() > now.getTime()

const progressOf = (startsAt: Date, endsAt: Date, now: Date) => {
  const total = endsAt.getTime() - startsAt.getTime()
  if (total <= 0) return 0
  return Math.min(1, Math.max(0, (endsAt.getTime() - now.getTime()) / total))
}

const timing = (startsAt: Date, endsAt: Date, now: Date) => ({
  remaining: remainingText(endsAt, now),
  endingSoon: isEndingSoon(endsAt, now),
  progress: progressOf(startsAt, endsAt, now),
})

// 住宿排第一，密藥依種類順序；已到期或房型已不存在的不列
export const bonusRows = (
  buffs: readonly BuffVo[],
  lodging: Lodging | null,
  rooms: readonly InnRoom[],
  now: Date,
): BonusRow[] => {
  const room =
    lodging !== null && isActive(lodging.endsAt, now)
      ? rooms.find((candidate) => candidate.id === lodging.roomId)
      : undefined
  const lodgingRows: BonusRow[] =
    room === undefined || lodging === null
      ? []
      : [
          {
            key: `lodging-${room.id}`,
            kind: room.kind,
            isLodging: true,
            name: room.name,
            effect: roomEffect(room),
            itemId: null,
            roomId: room.id,
            ...timing(lodging.startsAt, lodging.endsAt, now),
          },
        ]
  const elixirRows = buffs
    .filter((buff) => isActive(buff.endsAt, now))
    .toSorted((a, b) => buffKinds.indexOf(a.kind) - buffKinds.indexOf(b.kind))
    .map((buff): BonusRow => {
      const effect = `${buffLabel(buff.kind)} ×${buff.multiplier}`
      return {
        key: `elixir-${buff.kind}`,
        kind: buff.kind,
        isLodging: false,
        name: buff.itemName ?? effect,
        effect: buff.itemName === null ? null : effect,
        itemId: buff.itemId,
        roomId: null,
        ...timing(buff.startsAt, buff.endsAt, now),
      }
    })
  return [...lodgingRows, ...elixirRows]
}

// 讀進來時還有效、現在已到期 → 要重新讀取；讀進來時就已到期的不算，免得每次倒數都重讀
export const expiredSince = (
  buffs: readonly BuffVo[],
  lodging: Lodging | null,
  since: Date,
  now: Date,
) =>
  [...buffs.map((buff) => buff.endsAt), ...(lodging === null ? [] : [lodging.endsAt])].some(
    (endsAt) => isActive(endsAt, since) && !isActive(endsAt, now),
  )
