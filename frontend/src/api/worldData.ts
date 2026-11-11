import { recommendations, WorldAreaVo, type WorldPlace } from "../game/world"
import {
  parseArray,
  parseBoolean,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
  parseTimestamp,
} from "./parse"

const toPlace = (value: unknown, path: string): WorldPlace => {
  const dto = parseObject(value, path)
  const recommended = parseObject(dto.recommended, `${path}.recommended`)
  return {
    id: parseNumber(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    subPlaces: parseArray(dto.subPlaces, `${path}.subPlaces`, (item, itemPath) => {
      const subPlace = parseObject(item, itemPath)
      return {
        index: parseNumber(subPlace.index, `${itemPath}.index`),
        name: parseString(subPlace.name, `${itemPath}.name`),
      }
    }),
    recommended: {
      minimum: parseNumber(recommended.minimum, `${path}.recommended.minimum`),
      maximum: parseNumber(recommended.maximum, `${path}.recommended.maximum`),
    },
    restricted: parseBoolean(dto.restricted, `${path}.restricted`),
    current: parseBoolean(dto.current, `${path}.current`),
    recommendation: parseOneOf(dto.recommendation, recommendations, `${path}.recommendation`),
    keys: parseNumber(dto.keys, `${path}.keys`),
    passExpiresAt:
      dto.passExpiresAt === null
        ? null
        : parseTimestamp(dto.passExpiresAt, `${path}.passExpiresAt`),
  }
}

export const WorldConverter = {
  toVo(value: unknown, path = "area"): WorldAreaVo {
    const dto = parseObject(value, path)
    return WorldAreaVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      places: parseArray(dto.places, `${path}.places`, toPlace),
    })
  },
}
