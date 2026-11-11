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

const buffDto = { kind: "Experience", multiplier: 1.5, endsAt: "2026-11-12T08:30:00Z" }

// 規格 Buff end time becomes a date
test("BuffConverter_ConvertsTheEndTimeToADate", () => {
  const buff = BuffConverter.toVo(buffDto)

  expect(buff).toBeInstanceOf(BuffVo)
  expect(buff.kind).toBe("Experience")
  expect(buff.multiplier).toBe(1.5)
  expect(buff.endsAt).toEqual(new Date(Date.UTC(2026, 10, 12, 8, 30)))
})

test("BuffConverter_MissingMultiplierNamesThePath", () => {
  expect(
    parseFailure(() => BuffConverter.toVo({ kind: "Attack", endsAt: buffDto.endsAt }, "buffs[1]"))
      .path,
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
  lodging: { roomId: "apprentice-loft", endsAt: "2026-11-12T16:00:00Z" },
}

test("InnConverter_ConvertsTheWholeResponse", () => {
  const inn = InnConverter.toVo(innDto)

  expect(inn).toBeInstanceOf(InnVo)
  expect(inn.rooms).toEqual(innDto.rooms)
  expect(inn.lodging).toEqual({
    roomId: "apprentice-loft",
    endsAt: new Date(Date.UTC(2026, 10, 12, 16)),
  })
  expect(InnConverter.toVo({ ...innDto, lodging: null }).lodging).toBeNull()
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
