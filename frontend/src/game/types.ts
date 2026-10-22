import type { components } from "../api/schema"

type Schemas = components["schemas"]

export type CharacterView = Schemas["CharacterResponse"]
export type AdvanceResponse = Schemas["AdvanceResponse"]
export type AdvanceSummary = Schemas["AdvanceSummary"]
export type ContentNames = Schemas["ContentResponse"]
export type StatsView = Schemas["StatsResponse"]
export type GrowthStats = Schemas["BaseStats"]
export type JobGrowth = Schemas["JobGrowthContent"]
export type JobOption = Schemas["JobOptionResponse"]
export type SkillOverview = Schemas["SkillOverviewResponse"]
export type SkillView = Schemas["SkillResponse"]
export type SkillSlotView = Schemas["SkillSlotResponse"]
export type WorldArea = Schemas["WorldAreaResponse"]
export type WorldPlace = Schemas["WorldPlaceResponse"]
export type Recommendation = Schemas["Recommendation"]
export type InventoryView = Schemas["InventoryResponse"]
export type BagCategoryView = Schemas["BagCategoryResponse"]
export type InventoryItem = Schemas["InventoryItemResponse"]
export type EquippedItem = Schemas["EquippedItemResponse"]
export type ItemCategory = Schemas["ItemCategory"]
export type EquipmentSlot = Schemas["EquipmentSlot"]
export type ItemRequirement = Schemas["StatRequirement7"]
export type StatBonusView = Schemas["StatBonus"]
export type BaseStatsView = Schemas["BaseStatsResponse"]
export type ShopItem = Schemas["ShopItemResponse"]
export type DepotView = Schemas["DepotResponse"]
export type DepotMove = Schemas["DepotMoveResponse"]
export type ForgeResult = Schemas["ForgeResponse"]
export type UnsocketResult = Schemas["UnsocketResponse"]
export type Recipe = Schemas["RecipeResponse"]
export type SynthesisResult = Schemas["SynthesisResponse"]
export type MissionBoard = Schemas["MissionBoardResponse"]
export type MissionView = Schemas["MissionResponse"]
export type MissionRewardView = Schemas["MissionRewardResponse"]
export type MissionCompletion = Schemas["MissionCompleteResponse"]

// 產生的請求欄位允許 null（缺欄位由後端回 400）；前端送出時一律填滿
type FilledFields<T> = { [K in keyof T]-?: NonNullable<T[K]> }
export type StatAllocation = FilledFields<Schemas["StatAllocationRequest"]>
export type CreateCharacterCommand = Omit<
  FilledFields<Schemas["CreateCharacterRequest"]>,
  "allocation"
> & { allocation: StatAllocation }
export type SkillSlotCommand = FilledFields<Schemas["SkillSlotRequest"]>

// 產生的 type 欄位是選填；事件一律帶 type，所以改成必填以便縮小型別
export type EncounterEvent = Required<Schemas["BattleEventEncounterEvent"]>
export type RoundEvent = Required<Schemas["BattleEventRoundEvent"]>
export type AttackEvent = Required<Schemas["BattleEventAttackEvent"]>
export type SkillEvent = Required<Schemas["BattleEventSkillEvent"]>
export type PotionEvent = Required<Schemas["BattleEventPotionEvent"]>
export type DefeatEvent = Required<Schemas["BattleEventDefeatEvent"]>
export type TimeoutEvent = Required<Schemas["BattleEventTimeoutEvent"]>
export type RewardEvent = Required<Schemas["BattleEventRewardEvent"]>
export type DropResult = Schemas["DropResult"]
export type LevelUpEvent = Required<Schemas["BattleEventLevelUpEvent"]>
export type MissionProgressEvent = Required<Schemas["BattleEventMissionProgressEvent"]>
export type RestEvent = Required<Schemas["BattleEventRestEvent"]>
export type BattleEvent =
  | EncounterEvent
  | RoundEvent
  | AttackEvent
  | SkillEvent
  | PotionEvent
  | DefeatEvent
  | TimeoutEvent
  | RewardEvent
  | LevelUpEvent
  | MissionProgressEvent
  | RestEvent
