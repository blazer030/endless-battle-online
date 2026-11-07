import { waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { chooseOption } from "../../test/antd"
import { readOfficialContent } from "../../test/officialContent"
import { SkillConverter, type SkillDto } from "./SkillConverter"
import { skillFromValues, skillToValues } from "./skillFormValues"
import { lastSkillPut, openSkillEdit, renderSkillPage, saveSkill } from "../../test/skillTestPage"
import { skillDto, skillLevel } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const skillWith = (effects: unknown[]) => [
  skillDto({ maxLevel: 1, levels: [skillLevel(1, { effects: effects as never })] }),
]

const statChange = { kind: "StatChange", target: "Caster", stat: "Attack", percent: 5, rounds: 3 }

const effectFields = (dialog: HTMLElement) => [
  ...new Set(
    [...dialog.querySelectorAll('[aria-label^="效果 1 "]')].map((element) =>
      element.getAttribute("aria-label")!.replace("效果 1 ", ""),
    ),
  ),
]

const setNumber = async (dialog: HTMLElement, label: string, value: string) => {
  const input = within(dialog).getByLabelText(label)
  await userEvent.clear(input)
  await userEvent.type(input, value)
}

describe("skill level effects", () => {
  it.each([
    ["能力增減", ["種類", "對象", "能力", "方式", "百分比", "機率", "回合"]],
    ["追加傷害", ["種類", "攻擊方式", "基準 1", "倍率 1", "機率", "回合"]],
    ["連擊", ["種類", "攻擊方式", "連擊次數", "回合"]],
    ["減傷", ["種類", "攻擊方式", "百分比", "回合"]],
    ["回復", ["種類", "資源", "方式", "基準 1", "倍率 1", "回合"]],
    ["暈眩", ["種類", "機率", "依能力調整", "回合"]],
    ["封鎖", ["種類", "機率", "回合"]],
    ["百分比傷害", ["種類", "百分比", "回合"]],
    ["屬性剋制", ["種類", "百分比", "回合"]],
    ["持續傷害", ["種類", "基準 1", "倍率 1", "回合"]],
    ["減少 SP", ["種類", "方式", "固定值", "回合"]],
    ["攻擊屬性", ["種類", "屬性", "回合"]],
    ["防禦屬性", ["種類", "屬性", "回合"]],
  ])("shows only the fields of %s", async (kindLabel, fields) => {
    renderSkillPage(skillWith([{ kind: "Silence", chance: 10, rounds: 1 }]))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 種類" }), kindLabel)

    await waitFor(() => expect(effectFields(dialog).toSorted()).toEqual(fields.toSorted()))
  })

  it("sends only kind, chance and rounds after changing StatChange to Silence", async () => {
    const calls = renderSkillPage(skillWith([statChange]))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 種類" }), "封鎖")

    expect(effectFields(dialog).toSorted()).toEqual(["種類", "機率", "回合"].toSorted())
    await setNumber(dialog, "效果 1 機率", "25")
    await saveSkill(dialog, calls)
    expect(lastSkillPut(calls).levels[0].effects).toEqual([
      { kind: "Silence", chance: 25, rounds: 3 },
    ])
  })

  it("sends scaling and no percent after switching StatChange to scaling", async () => {
    const calls = renderSkillPage(skillWith([statChange]))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 方式" }), "依能力")
    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 基準 1" }), "智力")
    await setNumber(dialog, "效果 1 倍率 1", "1.5")
    await userEvent.click(within(dialog).getByRole("button", { name: "新增效果 1 基準" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 基準 2" }), "等級")
    await setNumber(dialog, "效果 1 倍率 2", "2")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].effects).toEqual([
      {
        kind: "StatChange",
        target: "Caster",
        stat: "Attack",
        scaling: [
          { basis: "Intelligence", ratio: 1.5 },
          { basis: "Level", ratio: 2 },
        ],
        rounds: 3,
      },
    ])
  })

  it("sends the stat and percent when Stun adjusts its chance by a stat", async () => {
    const calls = renderSkillPage(skillWith([{ kind: "Stun", chance: 20, rounds: 1 }]))
    const dialog = await openSkillEdit()

    await userEvent.click(within(dialog).getByRole("checkbox", { name: "效果 1 依能力調整" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 調整能力" }), "智力")
    await setNumber(dialog, "效果 1 調整百分比", "10")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].effects).toEqual([
      { kind: "Stun", chance: 20, chanceByStat: { stat: "Intelligence", percent: 10 }, rounds: 1 },
    ])
  })

  it("drops the optional chance when it is cleared", async () => {
    const calls = renderSkillPage(skillWith([{ ...statChange, chance: 50 }]))
    const dialog = await openSkillEdit()

    await userEvent.clear(within(dialog).getByLabelText("效果 1 機率"))
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].effects).toEqual([statChange])
  })

  it("adds and removes an effect", async () => {
    const calls = renderSkillPage(skillWith([statChange]))
    const dialog = await openSkillEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "新增效果" }))
    await userEvent.click(within(dialog).getByRole("button", { name: "刪除效果 1" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "效果 1 種類" }), "封鎖")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].effects).toEqual([
      { kind: "Silence", chance: 100, rounds: 0 },
    ])
  })

  it("copies the effects into a new level without sharing them with the previous one", async () => {
    const calls = renderSkillPage(skillWith([statChange]))
    const dialog = await openSkillEdit()
    const maxLevel = within(dialog).getByLabelText("等級上限")

    await userEvent.clear(maxLevel)
    await userEvent.type(maxLevel, "2")
    await userEvent.tab()
    await userEvent.click(within(dialog).getByText("第 2 級"))
    await setNumber(dialog, "效果 1 回合", "9")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].effects).toEqual([statChange])
    expect(lastSkillPut(calls).levels[1].effects).toEqual([{ ...statChange, rounds: 9 }])
  })
})

