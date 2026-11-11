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
