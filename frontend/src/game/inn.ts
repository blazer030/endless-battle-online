import { buffLabel, remainingMinutes, type BuffKind } from "./buffs"
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

type InnVoParams = { rooms: readonly InnRoom[]; lodging: Lodging | null }

export class InnVo {
  readonly rooms: readonly InnRoom[]
  readonly lodging: Lodging | null

  private constructor(params: InnVoParams) {
    this.rooms = params.rooms
    this.lodging = params.lodging
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

// 內容規定 seconds 為 60～86400：不到 60 分顯示分鐘（四捨五入），否則小時到小數一位，整數小時不帶小數
export function stayLength(seconds: number): string {
  const minutes = Math.round(seconds / 60)
  return minutes < 60 ? `${minutes} 分鐘` : `${Math.round((seconds / 3600) * 10) / 10} 小時`
}

// 到期或房型已被移除都視為沒有住宿加成
const activeRoom = (lodging: Lodging | null, rooms: readonly InnRoom[], now: Date) =>
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
  const minutes = remainingMinutes(lodging.endsAt, now)
  const clock = `${Math.floor(minutes / 60)}:${String(minutes % 60).padStart(2, "0")}`
  return `${room.name}・${roomEffect(room)}（剩 ${clock}）`
}

export function replaceHint(
  lodging: Lodging | null,
  rooms: readonly InnRoom[],
  roomId: string,
  now: Date,
): string | null {
  const current = activeRoom(lodging, rooms, now)
  return current === undefined || current.id === roomId ? null : `會取代目前的${current.name}`
}
