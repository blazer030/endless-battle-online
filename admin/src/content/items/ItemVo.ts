import type { ContentElement } from "../shared/elements"

export const itemCategories = [
  "Weapon",
  "Head",
  "Body",
  "Hand",
  "Foot",
  "Accessory",
  "Consumable",
  "Stone",
  "RefineStone",
  "Material",
  "Medal",
  "Key",
] as const
export type ItemCategory = (typeof itemCategories)[number]

export const consumableKinds = [
  "Supply",
  "StatPotion",
  "Elixir",
  "Capture",
  "ExperienceCapsule",
  "BagExpansion",
  "DepotExpansion",
  "ProficiencyBook",
  "StoneBox",
  "RefineBox",
  "PetEgg",
] as const
export type ConsumableKind = (typeof consumableKinds)[number]

// 其他種類的消耗品共用 amount，意義依種類不同
export const usesAmount = (kind: ConsumableKind | null) =>
  kind !== null && kind !== "Supply" && kind !== "StatPotion" && kind !== "Elixir"

// 魔石與精鍊石不會綁定
export const canBeBound = (category: ItemCategory) =>
  category !== "Stone" && category !== "RefineStone"

export const elixirKinds = ["Exp", "JobExp", "Attack", "Magic", "Defense", "MagicDefense"] as const
export type ElixirKind = (typeof elixirKinds)[number]

export const itemStatKeys = [
  "strength",
  "intelligence",
  "life",
  "vitality",
  "agility",
  "charisma",
  "faith",
  "attack",
  "magicAttack",
  "defense",
  "magicDefense",
  "maxHitPoints",
] as const
export type ItemStatKey = (typeof itemStatKeys)[number]

export type ItemStatsVo = Readonly<Record<ItemStatKey, number>>

export const itemRequirementKeys = ["strength", "intelligence", "agility", "charisma"] as const
export type ItemRequirementKey = (typeof itemRequirementKeys)[number]

export type ItemRequirementVo = Readonly<Record<ItemRequirementKey, number>>

export const itemRefineKeys = [
  "attack",
  "magicAttack",
  "defense",
  "magicDefense",
  "strength",
  "intelligence",
  "life",
  "vitality",
  "agility",
  "charisma",
  "faith",
] as const
export type ItemRefineKey = (typeof itemRefineKeys)[number]

export type ItemRangeVo = { readonly minimum: number; readonly maximum: number }

export type ItemRefineVo = {
  readonly level: number
  readonly kind: number
  readonly ranges: Readonly<Partial<Record<ItemRefineKey, ItemRangeVo>>>
}

export type ItemStoneVo = {
  readonly random: boolean
  readonly fixed: ItemStatsVo | null
  readonly minimum: ItemStatsVo | null
  readonly maximum: ItemStatsVo | null
  readonly boxClass: number | null
}

export type ItemKeyVo = { readonly placeId: number; readonly seconds: number }

export type ItemElixirVo = {
  readonly kind: ElixirKind
  readonly multiplier: number
  readonly seconds: number
}

export type ItemWeaponSkillVo = {
  readonly skillId: string
  readonly level: number
  readonly activationRate: number
}

type ItemVoParams = {
  id: string
  name: string
  category: ItemCategory
  consumableKind: ConsumableKind | null
  bonus: ItemStatsVo | null
  supplyHitPoints: number | null
  supplySkillPoints: number | null
  amount: number | null
  requires: ItemRequirementVo | null
  element: ContentElement | null
  holes: number | null
  refineMax: number | null
  price: number
  shopLevel: number
  shopSellable: boolean
  bound: boolean
  refine: ItemRefineVo | null
  stone: ItemStoneVo | null
  key: ItemKeyVo | null
  elixir: ItemElixirVo | null
  weaponSkill: ItemWeaponSkillVo | null
}

export class ItemVo {
  readonly id: string
  readonly name: string
  readonly category: ItemCategory
  readonly consumableKind: ConsumableKind | null
  readonly bonus: ItemStatsVo | null
  readonly supplyHitPoints: number | null
  readonly supplySkillPoints: number | null
  readonly amount: number | null
  readonly requires: ItemRequirementVo | null
  readonly element: ContentElement | null
  readonly holes: number | null
  readonly refineMax: number | null
  readonly price: number
  readonly shopLevel: number
  readonly shopSellable: boolean
  readonly bound: boolean
  readonly refine: ItemRefineVo | null
  readonly stone: ItemStoneVo | null
  readonly key: ItemKeyVo | null
  readonly elixir: ItemElixirVo | null
  readonly weaponSkill: ItemWeaponSkillVo | null

  private constructor(params: ItemVoParams) {
    this.id = params.id
    this.name = params.name
    this.category = params.category
    this.consumableKind = params.consumableKind
    this.bonus = params.bonus
    this.supplyHitPoints = params.supplyHitPoints
    this.supplySkillPoints = params.supplySkillPoints
    this.amount = params.amount
    this.requires = params.requires
    this.element = params.element
    this.holes = params.holes
    this.refineMax = params.refineMax
    this.price = params.price
    this.shopLevel = params.shopLevel
    this.shopSellable = params.shopSellable
    this.bound = params.bound
    this.refine = params.refine
    this.stone = params.stone
    this.key = params.key
    this.elixir = params.elixir
    this.weaponSkill = params.weaponSkill
  }

  static create(params: ItemVoParams): ItemVo {
    return new ItemVo(params)
  }
}
