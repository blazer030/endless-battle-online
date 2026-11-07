import { screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { chooseOption, visibleOptions } from "../../test/antd"
import { lastSkillPut, openSkillEdit, renderSkillPage, saveSkill } from "../../test/skillTestPage"
import { skillDto, skillLevel, skills } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

// 每一級都掛載著，只顯示目前這一級；查欄位時要限定範圍
const activeLevel = (dialog: HTMLElement) => within(dialog).getByTestId("active-level")

const renderSkills = (list: unknown = skills) => renderSkillPage(list)

const openEdit = openSkillEdit

const lastPut = lastSkillPut

const saveAndWait = saveSkill

describe("SkillListPage", () => {
  it("shows job names and filters by job and slot type", async () => {
    renderSkills()
    const both = await screen.findByRole("row", { name: /skill-3/ })
    expect(within(both).getByText("戰士、術士")).toBeInTheDocument()

    await chooseOption(screen.getByRole("combobox", { name: "職業" }), "術士")
    expect(screen.queryByRole("row", { name: /skill-1\b/ })).not.toBeInTheDocument()
    expect(screen.getByRole("row", { name: /skill-2/ })).toBeInTheDocument()
    expect(screen.getByRole("row", { name: /skill-3/ })).toBeInTheDocument()

    await chooseOption(screen.getByRole("combobox", { name: "欄位" }), "被動")
    expect(screen.getByRole("row", { name: /skill-2/ })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /skill-3/ })).not.toBeInTheDocument()
  })
})

describe("SkillFormModal basics", () => {
  it("adds a level that starts with the previous level's values when the maximum rises", async () => {
    const levels = Array.from({ length: 10 }, (_, index) => skillLevel(index + 1))
    const calls = renderSkills([skillDto({ maxLevel: 10, levels })])
    const dialog = await openEdit()
    const maxLevel = within(dialog).getByLabelText("等級上限")

    await userEvent.clear(maxLevel)
    await userEvent.type(maxLevel, "11")
    await userEvent.tab()
    await userEvent.click(within(dialog).getByText("第 11 級"))

    expect(within(activeLevel(dialog)).getByLabelText("學習費用")).toHaveValue("1000")
    await saveAndWait(dialog, calls)
    const body = lastPut(calls)
    expect(body.maxLevel).toBe(11)
    expect(body.levels).toHaveLength(11)
    expect(body.levels[10]).toEqual({ ...skillLevel(10), level: 11 })
  })

  it("removes the last levels when the maximum drops", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()
    const maxLevel = within(dialog).getByLabelText("等級上限")

    await userEvent.clear(maxLevel)
    await userEvent.type(maxLevel, "1")
    await saveAndWait(dialog, calls)

    expect(lastPut(calls).maxLevel).toBe(1)
    expect(lastPut(calls).levels).toEqual([skillLevel(1)])
  })

  it("edits each level's learning cost, proficiency and cooldown", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByText("第 2 級"))
    const cost = within(activeLevel(dialog)).getByLabelText("學習費用")
    await userEvent.clear(cost)
    await userEvent.type(cost, "999")
    const cooldown = within(activeLevel(dialog)).getByLabelText("冷卻")
    await userEvent.clear(cooldown)
    await userEvent.type(cooldown, "4")
    await saveAndWait(dialog, calls)

    expect(lastPut(calls).levels[0]).toEqual(skillLevel(1))
    expect(lastPut(calls).levels[1]).toEqual({ ...skillLevel(2), learningCost: 999, cooldown: 4 })
  })

  it("does not offer the skill itself as its prerequisite", async () => {
    renderSkills()
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("combobox", { name: "前置技能" }))

    await waitFor(() => expect(visibleOptions()).toContain("鐵壁"))
    expect(visibleOptions()).toContain("無")
    expect(visibleOptions()).not.toContain("爆氣")
  })

  it("sends the chosen prerequisite with its level and the jobs", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "前置技能" }), "鐵壁")
    await userEvent.type(within(dialog).getByLabelText("前置技能等級"), "3")
    await chooseOption(within(dialog).getByRole("combobox", { name: "能學的職業" }), "術士")
    await saveAndWait(dialog, calls)

    expect(lastPut(calls).prerequisite).toEqual({ skillId: "skill-2", level: 3 })
    expect(lastPut(calls).jobIds).toEqual(["job-6", "job-7"])
  })

  it("edits the name, slot type, timing and description", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()

    const name = within(dialog).getByLabelText("名稱")
    await userEvent.clear(name)
    await userEvent.type(name, "狂氣")
    await chooseOption(within(dialog).getByRole("combobox", { name: "欄位" }), "被動")
    await chooseOption(within(dialog).getByRole("combobox", { name: "時機" }), "命中後")
    await saveAndWait(dialog, calls)

    expect(lastPut(calls)).toMatchObject({ name: "狂氣", slotType: "Passive", timing: "AfterHit" })
  })
})

describe("SkillFormModal validation across levels", () => {
  it("does not save a blank effect value of another level as zero", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()
    await userEvent.clear(within(activeLevel(dialog)).getByLabelText("效果 1 百分比"))

    await userEvent.click(within(dialog).getByText("第 2 級"))
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() =>
      expect(within(dialog).getByRole("radio", { name: "第 1 級" })).toBeChecked(),
    )
    expect(await within(activeLevel(dialog)).findByText("請填寫百分比")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })

  it("does not save a blank learning cost of another level", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()
    await userEvent.click(within(dialog).getByText("第 2 級"))
    await userEvent.clear(within(activeLevel(dialog)).getByLabelText("學習費用"))

    await userEvent.click(within(dialog).getByText("第 1 級"))
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() =>
      expect(within(dialog).getByRole("radio", { name: "第 2 級" })).toBeChecked(),
    )
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })

  it("keeps the values of the other levels when saving", async () => {
    const calls = renderSkills()
    const dialog = await openEdit()
    await userEvent.click(within(dialog).getByText("第 2 級"))
    await chooseOption(
      within(activeLevel(dialog)).getByRole("combobox", { name: "效果 1 種類" }),
      "封鎖",
    )

    await saveAndWait(dialog, calls)

    expect(lastPut(calls).levels[0]).toEqual(skillLevel(1))
    expect(lastPut(calls).levels[1].effects).toEqual([{ kind: "Silence", chance: 100, rounds: 3 }])
  })
})
