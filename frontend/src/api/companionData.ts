import { buffKinds } from "../game/buffs"
import {
  honourExchangeKinds,
  honourRequirementStats,
  HonourShopVo,
  type HonourCost,
  type HonourExchange,
  type HonourItemDetail,
  type HonourMedal,
  type HonourMercenaryDetail,
  type HonourRequirement,
} from "../game/honour"
import { consumableKinds, itemCategoryIds } from "../game/inventory"
import {
  MercenaryHireVo,
  MercenaryOverviewVo,
  type MercenaryCatalogueEntry,
  type MercenaryContract,
} from "../game/mercenaries"
import {
  PetFeedVo,
  petFoodStats,
  petPersonalities,
  PetsVo,
  type Pet,
  type PetFood,
} from "../game/pets"
import { CharacterConverter } from "./characterData"
import { toStatBonus } from "./inventoryData"
import {
  ParseError,
  parseArray,
  parseBoolean,
  parseNullableNumber,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
  parseTimestamp,
} from "./parse"

const toPet = (value: unknown, path: string): Pet => {
  const dto = parseObject(value, path)
  return {
    id: parseNumber(dto.id, `${path}.id`),
    monsterId: parseString(dto.monsterId, `${path}.monsterId`),
    species: parseString(dto.species, `${path}.species`),
    name: parseString(dto.name, `${path}.name`),
    personality: parseOneOf(dto.personality, petPersonalities, `${path}.personality`),
    attack: parseNumber(dto.attack, `${path}.attack`),
    magic: parseNumber(dto.magic, `${path}.magic`),
    defense: parseNumber(dto.defense, `${path}.defense`),
    spirit: parseNumber(dto.spirit, `${path}.spirit`),
    satiety: parseNumber(dto.satiety, `${path}.satiety`),
    intimacy: parseNumber(dto.intimacy, `${path}.intimacy`),
    carried: parseBoolean(dto.carried, `${path}.carried`),
    age: parseNumber(dto.age, `${path}.age`),
    nextFeedAt: parseTimestamp(dto.nextFeedAt, `${path}.nextFeedAt`),
    activationPercent: parseNumber(dto.activationPercent, `${path}.activationPercent`),
  }
}

const toFood = (value: unknown, path: string): PetFood => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    stat: parseOneOf(dto.stat, petFoodStats, `${path}.stat`),
    amount: parseNumber(dto.amount, `${path}.amount`),
    cost: parseNumber(dto.cost, `${path}.cost`),
  }
}

export const PetsConverter = {
  toVo(value: unknown, path = "pets"): PetsVo {
    const dto = parseObject(value, path)
    return PetsVo.create({
      pets: parseArray(dto.pets, `${path}.pets`, toPet),
      maxPets: parseNumber(dto.maxPets, `${path}.maxPets`),
      foods: parseArray(dto.foods, `${path}.foods`, toFood),
    })
  },
}

export const PetFeedConverter = {
  toVo(value: unknown, path = "petFeed"): PetFeedVo {
    const dto = parseObject(value, path)
    return PetFeedVo.create({
      pets: PetsConverter.toVo(dto.pets, `${path}.pets`),
      character: CharacterConverter.toVo(dto.character, `${path}.character`),
    })
  },
}

const toCatalogueEntry = (value: unknown, path: string): MercenaryCatalogueEntry => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    price: parseNullableNumber(dto.price, `${path}.price`),
    fights: parseNumber(dto.fights, `${path}.fights`),
    moneyPercent: parseNumber(dto.moneyPercent, `${path}.moneyPercent`),
    dropPercent: parseNumber(dto.dropPercent, `${path}.dropPercent`),
    experiencePercent: parseNumber(dto.experiencePercent, `${path}.experiencePercent`),
    deathCost: parseNumber(dto.deathCost, `${path}.deathCost`),
  }
}

const toContract = (value: unknown, path: string): MercenaryContract | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    mercenaryId: parseString(dto.mercenaryId, `${path}.mercenaryId`),
    name: parseString(dto.name, `${path}.name`),
    remaining: parseNumber(dto.remaining, `${path}.remaining`),
    placeId: parseNullableNumber(dto.placeId, `${path}.placeId`),
    placeName: parseNullableString(dto.placeName, `${path}.placeName`),
    fighting: parseBoolean(dto.fighting, `${path}.fighting`),
    nextFightAt:
      dto.nextFightAt === null ? null : parseTimestamp(dto.nextFightAt, `${path}.nextFightAt`),
  }
}

export const MercenaryOverviewConverter = {
  toVo(value: unknown, path = "mercenaries"): MercenaryOverviewVo {
    const dto = parseObject(value, path)
    return MercenaryOverviewVo.create({
      catalogue: parseArray(dto.catalogue, `${path}.catalogue`, toCatalogueEntry),
      contract: toContract(dto.contract, `${path}.contract`),
    })
  },
}

