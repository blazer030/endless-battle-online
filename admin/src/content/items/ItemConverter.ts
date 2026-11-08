import type { components } from "../../api/schema"
import { contentElements } from "../shared/elements"
import {
  parseBoolean,
  parseNumber,
  parseObject,
  parseOneOf,
  parseOptionalNumber,
  parseString,
} from "../shared/parse"
import {
  canBeBound,
  consumableKinds,
  elixirKinds,
  itemCategories,
  itemRefineKeys,
  itemRequirementKeys,
  itemStatKeys,
  ItemVo,
  usesAmount,
  type ConsumableKind,
  type ItemCategory,
  type ItemRefineVo,
  type ItemRequirementVo,
  type ItemStatsVo,
  type ItemStoneVo,
} from "./ItemVo"

export type ItemDto = components["schemas"]["ItemRecord"]

const isPresent = (value: unknown) => value !== undefined && value !== null

const parseStats = (value: unknown, path: string): ItemStatsVo | null => {
  if (!isPresent(value)) {
    return null
  }
  const stats = parseObject(value, path)
  return Object.fromEntries(
    itemStatKeys.map((key) => [key, parseNumber(stats[key], `${path}.${key}`)]),
  ) as ItemStatsVo
}

const parseRequirement = (value: unknown, path: string): ItemRequirementVo | null => {
  if (!isPresent(value)) {
    return null
  }
  const requirement = parseObject(value, path)
  return Object.fromEntries(
    itemRequirementKeys.map((key) => [key, parseNumber(requirement[key], `${path}.${key}`)]),
  ) as ItemRequirementVo
}

const parseRefine = (value: unknown, path: string): ItemRefineVo | null => {
  if (!isPresent(value)) {
    return null
  }
  const refine = parseObject(value, path)
  const ranges: Record<string, { minimum: number; maximum: number }> = {}
  for (const key of itemRefineKeys) {
    if (isPresent(refine[key])) {
      const range = parseObject(refine[key], `${path}.${key}`)
      ranges[key] = {
        minimum: parseNumber(range.minimum, `${path}.${key}.minimum`),
        maximum: parseNumber(range.maximum, `${path}.${key}.maximum`),
      }
    }
  }
  return {
    level: parseNumber(refine.level, `${path}.level`),
    kind: parseNumber(refine.kind, `${path}.kind`),
    ranges,
  }
}

const parseStone = (value: unknown, path: string): ItemStoneVo | null => {
  if (!isPresent(value)) {
    return null
  }
  const stone = parseObject(value, path)
  return {
    random: parseBoolean(stone.random, `${path}.random`),
    fixed: parseStats(stone.fixed, `${path}.fixed`),
    minimum: parseStats(stone.minimum, `${path}.minimum`),
    maximum: parseStats(stone.maximum, `${path}.maximum`),
    boxClass: parseOptionalNumber(stone.boxClass, `${path}.boxClass`) ?? null,
  }
}

const isZero = (values: Readonly<Record<string, number>>) =>
  Object.values(values).every((value) => value === 0)

// 表單裡沒填的能力就是 0；全部是 0 代表這個欄位不存在
const statsToDto = (stats: ItemStatsVo | null) =>
  stats === null || isZero(stats) ? undefined : { ...stats }

const equipmentCategories: readonly ItemCategory[] = ["Weapon", "Head", "Body", "Hand", "Foot"]

const hasBonus = (category: ItemCategory, kind: ConsumableKind | null) =>
  equipmentCategories.includes(category) || category === "Accessory" || kind === "StatPotion"

