import { expect, test } from "vitest"
import { CharacterConverter, CreateCharacterConverter, type CharacterDto } from "./characterData"
import { ParseError } from "./parse"

const characterDto: CharacterDto = {
  name: "墨客",
  level: 3,
  experience: 120,
  nextLevelExperience: 3300,
  money: 2000,
  bank: 50,
  healthPoints: 300,
  manaPoints: 80,
  sex: 2,
  element: 3,
  birth: 1,
  jobId: "job-6",
  jobName: "戰士",
  stats: {
    strength: 15,
    intelligence: 12,
    life: 13,
    vitality: 10,
    agility: 11,
    charisma: 10,
    faith: 10,
    attack: 23,
    magicAttack: 12,
    defense: 10,
    magicDefense: 10,
    maxHealthPoints: 375,
    maxManaPoints: 91,
  },
  baseStats: {
    strength: 14,
    intelligence: 12,
    life: 13,
    vitality: 10,
    agility: 11,
    charisma: 10,
    faith: 10,
  },
  placeId: 1,
  placeName: "中央平原",
  subPlace: null,
  subPlaceName: null,
  areaName: "中央大陸",
  jobProficiency: [{ jobId: "job-6", proficiency: 40, skillPoints: 2 }],
  settings: {
    attack: "Magic",
    potionHealthPointsPercent: 30,
    potionManaPointsPercent: -1,
    restHealthPointsPercent: 20,
    restManaPointsPercent: -1,
    escapeHealthPointsPercent: 10,
  },
}

const parseFailure = (value: unknown): ParseError => {
  try {
    CharacterConverter.toVo(value)
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("CharacterConverter_ConvertsTheWholeResponse", () => {
  const character = CharacterConverter.toVo(characterDto)

  expect(character.experience).toBe(120)
  expect(character.nextLevelExperience).toBe(3300)
  expect(character.healthPoints).toBe(300)
  expect(character.manaPoints).toBe(80)
  expect(character.subPlace).toBeNull()
  expect(character.stats.attack).toBe(23)
  expect(character.stats.maxManaPoints).toBe(91)
  expect(character.baseStats.charisma).toBe(10)
  expect(character.jobProficiency).toEqual([{ jobId: "job-6", proficiency: 40, skillPoints: 2 }])
  expect(character.settings).toEqual(characterDto.settings)
})

test("CharacterConverter_MissingStrengthNamesThePath", () => {
  const stats: Record<string, unknown> = { ...characterDto.stats }
  delete stats.strength

  expect(parseFailure({ ...characterDto, stats }).path).toBe("character.stats.strength")
})

test("CharacterConverter_RejectsAnUnknownAttack", () => {
  const settings = { ...characterDto.settings, attack: "Fire" }

  expect(parseFailure({ ...characterDto, settings }).path).toBe("character.settings.attack")
})

test("CharacterConverter_IgnoresUnknownFields", () => {
  const character = CharacterConverter.toVo({ ...characterDto, hp: 1, title: "新手" })

  expect(character).not.toHaveProperty("hp")
  expect(character).not.toHaveProperty("title")
  expect(character.healthPoints).toBe(300)
})

test("CreateCharacterConverter_SendsCompleteAllocationNames", () => {
  expect(
    CreateCharacterConverter.toDto({
      name: "墨客",
      sex: 1,
      element: 3,
      birth: 1,
      jobId: "job-6",
      allocation: { strength: 1, intelligence: 1, life: 1, agility: 7 },
    }).allocation,
  ).toEqual({ strength: 1, intelligence: 1, life: 1, agility: 7 })
})