export const MercenaryHireConverter = {
  toVo(value: unknown, path = "mercenaryHire"): MercenaryHireVo {
    const dto = parseObject(value, path)
    return MercenaryHireVo.create({
      mercenaries: MercenaryOverviewConverter.toVo(dto.mercenaries, `${path}.mercenaries`),
      character: CharacterConverter.toVo(dto.character, `${path}.character`),
    })
  },
}

const toMedal = (value: unknown, path: string): HonourMedal => {
  const dto = parseObject(value, path)
  return {
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    owned: parseNumber(dto.owned, `${path}.owned`),
  }
}

const toCost = (value: unknown, path: string): HonourCost => {
  const dto = parseObject(value, path)
  return {
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    owned: parseNumber(dto.owned, `${path}.owned`),
  }
}

const toRequirement = (value: unknown, path: string): HonourRequirement => {
  const dto = parseObject(value, path)
  return {
    stat: parseOneOf(dto.stat, honourRequirementStats, `${path}.stat`),
    minimum: parseNumber(dto.minimum, `${path}.minimum`),
    met: parseBoolean(dto.met, `${path}.met`),
  }
}

const toElixir = (value: unknown, path: string): HonourItemDetail["elixir"] => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    kind: parseOneOf(dto.kind, buffKinds, `${path}.kind`),
    multiplier: parseNumber(dto.multiplier, `${path}.multiplier`),
    seconds: parseNumber(dto.seconds, `${path}.seconds`),
  }
}

const toKey = (value: unknown, path: string): HonourItemDetail["key"] => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    placeName: parseString(dto.placeName, `${path}.placeName`),
    seconds: parseNumber(dto.seconds, `${path}.seconds`),
  }
}

const toItemDetail = (value: unknown, path: string): HonourItemDetail => {
  const dto = parseObject(value, path)
  return {
    category: parseOneOf(dto.category, itemCategoryIds, `${path}.category`),
    consumable: parseOneOf(dto.consumable, consumableKinds, `${path}.consumable`),
    bonus: toStatBonus(dto.bonus, `${path}.bonus`),
    requirements: parseArray(dto.requirements, `${path}.requirements`, toRequirement),
    holes: parseNumber(dto.holes, `${path}.holes`),
    refineMax: parseNumber(dto.refineMax, `${path}.refineMax`),
    bound: parseBoolean(dto.bound, `${path}.bound`),
    supplyHealthPoints: parseNumber(dto.supplyHealthPoints, `${path}.supplyHealthPoints`),
    supplyManaPoints: parseNumber(dto.supplyManaPoints, `${path}.supplyManaPoints`),
    amount: parseNumber(dto.amount, `${path}.amount`),
    elixir: toElixir(dto.elixir, `${path}.elixir`),
    key: toKey(dto.key, `${path}.key`),
  }
}

const toMercenaryDetail = (value: unknown, path: string): HonourMercenaryDetail => {
  const dto = parseObject(value, path)
  return {
    fights: parseNumber(dto.fights, `${path}.fights`),
    moneyPercent: parseNumber(dto.moneyPercent, `${path}.moneyPercent`),
    dropPercent: parseNumber(dto.dropPercent, `${path}.dropPercent`),
    experiencePercent: parseNumber(dto.experiencePercent, `${path}.experiencePercent`),
    deathCost: parseNumber(dto.deathCost, `${path}.deathCost`),
  }
}

// 依 kind 只能有自己那一份詳情，另一份必須是 null
const detailFor = <T>(
  value: unknown,
  wanted: boolean,
  path: string,
  parse: (detail: unknown, detailPath: string) => T,
): T | null => {
  if (!wanted) {
    if (value !== null) throw new ParseError(path, "null")
    return null
  }
  return parse(value, path)
}

const toExchange = (value: unknown, path: string): HonourExchange => {
  const dto = parseObject(value, path)
  const kind = parseOneOf(dto.kind, honourExchangeKinds, `${path}.kind`)
  return {
    id: parseString(dto.id, `${path}.id`),
    kind,
    rewardId: parseString(dto.rewardId, `${path}.rewardId`),
    rewardName: parseString(dto.rewardName, `${path}.rewardName`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    cost: parseArray(dto.cost, `${path}.cost`, toCost),
    affordable: parseBoolean(dto.affordable, `${path}.affordable`),
    item: detailFor(dto.item, kind === "item", `${path}.item`, toItemDetail),
    mercenary: detailFor(
      dto.mercenary,
      kind === "mercenary",
      `${path}.mercenary`,
      toMercenaryDetail,
    ),
  }
}

export const HonourShopConverter = {
  toVo(value: unknown, path = "honour"): HonourShopVo {
    const dto = parseObject(value, path)
    return HonourShopVo.create({
      medals: parseArray(dto.medals, `${path}.medals`, toMedal),
      exchanges: parseArray(dto.exchanges, `${path}.exchanges`, toExchange),
      hiredMercenaryName: parseNullableString(dto.hiredMercenaryName, `${path}.hiredMercenaryName`),
    })
  },
}
