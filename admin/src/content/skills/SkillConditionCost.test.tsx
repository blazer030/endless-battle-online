import { waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { chooseOption, findOption, visibleOptions } from "../../test/antd"
import { lastSkillPut, openSkillEdit, renderSkillPage, saveSkill } from "./skillTestPage"
import { skillDto, skillLevel } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const skillWith = (overrides: Parameters<typeof skillLevel>[1]) => [
  skillDto({ maxLevel: 1, levels: [skillLevel(1, overrides)] }),
]

describe("skill level conditions", () => {
  it("shows only the percent for Chance", async () => {
    renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    expect(within(dialog).getByLabelText("條件 1 百分比")).toHaveValue("40")
    expect(within(dialog).queryByRole("combobox", { name: "條件 1 能力" })).not.toBeInTheDocument()
  })

  it("shows only the stat for StatAbove and sends it", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await chooseOption(
      within(dialog).getByRole("combobox", { name: "條件 1 種類" }),
      "能力比對手高",
    )

    expect(within(dialog).queryByLabelText("條件 1 百分比")).not.toBeInTheDocument()
    await chooseOption(within(dialog).getByRole("combobox", { name: "條件 1 能力" }), "智力")
    await saveSkill(dialog, calls)
    expect(lastSkillPut(calls).levels[0].conditions).toEqual([
      { kind: "StatAbove", stat: "Intelligence" },
    ])
  })

  it("has no fields for HitPointsBelowOpponent", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "條件 1 種類" }), "HP 比對手低")

    expect(within(dialog).queryByLabelText("條件 1 百分比")).not.toBeInTheDocument()
    expect(within(dialog).queryByRole("combobox", { name: "條件 1 能力" })).not.toBeInTheDocument()
    await saveSkill(dialog, calls)
    expect(lastSkillPut(calls).levels[0].conditions).toEqual([{ kind: "HitPointsBelowOpponent" }])
  })

  it("sends an empty list after removing every condition", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "刪除條件 1" }))
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].conditions).toEqual([])
  })

  it("adds a condition", async () => {
    const calls = renderSkillPage(skillWith({ conditions: [] }))
    const dialog = await openSkillEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "新增條件" }))
    const percent = within(dialog).getByLabelText("條件 1 百分比")
    await userEvent.clear(percent)
    await userEvent.type(percent, "30")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].conditions).toEqual([{ kind: "Chance", percent: 30 }])
  })
})

describe("skill level costs", () => {
  it("keeps existing costs unchanged", async () => {
    const costs = [
      { kind: "SkillPoints", all: true },
      { kind: "HitPoints", percent: 12.5 },
      { kind: "Money", amount: 300 },
    ]
    const calls = renderSkillPage(skillWith({ costs: costs as never }))
    const dialog = await openSkillEdit()

    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].costs).toEqual(costs)
  })

  it("has only a fixed amount for Money", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "消耗 1 種類" }), "金錢")

    expect(within(dialog).queryByRole("combobox", { name: "消耗 1 方式" })).not.toBeInTheDocument()
    const amount = within(dialog).getByLabelText("消耗 1 固定值")
    await userEvent.clear(amount)
    await userEvent.type(amount, "500")
    await saveSkill(dialog, calls)
    expect(lastSkillPut(calls).levels[0].costs).toEqual([{ kind: "Money", amount: 500 }])
  })

  it("sends all true and no amount when SkillPoints uses everything", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "消耗 1 方式" }), "全部")

    expect(within(dialog).queryByLabelText("消耗 1 固定值")).not.toBeInTheDocument()
    await saveSkill(dialog, calls)
    expect(lastSkillPut(calls).levels[0].costs).toEqual([{ kind: "SkillPoints", all: true }])
  })

  it("does not offer all for HitPoints and sends the percent", async () => {
    const calls = renderSkillPage(skillWith({ costs: [{ kind: "HitPoints", amount: 5 }] as never }))
    const dialog = await openSkillEdit()
    const mode = within(dialog).getByRole("combobox", { name: "消耗 1 方式" })

    await userEvent.click(mode)
    await waitFor(() => expect(visibleOptions()).toEqual(["固定值", "百分比"]))
    await userEvent.click(await findOption("百分比"))
    const percent = within(dialog).getByLabelText("消耗 1 百分比")
    await userEvent.clear(percent)
    await userEvent.type(percent, "12.5")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].costs).toEqual([{ kind: "HitPoints", percent: 12.5 }])
  })

  it("adds and removes a cost", async () => {
    const calls = renderSkillPage(skillWith({}))
    const dialog = await openSkillEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "新增消耗" }))
    await userEvent.click(within(dialog).getByRole("button", { name: "刪除消耗 1" }))
    const amount = within(dialog).getByLabelText("消耗 1 固定值")
    await userEvent.clear(amount)
    await userEvent.type(amount, "7")
    await saveSkill(dialog, calls)

    expect(lastSkillPut(calls).levels[0].costs).toEqual([{ kind: "SkillPoints", amount: 7 }])
  })
})
