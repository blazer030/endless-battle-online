import type { components } from "../api/schema"
import type { CharacterVo } from "./character"

type Schemas = components["schemas"]

// 回應裡的角色已由 CharacterConverter 轉成 CharacterVo
type WithCharacter<T extends { character: unknown }> = Omit<T, "character"> & {
  character: CharacterVo
}

export type ContentNames = Schemas["ContentResponse"]
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
export type ItemRequirement = Schemas["EquipmentRequirementResponse"]
export type StatBonusView = Schemas["StatBonusResponse"]
export type ShopItem = Schemas["ShopItemResponse"]
export type DepotView = Schemas["DepotResponse"]
export type DepotMove = Schemas["DepotMoveResponse"]
export type ForgeResult = Schemas["ForgeResponse"]
export type ItemUseResult = Schemas["UseItemResponse"]
export type BuffView = Schemas["BuffResponse"]
export type BuffKind = Schemas["BuffKind"]
export type InnView = Schemas["InnResponse"]
export type InnRoomView = Schemas["InnRoomResponse"]
export type LodgingView = Schemas["LodgingResponse"]
export type InnBookResult = WithCharacter<Schemas["InnBookResponse"]>
export type UnsocketResult = Schemas["UnsocketResponse"]
export type Recipe = Schemas["RecipeResponse"]
export type SynthesisResult = Schemas["SynthesisResponse"]
export type MissionBoard = Schemas["MissionBoardResponse"]
export type MissionView = Schemas["MissionResponse"]
export type MissionRewardView = Schemas["MissionRewardResponse"]
export type MissionCompletion = WithCharacter<Schemas["MissionCompleteResponse"]>
export type PetsView = Schemas["PetsResponse"]
export type PetView = Schemas["PetResponse"]
export type PetFood = Schemas["PetFoodResponse"]
export type PetPersonality = Schemas["PetPersonality"]
export type PetFeedResult = WithCharacter<Schemas["PetFeedResponse"]>
export type MercenaryOverview = Schemas["MercenaryOverviewResponse"]
export type MercenaryCatalogueEntry = Schemas["MercenaryCatalogueResponse"]
export type MercenaryContract = Schemas["MercenaryContractResponse"]
export type MercenaryHireResult = WithCharacter<Schemas["MercenaryHireResponse"]>
export type HonourShop = Schemas["HonourShopResponse"]
export type HonourExchange = Schemas["HonourExchangeResponse"]
export type HonourCost = Schemas["HonourCostResponse"]

// 產生的請求欄位允許 null（缺欄位由後端回 400）；前端送出時一律填滿
type FilledFields<T> = { [K in keyof T]-?: NonNullable<T[K]> }
export type SkillSlotCommand = FilledFields<Schemas["SkillSlotRequest"]>
