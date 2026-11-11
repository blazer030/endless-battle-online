import type { components } from "../api/schema"
import type { CharacterVo } from "./character"

type Schemas = components["schemas"]

// 回應裡的角色已由 CharacterConverter 轉成 CharacterVo
type WithCharacter<T extends { character: unknown }> = Omit<T, "character"> & {
  character: CharacterVo
}

export type BuffView = Schemas["BuffResponse"]
export type InnView = Schemas["InnResponse"]
export type InnRoomView = Schemas["InnRoomResponse"]
export type LodgingView = Schemas["LodgingResponse"]
export type InnBookResult = WithCharacter<Schemas["InnBookResponse"]>
