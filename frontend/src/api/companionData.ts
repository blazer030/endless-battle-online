import {
  honourExchangeKinds,
  HonourShopVo,
  type HonourCost,
  type HonourExchange,
  type HonourMedal,
} from "../game/honour"
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
import {
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

const toExchange = (value: unknown, path: string): HonourExchange => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    kind: parseOneOf(dto.kind, honourExchangeKinds, `${path}.kind`),
    rewardId: parseString(dto.rewardId, `${path}.rewardId`),
    rewardName: parseString(dto.rewardName, `${path}.rewardName`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    cost: parseArray(dto.cost, `${path}.cost`, toCost),
    affordable: parseBoolean(dto.affordable, `${path}.affordable`),
  }
}

export const HonourShopConverter = {
  toVo(value: unknown, path = "honour"): HonourShopVo {
    const dto = parseObject(value, path)
    return HonourShopVo.create({
      medals: parseArray(dto.medals, `${path}.medals`, toMedal),
      exchanges: parseArray(dto.exchanges, `${path}.exchanges`, toExchange),
    })
  },
}
