import type { components } from "../api/schema"

type Schemas = components["schemas"]

export type CharacterView = Schemas["CharacterResponse"]
export type AdvanceResponse = Schemas["AdvanceResponse"]
export type ContentNames = Schemas["ContentResponse"]
export type StatsView = Schemas["StatsResponse"]

// 產生的請求欄位允許 null（缺欄位由後端回 400）；前端送出時一律填滿
type FilledFields<T> = { [K in keyof T]-?: NonNullable<T[K]> }
export type StatAllocation = FilledFields<Schemas["StatAllocationRequest"]>
export type CreateCharacterCommand = Omit<
  FilledFields<Schemas["CreateCharacterRequest"]>,
  "allocation"
> & { allocation: StatAllocation }

// 產生的 type 欄位是選填；事件一律帶 type，所以改成必填以便縮小型別
export type EncounterEvent = Required<Schemas["BattleEventEncounterEvent"]>
export type AttackEvent = Required<Schemas["BattleEventAttackEvent"]>
export type DefeatEvent = Required<Schemas["BattleEventDefeatEvent"]>
export type RewardEvent = Required<Schemas["BattleEventRewardEvent"]>
export type BattleEvent = EncounterEvent | AttackEvent | DefeatEvent | RewardEvent
