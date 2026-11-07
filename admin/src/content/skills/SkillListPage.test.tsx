import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../../App"
import { chooseOption, visibleOptions } from "../../test/antd"
import { mockFetch, type FetchCall } from "../../test/fetchMock"
import { jobs } from "../jobs/testData"
import type { SkillDto } from "./SkillConverter"
import { skillDto, skillLevel, skills } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const savedStatus = { exists: true, valid: true, error: null }

function renderSkills(list: unknown = skills): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/skills": list,
    "GET /admin/content/jobs": jobs,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "PUT /admin/content/skills/skill-1": savedStatus,
  })
  render(
    <MemoryRouter initialEntries={["/skills"]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

async function openEdit(id = "skill-1") {
  const row = await screen.findByRole("row", { name: new RegExp(`${id}\\b`) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

const lastPut = (calls: FetchCall[]) =>
  calls.filter((call) => call.method === "PUT").at(-1)?.body as SkillDto

const saveAndWait = async (dialog: HTMLElement, calls: FetchCall[]) => {
  await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))
  await waitFor(() => expect(lastPut(calls)).toBeDefined())
}

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

    expect(within(dialog).getByLabelText("學習費用")).toHaveValue("1000")
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
    const cost = within(dialog).getByLabelText("學習費用")
    await userEvent.clear(cost)
    await userEvent.type(cost, "999")
    const cooldown = within(dialog).getByLabelText("冷卻")
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