// 內容檔的欄位轉成 API 的欄位，轉換規則獨立於 Converter：等級的兩個欄位改名、能力與基準的列舉字首大寫、補上關聯表
const capitalize = (text: string) => text.charAt(0).toUpperCase() + text.slice(1)

type FilePart = Record<string, unknown> & {
  stat?: string
  scaling?: { basis: string; ratio: number }[]
  chanceByStat?: { stat: string; percent: number }
}

const partToApi = (part: FilePart) => ({
  ...part,
  ...(part.stat === undefined ? {} : { stat: capitalize(part.stat) }),
  ...(part.scaling === undefined
    ? {}
    : { scaling: part.scaling.map((row) => ({ ...row, basis: capitalize(row.basis) })) }),
  ...(part.chanceByStat === undefined
    ? {}
    : { chanceByStat: { ...part.chanceByStat, stat: capitalize(part.chanceByStat.stat) } }),
})

type FileLevel = {
  level: number
  money: number
  proficiency: number
  cooldown: number
  conditions: FilePart[]
  costs: FilePart[]
  effects: FilePart[]
}

type FileSkill = Omit<SkillDto, "levels" | "jobIds" | "prerequisite"> & {
  levels: FileLevel[]
  prerequisite?: SkillDto["prerequisite"]
}

function officialSkills(): SkillDto[] {
  const { skills } = readOfficialContent("skills.json") as { skills: FileSkill[] }
  const { jobSkills } = readOfficialContent("job-skills.json") as {
    jobSkills: { jobId: string; skillId: string }[]
  }
  return skills.map((skill) => ({
    ...skill,
    prerequisite: skill.prerequisite ?? null,
    jobIds: jobSkills.filter((row) => row.skillId === skill.id).map((row) => row.jobId),
    levels: skill.levels.map(({ money, proficiency, ...level }) => ({
      ...level,
      learningCost: money,
      proficiencyRequired: proficiency,
      conditions: level.conditions.map(partToApi),
      costs: level.costs.map(partToApi),
      effects: level.effects.map(partToApi),
    })),
  })) as unknown as SkillDto[]
}

describe("official skill content", () => {
  it("goes through the converter unchanged for all 76 skills", () => {
    const dtos = officialSkills()

    expect(dtos).toHaveLength(76)
    for (const dto of dtos) {
      expect(SkillConverter.toDto(SkillConverter.toVo(dto))).toEqual(dto)
    }
  })

  it("goes through the form values unchanged for all 76 skills", () => {
    for (const dto of officialSkills()) {
      const vo = SkillConverter.toVo(dto)

      expect(SkillConverter.toDto(skillFromValues(skillToValues(vo)))).toEqual(dto)
    }
  })
})
