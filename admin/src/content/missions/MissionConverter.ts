import type { components } from "../../api/schema"
import { parseBoolean, parseString } from "../shared/parse"
import { MissionVo } from "./MissionVo"

export type MissionDto = components["schemas"]["MissionSummaryRecord"]

export const MissionConverter = {
  toVo(dto: MissionDto, path = "mission"): MissionVo {
    return MissionVo.create({
      id: parseString(dto.id, `${path}.id`),
      title: parseString(dto.title, `${path}.title`),
      daily: parseBoolean(dto.daily, `${path}.daily`),
    })
  },

  toDto(vo: MissionVo): MissionDto {
    return { id: vo.id, title: vo.title, daily: vo.daily }
  },
}
