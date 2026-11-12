import { expect, test } from "vitest"
import { HonourShopVo } from "../game/honour"
import { MercenaryHireVo, MercenaryOverviewVo } from "../game/mercenaries"
import { PetFeedVo, PetsVo } from "../game/pets"
import {
  makeCharacter,
  makeHonourShop,
  makeMercenaryOverview,
  makePet,
  makePets,
} from "../test/fixtures"
import {
  HonourShopConverter,
  MercenaryHireConverter,
  MercenaryOverviewConverter,
  PetFeedConverter,
  PetsConverter,
} from "./companionData"
import { ParseError } from "./parse"

const asDto = (value: unknown): Record<string, unknown> => JSON.parse(JSON.stringify(value))

const parseFailure = (convert: () => unknown) => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

const pets = makePets([makePet({ id: 3, nextFeedAt: new Date("2026-11-12T08:30:00Z") })])
const petsDto = asDto(pets)

test("PetsConverter_ConvertsTheWholeResponse", () => {
  const converted = PetsConverter.toVo(petsDto)

  expect(converted).toBeInstanceOf(PetsVo)
  expect(converted).toEqual(pets)
})

test("PetsConverter_ConvertsTheNextFeedTimeToADate", () => {
  expect(PetsConverter.toVo(petsDto).pets[0].nextFeedAt).toEqual(
    new Date(Date.UTC(2026, 10, 12, 8, 30)),
  )
})

test("PetsConverter_RejectsAnUnknownPersonality", () => {
  const dto = asDto(petsDto)
  ;(dto.pets as Record<string, unknown>[])[0].personality = "Lazy"

  expect(parseFailure(() => PetsConverter.toVo(dto)).path).toBe("pets.pets[0].personality")
})

test("PetsConverter_MissingNextFeedTimeNamesThePath", () => {
  const dto = asDto(petsDto)
  delete (dto.pets as Record<string, unknown>[])[0].nextFeedAt

  expect(parseFailure(() => PetsConverter.toVo(dto)).path).toBe("pets.pets[0].nextFeedAt")
})

test("PetsConverter_IgnoresUnknownFields", () => {
  expect(PetsConverter.toVo({ ...petsDto, barn: "east" })).not.toHaveProperty("barn")
})

const feedDto = asDto({ pets, character: makeCharacter({ money: 980 }) })

test("PetFeedConverter_ConvertsPetsAndCharacter", () => {
  const converted = PetFeedConverter.toVo(feedDto)

  expect(converted).toBeInstanceOf(PetFeedVo)
  expect(converted.pets).toEqual(pets)
  expect(converted.character.money).toBe(980)
})

test("PetFeedConverter_MissingCharacterNamesThePath", () => {
  expect(parseFailure(() => PetFeedConverter.toVo({ pets: petsDto })).path).toBe(
    "petFeed.character",
  )
})

test("PetFeedConverter_IgnoresUnknownFields", () => {
  expect(PetFeedConverter.toVo({ ...feedDto, crumbs: 1 })).not.toHaveProperty("crumbs")
})

const overview = makeMercenaryOverview({
  contract: {
    mercenaryId: "mercenary-f",
    name: "F級傭兵",
    remaining: 99,
    placeId: 1,
    placeName: "中央平原",
    fighting: true,
    nextFightAt: new Date("2026-11-12T08:31:40Z"),
  },
})
const overviewDto = asDto(overview)

test("MercenaryOverviewConverter_ConvertsTheWholeResponse", () => {
  const converted = MercenaryOverviewConverter.toVo(overviewDto)

  expect(converted).toBeInstanceOf(MercenaryOverviewVo)
  expect(converted).toEqual(overview)
  expect(converted.contract?.nextFightAt).toEqual(new Date(Date.UTC(2026, 10, 12, 8, 31, 40)))
})

test("MercenaryOverviewConverter_KeepsAMissingContractOrFightAsNull", () => {
  const contract = overviewDto.contract as Record<string, unknown>

  expect(MercenaryOverviewConverter.toVo({ ...overviewDto, contract: null }).contract).toBeNull()
  expect(
    MercenaryOverviewConverter.toVo({
      ...overviewDto,
      contract: { ...contract, nextFightAt: null },
    }).contract?.nextFightAt,
  ).toBeNull()
})

test("MercenaryOverviewConverter_MissingExperiencePercentNamesThePath", () => {
  const dto = asDto(overviewDto)
  delete (dto.catalogue as Record<string, unknown>[])[1].experiencePercent

  expect(parseFailure(() => MercenaryOverviewConverter.toVo(dto)).path).toBe(
    "mercenaries.catalogue[1].experiencePercent",
  )
})

test("MercenaryOverviewConverter_IgnoresUnknownFields", () => {
  expect(MercenaryOverviewConverter.toVo({ ...overviewDto, guild: "north" })).not.toHaveProperty(
    "guild",
  )
})

const hireDto = asDto({ mercenaries: overview, character: makeCharacter({ money: 0 }) })

test("MercenaryHireConverter_ConvertsOverviewAndCharacter", () => {
  const converted = MercenaryHireConverter.toVo(hireDto)

  expect(converted).toBeInstanceOf(MercenaryHireVo)
  expect(converted.mercenaries).toEqual(overview)
  expect(converted.character.money).toBe(0)
})

test("MercenaryHireConverter_MissingOverviewNamesThePath", () => {
  expect(
    parseFailure(() => MercenaryHireConverter.toVo({ character: hireDto.character })).path,
  ).toBe("mercenaryHire.mercenaries")
})

test("MercenaryHireConverter_IgnoresUnknownFields", () => {
  expect(MercenaryHireConverter.toVo({ ...hireDto, receipt: 1 })).not.toHaveProperty("receipt")
})

const honour = makeHonourShop()
const honourDto = asDto(honour)

test("HonourShopConverter_ConvertsTheWholeResponse", () => {
  const converted = HonourShopConverter.toVo(honourDto)

  expect(converted).toBeInstanceOf(HonourShopVo)
  expect(converted).toEqual(honour)
})

test("HonourShopConverter_RejectsAnUnknownExchangeKind", () => {
  const dto = asDto(honourDto)
  ;(dto.exchanges as Record<string, unknown>[])[0].kind = "pet"

  expect(parseFailure(() => HonourShopConverter.toVo(dto)).path).toBe("honour.exchanges[0].kind")
})

test("HonourShopConverter_MissingOwnedCountNamesThePath", () => {
  const dto = asDto(honourDto)
  delete (dto.medals as Record<string, unknown>[])[1].owned

  expect(parseFailure(() => HonourShopConverter.toVo(dto)).path).toBe("honour.medals[1].owned")
})

test("HonourShopConverter_IgnoresUnknownFields", () => {
  expect(HonourShopConverter.toVo({ ...honourDto, banner: "x" })).not.toHaveProperty("banner")
})
