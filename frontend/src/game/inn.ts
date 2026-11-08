import { buffLabel, remainingMinutes } from "./buffs"
import type { InnRoomView, LodgingView } from "./types"

export const roomEffect = (room: InnRoomView) =>
  `${buffLabel(room.kind)} ×${room.multiplier.toFixed(2)}`

// 內容規定 seconds 為 60～86400：不到 60 分顯示分鐘（四捨五入），否則小時到小數一位，整數小時不帶小數
export function stayLength(seconds: number): string {
  const minutes = Math.round(seconds / 60)
  return minutes < 60 ? `${minutes} 分鐘` : `${Math.round((seconds / 3600) * 10) / 10} 小時`
}

// 到期或房型已被移除都視為沒有住宿加成
const activeRoom = (lodging: LodgingView | null, rooms: InnRoomView[], now: Date) =>
  lodging !== null && Date.parse(lodging.endsAt) > now.getTime()
    ? rooms.find((room) => room.id === lodging.roomId)
    : undefined

export function lodgingLine(
  lodging: LodgingView | null,
  rooms: InnRoomView[],
  now: Date,
): string | null {
  const room = activeRoom(lodging, rooms, now)
  if (room === undefined || lodging === null) return null
  const minutes = remainingMinutes(lodging.endsAt, now)
  const clock = `${Math.floor(minutes / 60)}:${String(minutes % 60).padStart(2, "0")}`
  return `${room.name}・${roomEffect(room)}（剩 ${clock}）`
}

export function replaceHint(
  lodging: LodgingView | null,
  rooms: InnRoomView[],
  roomId: string,
  now: Date,
): string | null {
  const current = activeRoom(lodging, rooms, now)
  return current === undefined || current.id === roomId ? null : `會取代目前的${current.name}`
}
