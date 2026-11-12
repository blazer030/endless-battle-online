import { expect, test } from "vitest"
import { BuffVo } from "../game/buffs"
import { InnBookVo, InnVo } from "../game/inn"
import { makeCharacter } from "../test/fixtures"
import { BuffConverter, InnBookConverter, InnConverter } from "./lodgingData"
import { ParseError } from "./parse"

const parseFailure = (convert: () => unknown) => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

const buffDto = {
  kind: "Experience",
  multiplier: 1.5,
  startsAt: "2026-11-12T06:30:00Z",
  endsAt: "2026-11-12T08:30:00Z",
  itemId: "item-2255",
  itemName: "戰鬥熟練手冊LV1",
}

// 規格 Buff end time becomes a date
test("BuffConverter_ConvertsTheEndTimeToADate", () => {
  const buff = BuffConverter.toVo(buffDto)

  expect(buff).toBeInstanceOf(BuffVo)
  expect(buff.kind).toBe("Experience")
  expect(buff.multiplier).toBe(1.5)
  expect(buff.endsAt).toEqual(new Date(Date.UTC(2026, 10, 12, 8, 30)))
})

test("BuffConverter_ConvertsTheStartAndItem", () => {
  const buff = BuffConverter.toVo(buffDto)

  expect(buff.startsAt).toEqual(new Date(Date.UTC(2026, 10, 12, 6, 30)))
  expect(buff.itemId).toBe("item-2255")
  expect(buff.itemName).toBe("戰鬥熟練手冊LV1")
})

test("BuffConverter_KeepsAMissingItemAsNull", () => {
  const buff = BuffConverter.toVo({ ...buffDto, itemId: null, itemName: null })

  expect(buff.itemId).toBeNull()
  expect(buff.itemName).toBeNull()
})

test("BuffConverter_MissingStartNamesThePath", () => {
  expect(parseFailure(() => BuffConverter.toVo({ ...buffDto, startsAt: undefined })).path).toBe(
    "buffs.startsAt",
  )
})

test("BuffConverter_MissingMultiplierNamesThePath", () => {
  expect(
    parseFailure(() => BuffConverter.toVo({ ...buffDto, multiplier: undefined }, "buffs[1]")).path,
  ).toBe("buffs[1].multiplier")
})

test("BuffConverter_RejectsTheOldExperienceCode", () => {
  expect(parseFailure(() => BuffConverter.toVo({ ...buffDto, kind: "Exp" })).path).toBe(
    "buffs.kind",
  )
})

test("BuffConverter_IgnoresUnknownFields", () => {
  expect(BuffConverter.toVo({ ...buffDto, icon: "x" })).not.toHaveProperty("icon")
})

const innDto = {
  rooms: [
    {
      id: "apprentice-loft",
      name: "學徒閣樓",
      kind: "Experience",
      multiplier: 1.1,
      seconds: 28800,
      price: 100,
    },
  ],
  lodging: {
    roomId: "apprentice-loft",
    startsAt: "2026-11-12T08:00:00Z",
    endsAt: "2026-11-12T16:00:00Z",
  },
}

test("InnConverter_ConvertsTheWholeResponse", () => {
  const inn = InnConverter.toVo(innDto)

  expect(inn).toBeInstanceOf(InnVo)
  expect(inn.rooms).toEqual(innDto.rooms)
  expect(inn.lodging).toEqual({
    roomId: "apprentice-loft",
    startsAt: new Date(Date.UTC(2026, 10, 12, 8)),
    endsAt: new Date(Date.UTC(2026, 10, 12, 16)),
  })
  expect(InnConverter.toVo({ ...innDto, lodging: null }).lodging).toBeNull()
})

test("InnConverter_ConvertsTheLodgingStart", () => {
  expect(InnConverter.toVo(innDto).lodging?.startsAt).toEqual(new Date(Date.UTC(2026, 10, 12, 8)))
  expect(
    parseFailure(() =>
      InnConverter.toVo({ ...innDto, lodging: { ...innDto.lodging, startsAt: undefined } }),
    ).path,
  ).toBe("inn.lodging.startsAt")
})

test("InnConverter_MissingRoomPriceNamesThePath", () => {
  const rooms = [{ ...innDto.rooms[0], price: undefined }]

  expect(parseFailure(() => InnConverter.toVo({ ...innDto, rooms })).path).toBe(
    "inn.rooms[0].price",
  )
})

test("InnConverter_IgnoresUnknownFields", () => {
  expect(InnConverter.toVo({ ...innDto, keeper: "瑪莎" })).not.toHaveProperty("keeper")
})

const bookDto = JSON.parse(
  JSON.stringify({ character: makeCharacter({ money: 700 }), lodging: innDto.lodging, cost: 600 }),
)

test("InnBookConverter_ConvertsTheWholeResponse", () => {
  const booked = InnBookConverter.toVo(bookDto)

  expect(booked).toBeInstanceOf(InnBookVo)
  expect(booked.character.money).toBe(700)
  expect(booked.cost).toBe(600)
  expect(booked.lodging.endsAt).toEqual(new Date(Date.UTC(2026, 10, 12, 16)))
})

test("InnBookConverter_MissingCostNamesThePath", () => {
  expect(
    parseFailure(() =>
      InnBookConverter.toVo({ character: bookDto.character, lodging: bookDto.lodging }),
    ).path,
  ).toBe("innBooking.cost")
})

test("InnBookConverter_IgnoresUnknownFields", () => {
  expect(InnBookConverter.toVo({ ...bookDto, receipt: 1 })).not.toHaveProperty("receipt")
})
