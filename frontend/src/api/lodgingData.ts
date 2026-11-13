import { BuffVo, buffKinds } from "../game/buffs"
import { InnBookVo, InnVo, type InnRoom, type Lodging } from "../game/inn"
import { CharacterConverter } from "./characterData"
import {
  parseArray,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
  parseTimestamp,
} from "./parse"

// 道具使用回應裡的 buff 也用這個轉換
export const BuffConverter = {
  toVo(value: unknown, path = "buffs"): BuffVo {
    const dto = parseObject(value, path)
    return BuffVo.create({
      kind: parseOneOf(dto.kind, buffKinds, `${path}.kind`),
      multiplier: parseNumber(dto.multiplier, `${path}.multiplier`),
      startsAt: parseTimestamp(dto.startsAt, `${path}.startsAt`),
      endsAt: parseTimestamp(dto.endsAt, `${path}.endsAt`),
      itemId: parseNullableString(dto.itemId, `${path}.itemId`),
      itemName: parseNullableString(dto.itemName, `${path}.itemName`),
    })
  },
}

const toRoom = (value: unknown, path: string): InnRoom => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    kind: parseOneOf(dto.kind, buffKinds, `${path}.kind`),
    multiplier: parseNumber(dto.multiplier, `${path}.multiplier`),
    seconds: parseNumber(dto.seconds, `${path}.seconds`),
    price: parseNumber(dto.price, `${path}.price`),
  }
}

const toLodging = (value: unknown, path: string): Lodging => {
  const dto = parseObject(value, path)
  return {
    roomId: parseString(dto.roomId, `${path}.roomId`),
    startsAt: parseTimestamp(dto.startsAt, `${path}.startsAt`),
    endsAt: parseTimestamp(dto.endsAt, `${path}.endsAt`),
  }
}

export const InnConverter = {
  toVo(value: unknown, path = "inn"): InnVo {
    const dto = parseObject(value, path)
    return InnVo.create({
      rooms: parseArray(dto.rooms, `${path}.rooms`, toRoom),
      lodging: dto.lodging === null ? null : toLodging(dto.lodging, `${path}.lodging`),
      restFee: parseNumber(dto.restFee, `${path}.restFee`),
    })
  },
}

export const InnBookConverter = {
  toVo(value: unknown, path = "innBooking"): InnBookVo {
    const dto = parseObject(value, path)
    return InnBookVo.create({
      character: CharacterConverter.toVo(dto.character, `${path}.character`),
      lodging: toLodging(dto.lodging, `${path}.lodging`),
      cost: parseNumber(dto.cost, `${path}.cost`),
    })
  },
}
