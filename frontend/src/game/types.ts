import type { components } from "../api/schema"

type Schemas = components["schemas"]

export type CharacterView = Schemas["CharacterResponse"]
export type AdvanceResponse = Schemas["AdvanceResponse"]
export type ContentNames = Schemas["ContentResponse"]

// 產生的 type 欄位是選填；事件一律帶 type，所以改成必填以便縮小型別
export type EncounterEvent = Required<Schemas["BattleEventEncounterEvent"]>
export type AttackEvent = Required<Schemas["BattleEventAttackEvent"]>
export type DefeatEvent = Required<Schemas["BattleEventDefeatEvent"]>
export type RewardEvent = Required<Schemas["BattleEventRewardEvent"]>
export type BattleEvent = EncounterEvent | AttackEvent | DefeatEvent | RewardEvent
