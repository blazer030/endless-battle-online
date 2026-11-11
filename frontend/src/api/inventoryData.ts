import {
  consumableKinds,
  DepotMoveVo,
  DepotVo,
  equipmentSlotIds,
  InventoryVo,
  itemCategoryIds,
  ItemUseVo,
  ShopItemVo,
  type BagCategory,
  type EquipmentRequirement,
  type EquippedItem,
  type InventoryItem,
  type Socket,
  type StatBonus,
  type WeaponSkill,
} from "../game/inventory"
import { BuffConverter } from "./lodgingData"
import {
  parseArray,
  parseBoolean,
  parseNullableNumber,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
} from "./parse"

export const toStatBonus = (value: unknown, path: string): StatBonus => {
  const dto = parseObject(value, path)
  const stat = (key: string) => parseNumber(dto[key], `${path}.${key}`)
  return {
    strength: stat("strength"),
    intelligence: stat("intelligence"),
    life: stat("life"),
    vitality: stat("vitality"),
    agility: stat("agility"),
    charisma: stat("charisma"),
    faith: stat("faith"),
    attack: stat("attack"),
    magicAttack: stat("magicAttack"),
    defense: stat("defense"),
    magicDefense: stat("magicDefense"),
    maxHealthPoints: stat("maxHealthPoints"),
  }
}

const toRequirement = (value: unknown, path: string): EquipmentRequirement => {
  const dto = parseObject(value, path)
  return {
    strength: parseNumber(dto.strength, `${path}.strength`),
    intelligence: parseNumber(dto.intelligence, `${path}.intelligence`),
    agility: parseNumber(dto.agility, `${path}.agility`),
    charisma: parseNumber(dto.charisma, `${path}.charisma`),
  }
}

const toSocket = (value: unknown, path: string): Socket => {
  const dto = parseObject(value, path)
  return {
    hole: parseNumber(dto.hole, `${path}.hole`),
    stoneId: parseString(dto.stoneId, `${path}.stoneId`),
    stoneName: parseString(dto.stoneName, `${path}.stoneName`),
    rolled: toStatBonus(dto.rolled, `${path}.rolled`),
  }
}

const toWeaponSkill = (value: unknown, path: string): WeaponSkill | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    name: parseString(dto.name, `${path}.name`),
    level: parseNumber(dto.level, `${path}.level`),
    rate: parseNumber(dto.rate, `${path}.rate`),
  }
}

export const toInventoryItem = (value: unknown, path: string): InventoryItem => {
  const dto = parseObject(value, path)
  return {
    inventoryId: parseNumber(dto.inventoryId, `${path}.inventoryId`),
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    category: parseOneOf(dto.category, itemCategoryIds, `${path}.category`),
    consumable: parseOneOf(dto.consumable, consumableKinds, `${path}.consumable`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    bonus: toStatBonus(dto.bonus, `${path}.bonus`),
    requires: toRequirement(dto.requires, `${path}.requires`),
    jobId: parseNullableString(dto.jobId, `${path}.jobId`),
    supplyHealthPoints: parseNumber(dto.supplyHealthPoints, `${path}.supplyHealthPoints`),
    supplyManaPoints: parseNumber(dto.supplyManaPoints, `${path}.supplyManaPoints`),
    amount: parseNumber(dto.amount, `${path}.amount`),
    refineLevel: parseNumber(dto.refineLevel, `${path}.refineLevel`),
    refineMax: parseNumber(dto.refineMax, `${path}.refineMax`),
    refineBonus: toStatBonus(dto.refineBonus, `${path}.refineBonus`),
    holes: parseNumber(dto.holes, `${path}.holes`),
    sockets: parseArray(dto.sockets, `${path}.sockets`, toSocket),
    price: parseNumber(dto.price, `${path}.price`),
    bound: parseBoolean(dto.bound, `${path}.bound`),
    refineStoneLevel: parseNullableNumber(dto.refineStoneLevel, `${path}.refineStoneLevel`),
    enhanced: parseBoolean(dto.enhanced, `${path}.enhanced`),
    weaponSkill: toWeaponSkill(dto.weaponSkill, `${path}.weaponSkill`),
  }
}

const toBagCategory = (value: unknown, path: string): BagCategory => {
  const dto = parseObject(value, path)
  return {
    category: parseOneOf(dto.category, itemCategoryIds, `${path}.category`),
    capacity: parseNumber(dto.capacity, `${path}.capacity`),
    items: parseArray(dto.items, `${path}.items`, toInventoryItem),
  }
}

const toEquippedItem = (value: unknown, path: string): EquippedItem => {
  const dto = parseObject(value, path)
  return {
    slot: parseOneOf(dto.slot, equipmentSlotIds, `${path}.slot`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    item: toInventoryItem(dto.item, `${path}.item`),
  }
}

// 其他回應裡的 inventory 也用這個轉換，路徑接在外層之後（例如 forge.inventory）
export const InventoryConverter = {
  toVo(value: unknown, path = "inventory"): InventoryVo {
    const dto = parseObject(value, path)
    return InventoryVo.create({
      bag: parseArray(dto.bag, `${path}.bag`, toBagCategory),
      equipment: parseArray(dto.equipment, `${path}.equipment`, toEquippedItem),
      bonus: toStatBonus(dto.bonus, `${path}.bonus`),
    })
  },
}

export const ShopItemConverter = {
  toVo(value: unknown, path = "shop"): ShopItemVo {
    const dto = parseObject(value, path)
    return ShopItemVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      category: parseOneOf(dto.category, itemCategoryIds, `${path}.category`),
      price: parseNumber(dto.price, `${path}.price`),
      bonus: toStatBonus(dto.bonus, `${path}.bonus`),
      requires: toRequirement(dto.requires, `${path}.requires`),
      holes: parseNumber(dto.holes, `${path}.holes`),
      refineMax: parseNumber(dto.refineMax, `${path}.refineMax`),
    })
  },
}

export const DepotConverter = {
  toVo(value: unknown, path = "depot"): DepotVo {
    const dto = parseObject(value, path)
    return DepotVo.create({
      entries: parseArray(dto.entries, `${path}.entries`, toInventoryItem),
      capacity: parseNumber(dto.capacity, `${path}.capacity`),
    })
  },
}

export const DepotMoveConverter = {
  toVo(value: unknown, path = "depotMove"): DepotMoveVo {
    const dto = parseObject(value, path)
    return DepotMoveVo.create({
      inventory: InventoryConverter.toVo(dto.inventory, `${path}.inventory`),
      depot: DepotConverter.toVo(dto.depot, `${path}.depot`),
    })
  },
}

export const ItemUseConverter = {
  toVo(value: unknown, path = "itemUse"): ItemUseVo {
    const dto = parseObject(value, path)
    return ItemUseVo.create({
      inventory: InventoryConverter.toVo(dto.inventory, `${path}.inventory`),
      obtained: parseArray(dto.obtained, `${path}.obtained`, parseString),
      petName: parseNullableString(dto.petName, `${path}.petName`),
      skillPoints: parseNumber(dto.skillPoints, `${path}.skillPoints`),
      levelsGained: parseNumber(dto.levelsGained, `${path}.levelsGained`),
      buff: dto.buff === null ? null : BuffConverter.toVo(dto.buff, `${path}.buff`),
    })
  },
}
