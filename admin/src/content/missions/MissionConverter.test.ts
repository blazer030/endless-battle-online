import { describe, expect, it } from "vitest"
import { ContentParseError } from "../shared/parse"
import { MissionConverter, type MissionDto } from "./MissionConverter"

describe("MissionConverter", () => {
  it("reports the path of a missing title", () => {
    const dto = { id: "mission-2", daily: false } as MissionDto

    expect(() => MissionConverter.toVo(dto, "missions[0]")).toThrow(ContentParseError)
    expect(() => MissionConverter.toVo(dto, "missions[0]")).toThrow("missions[0].title")
  })

  it("keeps every field through toVo and toDto", () => {
    const dto: MissionDto = { id: "mission-2", title: "打敗米列", daily: true }

    expect(MissionConverter.toDto(MissionConverter.toVo(dto))).toEqual(dto)
  })
})