export const ItemConverter = {
  toVo(dto: ItemDto, path = "item"): ItemVo {
    const raw = dto as unknown as Record<string, unknown>
    const nested = (name: string) =>
      isPresent(raw[name]) ? parseObject(raw[name], `${path}.${name}`) : null
    const key = nested("key")
    const elixir = nested("elixir")
    const weaponSkill = nested("weaponSkill")
    return ItemVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      category: parseOneOf(dto.category, `${path}.category`, itemCategories),
      consumableKind: isPresent(dto.consumableKind)
        ? parseOneOf(dto.consumableKind, `${path}.consumableKind`, consumableKinds)
        : null,
      bonus: parseStats(dto.bonus, `${path}.bonus`),
      supplyHitPoints: parseOptionalNumber(dto.supplyHitPoints, `${path}.supplyHitPoints`) ?? null,
      supplySkillPoints:
        parseOptionalNumber(dto.supplySkillPoints, `${path}.supplySkillPoints`) ?? null,
      amount: parseOptionalNumber(dto.amount, `${path}.amount`) ?? null,
      requires: parseRequirement(dto.requires, `${path}.requires`),
      element: isPresent(dto.element)
        ? parseOneOf(dto.element, `${path}.element`, contentElements)
        : null,
      holes: parseOptionalNumber(dto.holes, `${path}.holes`) ?? null,
      refineMax: parseOptionalNumber(dto.refineMax, `${path}.refineMax`) ?? null,
      price: parseNumber(dto.price, `${path}.price`),
      shopLevel: parseNumber(dto.shopLevel, `${path}.shopLevel`),
      shopSellable: parseBoolean(dto.shopSellable, `${path}.shopSellable`),
      bound: parseBoolean(dto.bound, `${path}.bound`),
      refine: parseRefine(dto.refine, `${path}.refine`),
      stone: parseStone(dto.stone, `${path}.stone`),
      key: key && {
        placeId: parseNumber(key.placeId, `${path}.key.placeId`),
        seconds: parseNumber(key.seconds, `${path}.key.seconds`),
      },
      elixir: elixir && {
        kind: parseOneOf(elixir.kind, `${path}.elixir.kind`, elixirKinds),
        multiplier: parseNumber(elixir.multiplier, `${path}.elixir.multiplier`),
        seconds: parseNumber(elixir.seconds, `${path}.elixir.seconds`),
      },
      weaponSkill: weaponSkill && {
        skillId: parseString(weaponSkill.skillId, `${path}.weaponSkill.skillId`),
        level: parseNumber(weaponSkill.level, `${path}.weaponSkill.level`),
        activationRate: parseNumber(
          weaponSkill.activationRate,
          `${path}.weaponSkill.activationRate`,
        ),
      },
    })
  },

  // 只輸出這個分類與消耗品種類的欄位，切換分類後留在表單裡的值不會送出
  toDto(vo: ItemVo): ItemDto {
    const { category } = vo
    const kind = category === "Consumable" ? vo.consumableKind : null
    const isEquipment = equipmentCategories.includes(category)
    const dto: ItemDto = {
      id: vo.id,
      name: vo.name,
      category,
      price: vo.price,
      shopLevel: vo.shopLevel,
      shopSellable: vo.shopSellable,
      bound: canBeBound(category) && vo.bound,
    }
    if (kind !== null) {
      dto.consumableKind = kind
    }
    if (hasBonus(category, kind)) {
      dto.bonus = statsToDto(vo.bonus)
    }
    if (kind === "Supply") {
      dto.supplyHitPoints = vo.supplyHitPoints ?? undefined
      dto.supplySkillPoints = vo.supplySkillPoints ?? undefined
    }
    if (usesAmount(kind)) {
      dto.amount = vo.amount ?? undefined
    }
    if ((isEquipment || category === "Accessory") && vo.requires !== null && !isZero(vo.requires)) {
      dto.requires = { ...vo.requires }
    }
    if (isEquipment) {
      dto.holes = vo.holes ?? undefined
      dto.refineMax = vo.refineMax ?? undefined
    }
    if (category === "Weapon") {
      dto.element = vo.element ?? undefined
      dto.weaponSkill = vo.weaponSkill ? { ...vo.weaponSkill } : undefined
    }
    if (kind === "Elixir" && vo.elixir) {
      dto.elixir = { ...vo.elixir }
    }
    if (category === "RefineStone" && vo.refine) {
      dto.refine = { level: vo.refine.level, kind: vo.refine.kind, ...vo.refine.ranges }
    }
    if (category === "Stone" && vo.stone) {
      dto.stone = {
        random: vo.stone.random,
        ...(vo.stone.random
          ? { minimum: statsToDto(vo.stone.minimum), maximum: statsToDto(vo.stone.maximum) }
          : { fixed: statsToDto(vo.stone.fixed) }),
        boxClass: vo.stone.boxClass ?? undefined,
      }
    }
    if (category === "Key" && vo.key) {
      dto.key = { ...vo.key }
    }
    return dto
  },
}
