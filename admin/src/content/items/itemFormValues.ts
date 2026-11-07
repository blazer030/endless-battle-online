import type { ContentElement } from "../monsters/MonsterVo"
import {
  itemRefineKeys,
  itemRequirementKeys,
  itemStatKeys,
  ItemVo,
  type ConsumableKind,
  type ElixirKind,
  type ItemCategory,
  type ItemRefineKey,
  type ItemRequirementVo,
  type ItemStatsVo,
} from "./ItemVo"

// 表單的能力值可以留白，留白就是 0
type StatValues = Partial<Record<string, number | null>>

type RangeValues = { minimum?: number | null; maximum?: number | null }

export type ItemFormValues = {
  id: string
  name: string
  category: ItemCategory
  consumableKind: ConsumableKind
  bonus: StatValues
  supplyHitPoints: number | null
  supplySkillPoints: number | null
  amount: number | null
  requires: StatValues
  // 空字串代表沒有屬性
  element: ContentElement | ""
  holes: number | null
  refineMax: number | null
  price: number
  shopLevel: number
  shopSellable: boolean
  bound: boolean
  // 空字串代表沒有武器技能
  weaponSkillId: string
  weaponSkillLevel: number | null
  weaponSkillRate: number | null
  elixir: { kind: ElixirKind; multiplier: number | null; seconds: number | null }
  key: { placeId: number | null; seconds: number | null }
  refine: {
    level: number | null
    kind: number | null
    ranges: Partial<Record<ItemRefineKey, RangeValues>>
  }
  stone: {
    random: boolean
    fixed: StatValues
    minimum: StatValues
    maximum: StatValues
    boxClass: number | null
  }
}

export const blankItemValues: ItemFormValues = {
  id: "",
  name: "",
  category: "Weapon",
  consumableKind: "Supply",
  bonus: {},
  supplyHitPoints: null,
  supplySkillPoints: null,
  amount: null,
  requires: {},
  element: "",
  holes: null,
  refineMax: null,
  price: 0,
  shopLevel: 1,
  shopSellable: false,
  bound: false,
  weaponSkillId: "",
  weaponSkillLevel: null,
  weaponSkillRate: null,
  elixir: { kind: "Exp", multiplier: null, seconds: null },
  key: { placeId: null, seconds: null },
  refine: { level: null, kind: null, ranges: {} },
  stone: { random: false, fixed: {}, minimum: {}, maximum: {}, boxClass: null },
}

// 0 在表單裡顯示成留白，只留下有數值的能力
const withoutZeros = (values: Readonly<Record<string, number>> | null): StatValues =>
  Object.fromEntries(Object.entries(values ?? {}).filter(([, value]) => value !== 0))

export const itemToValues = (item: ItemVo): ItemFormValues => ({
  id: item.id,
  name: item.name,
  category: item.category,
  consumableKind: item.consumableKind ?? "Supply",
  bonus: withoutZeros(item.bonus),
  supplyHitPoints: item.supplyHitPoints,
  supplySkillPoints: item.supplySkillPoints,
  amount: item.amount,
  requires: withoutZeros(item.requires),
  element: item.element ?? "",
  holes: item.holes,
  refineMax: item.refineMax,
  price: item.price,
  shopLevel: item.shopLevel,
  shopSellable: item.shopSellable,
  bound: item.bound,
  weaponSkillId: item.weaponSkill?.skillId ?? "",
  weaponSkillLevel: item.weaponSkill?.level ?? null,
  weaponSkillRate: item.weaponSkill?.activationRate ?? null,
  elixir: item.elixir ? { ...item.elixir } : blankItemValues.elixir,
  key: item.key ? { ...item.key } : blankItemValues.key,
  refine: item.refine
    ? { level: item.refine.level, kind: item.refine.kind, ranges: { ...item.refine.ranges } }
    : blankItemValues.refine,
  stone: item.stone
    ? {
        random: item.stone.random,
        fixed: withoutZeros(item.stone.fixed),
        minimum: withoutZeros(item.stone.minimum),
        maximum: withoutZeros(item.stone.maximum),
        boxClass: item.stone.boxClass,
      }
    : blankItemValues.stone,
})

const statsFromValues = (values: StatValues | undefined) =>
  Object.fromEntries(itemStatKeys.map((key) => [key, values?.[key] ?? 0])) as ItemStatsVo

const requirementFromValues = (values: StatValues | undefined) =>
  Object.fromEntries(
    itemRequirementKeys.map((key) => [key, values?.[key] ?? 0]),
  ) as ItemRequirementVo

// 最小、最大都留白代表這個能力不加
function rangesFromValues(ranges: ItemFormValues["refine"]["ranges"] | undefined) {
  const result: Partial<Record<ItemRefineKey, { minimum: number; maximum: number }>> = {}
  for (const key of itemRefineKeys) {
    const range = ranges?.[key]
    if ((range?.minimum ?? null) !== null || (range?.maximum ?? null) !== null) {
      result[key] = { minimum: range?.minimum ?? 0, maximum: range?.maximum ?? 0 }
    }
  }
  return result
}

export const itemFromValues = (values: ItemFormValues): ItemVo =>
  ItemVo.create({
    id: values.id.trim(),
    name: values.name,
    category: values.category,
    consumableKind: values.consumableKind,
    bonus: statsFromValues(values.bonus),
    supplyHitPoints: values.supplyHitPoints,
    supplySkillPoints: values.supplySkillPoints,
    amount: values.amount,
    requires: requirementFromValues(values.requires),
    element: values.element === "" ? null : values.element,
    holes: values.holes,
    refineMax: values.refineMax,
    price: values.price,
    shopLevel: values.shopLevel,
    shopSellable: values.shopSellable,
    bound: values.bound,
    weaponSkill:
      values.weaponSkillId === ""
        ? null
        : {
            skillId: values.weaponSkillId,
            level: values.weaponSkillLevel ?? 1,
            activationRate: values.weaponSkillRate ?? 1,
          },
    elixir: {
      kind: values.elixir.kind,
      multiplier: values.elixir.multiplier ?? 1,
      seconds: values.elixir.seconds ?? 0,
    },
    key: { placeId: values.key.placeId ?? 0, seconds: values.key.seconds ?? 0 },
    refine: {
      level: values.refine.level ?? 1,
      kind: values.refine.kind ?? 1,
      ranges: rangesFromValues(values.refine.ranges),
    },
    stone: {
      random: values.stone.random,
      fixed: statsFromValues(values.stone.fixed),
      minimum: statsFromValues(values.stone.minimum),
      maximum: statsFromValues(values.stone.maximum),
      boxClass: values.stone.boxClass,
    },
  })
