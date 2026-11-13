import { buffLabel, remainingMinutes, remainingText, type BuffKind } from "./buffs"
import { formatNumber } from "./format"
import type { CharacterVo } from "./character"

// 旅館的資料（來自 /api/inn），由 api/lodgingData.ts 轉換
// seconds 是住宿時長，price 已依角色等級算好
export type InnRoom = {
  readonly id: string
  readonly name: string
  readonly kind: BuffKind
  readonly multiplier: number
  readonly seconds: number
  readonly price: number
}

export type Lodging = { readonly roomId: string; readonly startsAt: Date; readonly endsAt: Date }

// restFee 是目前等級自動住宿一次的最低費用（0＝免費）
type InnVoParams = { rooms: readonly InnRoom[]; lodging: Lodging | null; restFee: number }

export class InnVo {
  readonly rooms: readonly InnRoom[]
  readonly lodging: Lodging | null
  readonly restFee: number

  private constructor(params: InnVoParams) {
    this.rooms = params.rooms
    this.lodging = params.lodging
    this.restFee = params.restFee
  }

  static create(params: InnVoParams): InnVo {
    return new InnVo(params)
  }
}

// 入住會扣錢，回應帶著更新後的角色
type InnBookVoParams = { character: CharacterVo; lodging: Lodging; cost: number }

export class InnBookVo {
  readonly character: CharacterVo
  readonly lodging: Lodging
  readonly cost: number

  private constructor(params: InnBookVoParams) {
    this.character = params.character
    this.lodging = params.lodging
    this.cost = params.cost
  }

  static create(params: InnBookVoParams): InnBookVo {
    return new InnBookVo(params)
  }
}

export const roomEffect = (room: InnRoom) =>
  `${buffLabel(room.kind)} ×${room.multiplier.toFixed(2)}`

// 內容規定 seconds 為 60～86400：整點「H 小時」、不到一小時「M 分」、其餘「H 小時 M 分」（分鐘四捨五入）
export function stayLength(seconds: number): string {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.round((seconds % 3600) / 60)
  if (minutes === 0) return `${hours} 小時`
  return hours === 0 ? `${minutes} 分` : `${hours} 小時 ${minutes} 分`
}

// 剩餘時間「H:MM」（分鐘無條件進位）
export function remainingClock(endsAt: Date, now: Date): string {
  const minutes = remainingMinutes(endsAt, now)
  return `${Math.floor(minutes / 60)}:${String(minutes % 60).padStart(2, "0")}`
}

// 住宿剩餘時間的「現在」：本機時鐘比伺服器慢、早於住宿開始時，以開始時刻計，剩餘不會超過整段時長
const clampToStart = (lodging: Lodging, now: Date) =>
  now.getTime() < lodging.startsAt.getTime() ? lodging.startsAt : now

// 與城鎮加成列同一個格式（HH:MM／MM:SS）
export const stayRemainingClock = (lodging: Lodging, now: Date) =>
  remainingText(lodging.endsAt, clampToStart(lodging, now))

// 到期或房型已被移除都視為沒有住宿加成
export const activeRoom = (lodging: Lodging | null, rooms: readonly InnRoom[], now: Date) =>
  lodging !== null && lodging.endsAt.getTime() > now.getTime()
    ? rooms.find((room) => room.id === lodging.roomId)
    : undefined

export function lodgingLine(
  lodging: Lodging | null,
  rooms: readonly InnRoom[],
  now: Date,
): string | null {
  const room = activeRoom(lodging, rooms, now)
  if (room === undefined || lodging === null) return null
  // 角色頁舊外殼的密藥效果卡：維持 H:MM（分鐘進位）
  return `${room.name}・${roomEffect(room)}（剩 ${remainingClock(lodging.endsAt, clampToStart(lodging, now))}）`
}

// 詳情上的取代說明：換別間是警示，同一間是「正在住這間」；沒有生效中的住宿時沒有說明
export function replaceNotice(
  lodging: Lodging | null,
  rooms: readonly InnRoom[],
  roomId: string,
  now: Date,
): { text: string; replacing: boolean } | null {
  const current = activeRoom(lodging, rooms, now)
  if (current === undefined || lodging === null) return null
  const left = stayRemainingClock(lodging, now)
  return current.id === roomId
    ? { text: `正在住這間，剩 ${left}`, replacing: false }
    : { text: `會取代目前的${current.name}（${roomEffect(current)}，剩 ${left}）`, replacing: true }
}

export type InnkeeperEvent =
  | { kind: "booked"; roomName: string }
  | { kind: "notEnoughMoney" }
  | { kind: "bookFailed" }
  | { kind: "thresholdFailed" }
  | { kind: "loadFailed" }

// 老闆娘瑪莎的台詞：最近的事件優先，沒有事件時依有沒有入住打招呼
export function innkeeperLine(event: InnkeeperEvent | null, hasStay: boolean): string {
  switch (event?.kind) {
    case "booked":
      return `${event.roomName}收拾好了，好好睡一覺吧！`
    case "notEnoughMoney":
      return "哎呀，這間的錢好像還差一點呢。"
    case "bookFailed":
      return "咦，登記簿沒寫上去……等會兒再來吧。"
    case "thresholdFailed":
      return "剛剛那個沒記下來，再調一次好嗎？"
    case "loadFailed":
      return "哎呀，今天的房帳一時找不到……"
    case undefined:
      return hasStay ? "房間還幫你留著呢。要換一間，還是續住？" : "歡迎光臨暖爐亭！今晚想住哪一間？"
  }
}

const ThresholdStep = 5
const ThresholdSkip = -1
const ThresholdMaximum = 100

// 自動住宿門檻 −／＋：對齊到相鄰的 5 的倍數；0 再減是略過（−1），略過再加是 0；不能按時回 null
export function stepThreshold(value: number, direction: 1 | -1): number | null {
  if (direction === 1) {
    if (value >= ThresholdMaximum) return null
    if (value < 0) return 0
    return Math.min(
      ThresholdMaximum,
      Math.floor(value / ThresholdStep) * ThresholdStep + ThresholdStep,
    )
  }
  if (value < 0) return null
  if (value === 0) return ThresholdSkip
  return Math.max(0, Math.ceil(value / ThresholdStep) * ThresholdStep - ThresholdStep)
}

export const restFeeText = (fee: number) => (fee === 0 ? "免費" : `${formatNumber(fee)} 起`)

const PanoramaHeight = 844

// 旅館從城鎮市集的旅館名牌展開：圓心是名牌中心（舞台座標），隨全景倍率 k＝高／844
export const innRevealOrigin = (stageHeight: number) => {
  const scale = stageHeight / PanoramaHeight
  return { x: 195 - 81 * scale, y: 300 * scale + 19 }
}

// 木台上緣：844 高時 250，每矮 1 上移 0.4
export const innCounterTop = (stageHeight: number) =>
  Math.round(250 - (PanoramaHeight - stageHeight) * 0.4)

// 住宿剩餘比例：（結束−現在）／（結束−開始），夾在 0～1
export const stayProgress = (lodging: Lodging, now: Date) => {
  const total = lodging.endsAt.getTime() - lodging.startsAt.getTime()
  if (total <= 0) return 0
  return Math.min(1, Math.max(0, (lodging.endsAt.getTime() - now.getTime()) / total))
}
