import { describe, expect, it } from "vitest"
import { ContentParseError } from "../shared/parse"
import { MonsterConverter, type MonsterDto } from "./MonsterConverter"
import { monsterDto } from "./testData"

const convertList = (dtos: unknown[]) =>
  dtos.map((dto, index) => MonsterConverter.toVo(dto as MonsterDto, `monsters[${index}]`))

describe("MonsterConverter", () => {
  it("reports the path of a missing required field", () => {
    const withoutLevel: Partial<MonsterDto> = monsterDto()
    delete withoutLevel.level

    expect(() => convertList([withoutLevel])).toThrow(ContentParseError)
    expect(() => convertList([withoutLevel])).toThrow("monsters[0].level")
  })

  it.each([
    ["name", { name: 3 }, "monsters[0].name"],
    ["element", { element: "Lightning" }, "monsters[0].element"],
    ["drop rarity", { drops: [{ itemId: "item-204" }] }, "monsters[0].drops[0].dropRarity"],
    ["skill id", { skills: [{ level: 1, activationRate: 1 }] }, "monsters[0].skills[0].skillId"],
    ["sub-place", { subPlaceIndexes: ["1"] }, "monsters[0].subPlaceIndexes[0]"],
    ["mission", { exclusiveMissionId: undefined }, "monsters[0].exclusiveMissionId"],
    ["npc flag", { isNpc: 1 }, "monsters[0].isNpc"],
  ])("reports the path of a wrong %s", (_name, overrides, path) => {
    expect(() => convertList([{ ...monsterDto(), ...overrides }])).toThrow(path)
  })

  it("ignores fields it does not know", () => {
    const vo = MonsterConverter.toVo({ ...monsterDto(), rarity: 3 } as MonsterDto)

    expect(vo.name).toBe("史萊姆")
    expect("rarity" in vo).toBe(false)
  })

  it("keeps every field through toVo and toDto", () => {
    const dto = monsterDto({
      exclusiveMissionId: "mission-2",
      isNpc: true,
      npcMessage: "你好",
      subPlaceIndexes: [1, 3],
    })

    expect(MonsterConverter.toDto(MonsterConverter.toVo(dto))).toEqual(dto)
  })
})
