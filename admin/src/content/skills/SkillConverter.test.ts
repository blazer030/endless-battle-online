import { describe, expect, it } from "vitest"
import { ContentParseError } from "../shared/parse"
import { SkillConverter, type SkillDto } from "./SkillConverter"
import { skillDto, skillLevel } from "./testData"

const convert = (dto: unknown) => SkillConverter.toVo(dto as SkillDto, "skills[0]")

describe("SkillConverter", () => {
  it("keeps every field through toVo and toDto", () => {
    const dto = skillDto({ prerequisite: { skillId: "skill-9", level: 3 } })

    expect(SkillConverter.toDto(convert(dto))).toEqual(dto)
  })

  it("reports the path of a missing learning cost", () => {
    const level: Partial<ReturnType<typeof skillLevel>> = skillLevel(1)
    delete level.learningCost

    expect(() => convert(skillDto({ levels: [level as never] }))).toThrow(ContentParseError)
    expect(() => convert(skillDto({ levels: [level as never] }))).toThrow(
      "skills[0].levels[0].learningCost",
    )
  })

  it.each([
    [
      "condition kind",
      { conditions: [{ kind: "Luck" }] },
      "skills[0].levels[0].conditions[0].kind",
    ],
    ["cost kind", { costs: [{ kind: "Gold" }] }, "skills[0].levels[0].costs[0].kind"],
    [
      "effect kind",
      { effects: [{ kind: "Teleport", rounds: 1 }] },
      "skills[0].levels[0].effects[0].kind",
    ],
    [
      "effect value",
      { effects: [{ kind: "Silence", rounds: 1 }] },
      "skills[0].levels[0].effects[0].chance",
    ],
    [
      "stat",
      { conditions: [{ kind: "StatAbove", stat: "Luck" }] },
      "skills[0].levels[0].conditions[0].stat",
    ],
    [
      "scaling",
      { effects: [{ kind: "DamageOverTime", scaling: [{ basis: "Level" }], rounds: 1 }] },
      "skills[0].levels[0].effects[0].scaling[0].ratio",
    ],
  ])("reports the path of a wrong %s", (_name, overrides, path) => {
    expect(() => convert(skillDto({ levels: [skillLevel(1, overrides as never)] }))).toThrow(path)
  })

  it("ignores fields it does not know", () => {
    const vo = convert({ ...skillDto(), rarity: 3 })

    expect("rarity" in vo).toBe(false)
  })
})
