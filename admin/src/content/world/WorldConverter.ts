import type { components } from "../../api/schema"
import { parseArray, parseBoolean, parseNumber, parseObject, parseString } from "../shared/parse"
import { AreaVo, PlaceVo } from "./WorldVo"

export type AreaDto = components["schemas"]["AreaRecord"]

export type PlaceDto = components["schemas"]["PlaceRecord"]

export const AreaConverter = {
  toVo(dto: AreaDto, path = "area"): AreaVo {
    return AreaVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      placeIds: parseArray(dto.placeIds, `${path}.placeIds`, parseNumber),
    })
  },

  toDto(vo: AreaVo): AreaDto {
    return { id: vo.id, name: vo.name, placeIds: [...vo.placeIds] }
  },
}

export const PlaceConverter = {
  toVo(dto: PlaceDto, path = "place"): PlaceVo {
    const recommendedPath = `${path}.recommendedLevel`
    const recommended = parseObject(dto.recommendedLevel, recommendedPath)
    return PlaceVo.create({
      id: parseNumber(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      subPlaceNames: parseArray(dto.subPlaceNames, `${path}.subPlaceNames`, parseString),
      recommendedMinimum: parseNumber(recommended.minimum, `${recommendedPath}.minimum`),
      recommendedMaximum: parseNumber(recommended.maximum, `${recommendedPath}.maximum`),
      restricted: parseBoolean(dto.restricted, `${path}.restricted`),
    })
  },

  toDto(vo: PlaceVo): PlaceDto {
    return {
      id: vo.id,
      name: vo.name,
      subPlaceNames: [...vo.subPlaceNames],
      recommendedLevel: { minimum: vo.recommendedMinimum, maximum: vo.recommendedMaximum },
      restricted: vo.restricted,
    }
  },
}
