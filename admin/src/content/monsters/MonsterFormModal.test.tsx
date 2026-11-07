import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../../App"
import { chooseOption, visibleOptions } from "../../test/antd"
import { mockFetch, respond, type FetchCall } from "../../test/fetchMock"
import type { MonsterDto } from "./MonsterConverter"
import { itemRecords, missionRecords, monsterDto, placeRecords, skillRecords } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const savedStatus = { exists: true, valid: true, error: null }

function renderMonsters(
  routes: Record<string, unknown> = {},
  monsters: unknown = [monsterDto()],
): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/monsters": monsters,
    "GET /admin/content/places": placeRecords,
    "GET /admin/content/items": itemRecords,
    "GET /admin/content/skills": skillRecords,
    "GET /admin/content/missions": missionRecords,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "PUT /admin/content/monsters/monster-1": savedStatus,
    ...routes,
  })
  render(
    <MemoryRouter initialEntries={["/monsters"]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

async function openEdit(id = "monster-1") {
  const row = await screen.findByRole("row", { name: new RegExp(id) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

const save = (dialog: HTMLElement) =>
  userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

const lastBody = (calls: FetchCall[], method: string) =>
  calls.filter((call) => call.method === method).at(-1)?.body as MonsterDto

const setNumber = async (dialog: HTMLElement, label: string, value: string) => {
  const input = within(dialog).getByLabelText(label)
  await userEvent.clear(input)
  await userEvent.type(input, value)
}

describe("MonsterFormModal", () => {
  it("saves the new level, closes the dialog and shows it in the list", async () => {
    let saved = monsterDto()
    const calls = renderMonsters({
      "GET /admin/content/monsters": () => [saved],
      "PUT /admin/content/monsters/monster-1": (body: MonsterDto) => {
        saved = body
        return savedStatus
      },
    })
    const dialog = await openEdit()

    await setNumber(dialog, "等級", "9")
    await save(dialog)

    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument())
    expect(lastBody(calls, "PUT")).toEqual({ ...monsterDto(), level: 9 })
    const row = await screen.findByRole("row", { name: /monster-1/ })
    expect(within(row).getByText("9")).toBeInTheDocument()
  })

  it("sends the chosen place and the index of the ticked sub-place", async () => {
    const calls = renderMonsters({}, [monsterDto({ placeId: 2, subPlaceIndexes: [] })])
    const dialog = await openEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "地區" }), "中央平原")
    await userEvent.click(within(dialog).getByRole("checkbox", { name: "綠風草地" }))
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "PUT")?.placeId).toBe(1))
    expect(lastBody(calls, "PUT").subPlaceIndexes).toEqual([2])
  })

  it("clears the ticked sub-places when the place changes", async () => {
    const calls = renderMonsters({}, [monsterDto({ placeId: 1, subPlaceIndexes: [2, 3] })])
    const dialog = await openEdit()
    expect(within(dialog).getByRole("checkbox", { name: "綠風草地" })).toBeChecked()

    await chooseOption(within(dialog).getByRole("combobox", { name: "地區" }), "灼熱荒漠")

    expect(within(dialog).queryByRole("checkbox", { name: "綠風草地" })).not.toBeInTheDocument()
    expect(within(dialog).getByRole("checkbox", { name: "黃沙古道" })).not.toBeChecked()
    await save(dialog)
    await waitFor(() => expect(lastBody(calls, "PUT")?.subPlaceIndexes).toEqual([]))
  })

  it("adds a drop and an exclusive mission by name and sends their ids", async () => {
    const calls = renderMonsters({}, [monsterDto({ drops: [] })])
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "新增掉落" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "掉落道具 1" }), "錢包")
    await setNumber(dialog, "掉落難度 1", "90")
    await chooseOption(within(dialog).getByRole("combobox", { name: "限定任務" }), "打敗米列")
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "PUT")?.exclusiveMissionId).toBe("mission-2"))
    expect(lastBody(calls, "PUT").drops).toEqual([{ itemId: "item-204", dropRarity: 90 }])
  })

  it("does not offer daily missions as the exclusive mission", async () => {
    renderMonsters()
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("combobox", { name: "限定任務" }))

    await waitFor(() => expect(visibleOptions()).toContain("打敗米列"))
    expect(visibleOptions()).toContain("無")
    expect(visibleOptions()).not.toContain("[每日]治安維持任務")
  })

  it("narrows long choice lists by name or id and tells same-named items apart", async () => {
    renderMonsters({}, [monsterDto({ drops: [{ itemId: "item-1", dropRarity: 5 }] })])
    const dialog = await openEdit()
    const itemChoices = within(dialog).getByRole("combobox", { name: "掉落道具 1" })

    await userEvent.click(itemChoices)
    await userEvent.type(itemChoices, "錢包")
    await waitFor(() => expect(visibleOptions()).toEqual(["錢包"]))
    await userEvent.clear(itemChoices)
    await userEvent.type(itemChoices, "item-2")

    await waitFor(() => expect(visibleOptions()).toEqual(["錢包", "布衣（item-2）"]))
  })

  it("keeps only NPC fields and zeroes the rest when marked as an NPC", async () => {
    const calls = renderMonsters({}, [
      monsterDto({ exclusiveMissionId: "mission-2", encounterRate: 30 }),
    ])
    const dialog = await openEdit()
    expect(within(dialog).getByLabelText("攻擊")).toBeInTheDocument()

    await userEvent.click(within(dialog).getByRole("checkbox", { name: "NPC" }))

    for (const hidden of ["等級", "HP", "SP", "攻擊", "魔攻", "防禦", "魔防", "力量", "職業經驗"]) {
      expect(within(dialog).queryByLabelText(hidden)).not.toBeInTheDocument()
    }
    expect(within(dialog).queryByRole("button", { name: "新增掉落" })).not.toBeInTheDocument()
    expect(within(dialog).getByLabelText("名稱")).toBeInTheDocument()
    expect(within(dialog).getByRole("combobox", { name: "地區" })).toBeInTheDocument()
    expect(within(dialog).getByRole("combobox", { name: "限定任務" })).toBeInTheDocument()
    expect(within(dialog).getByLabelText("遇敵率")).toBeInTheDocument()
    await userEvent.type(within(dialog).getByLabelText("遭遇訊息"), "你好")
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "PUT")?.isNpc).toBe(true))
    expect(lastBody(calls, "PUT")).toEqual({
      ...monsterDto(),
      level: 0,
      hitPoints: 0,
      skillPoints: 0,
      attack: 0,
      magicAttack: 0,
      defense: 0,
      magicDefense: 0,
      strength: 0,
      intelligence: 0,
      vitality: 0,
      agility: 0,
      charisma: 0,
      faith: 0,
      element: "None",
      jobExperience: 0,
      drops: [],
      skills: [],
      exclusiveMissionId: "mission-2",
      encounterRate: 30,
      isNpc: true,
      npcMessage: "你好",
    })
  })

  it("does not send an NPC message for a monster that is not an NPC", async () => {
    const calls = renderMonsters({}, [monsterDto({ isNpc: true, npcMessage: "你好" })])
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("checkbox", { name: "NPC" }))
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "PUT")?.isNpc).toBe(false))
    expect(lastBody(calls, "PUT").npcMessage).toBeNull()
  })

  it("creates from a copy with the id cleared and keeps the other fields", async () => {
    const calls = renderMonsters({ "POST /admin/content/monsters": respond(201, savedStatus) })
    await screen.findByRole("row", { name: /monster-1/ })
    await userEvent.click(screen.getByRole("button", { name: "新增" }))
    const dialog = await screen.findByRole("dialog")

    await chooseOption(within(dialog).getByRole("combobox", { name: "複製自" }), "monster-1 史萊姆")
    expect(within(dialog).getByLabelText("id")).toHaveValue("")
    await userEvent.type(within(dialog).getByLabelText("id"), "monster-900")
    await save(dialog)

    await waitFor(() =>
      expect(lastBody(calls, "POST")).toEqual({ ...monsterDto(), id: "monster-900" }),
    )
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument())
  })

  it("creates from a blank form", async () => {
    const calls = renderMonsters({ "POST /admin/content/monsters": respond(201, savedStatus) })
    await screen.findByRole("row", { name: /monster-1/ })
    await userEvent.click(screen.getByRole("button", { name: "新增" }))
    const dialog = await screen.findByRole("dialog")

    await userEvent.type(within(dialog).getByLabelText("id"), "monster-900")
    await userEvent.type(within(dialog).getByLabelText("名稱"), "新怪")
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "POST")?.id).toBe("monster-900"))
    expect(lastBody(calls, "POST")).toMatchObject({
      name: "新怪",
      level: 0,
      placeId: 0,
      element: "None",
      drops: [],
      skills: [],
      exclusiveMissionId: null,
      isNpc: false,
      npcMessage: null,
    })
  })

  it("does not let the id change while editing", async () => {
    renderMonsters()
    const dialog = await openEdit()

    expect(within(dialog).getByLabelText("id")).toBeDisabled()
  })

  it("keeps the dialog open and shows the error when the id already exists", async () => {
    renderMonsters({
      "POST /admin/content/monsters": respond(409, { field: "id", message: "id monster-1 已存在" }),
    })
    await screen.findByRole("row", { name: /monster-1/ })
    await userEvent.click(screen.getByRole("button", { name: "新增" }))
    const dialog = await screen.findByRole("dialog")
    await userEvent.type(within(dialog).getByLabelText("id"), "monster-1")
    await userEvent.type(within(dialog).getByLabelText("名稱"), "重複")

    await save(dialog)

    expect(await within(dialog).findByText(/id monster-1 已存在/)).toBeInTheDocument()
    expect(screen.getByRole("dialog")).toBeInTheDocument()
  })

  it("requires a name before sending anything", async () => {
    const calls = renderMonsters()
    const dialog = await openEdit()

    await userEvent.clear(within(dialog).getByLabelText("名稱"))
    await save(dialog)

    expect(await within(dialog).findByText("請填寫名稱")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })

  it("adds a skill by name with its level and activation rate", async () => {
    const calls = renderMonsters({}, [monsterDto({ skills: [] })])
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("button", { name: "新增技能" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "技能 1" }), "爆氣")
    await setNumber(dialog, "技能等級 1", "2")
    await setNumber(dialog, "發動率 1", "10")
    await save(dialog)

    await waitFor(() =>
      expect(lastBody(calls, "PUT")?.skills).toEqual([
        { skillId: "skill-1", level: 2, activationRate: 10 },
      ]),
    )
  })

  it("shows the saved skills by name and can remove them", async () => {
    const calls = renderMonsters()
    const dialog = await openEdit()
    expect(within(dialog).getByTitle("爆氣")).toBeInTheDocument()

    await userEvent.click(within(dialog).getByRole("button", { name: "刪除技能 1" }))
    await save(dialog)

    await waitFor(() => expect(lastBody(calls, "PUT")?.skills).toEqual([]))
  })

  it("does not let a skill level go above the chosen skill's maximum", async () => {
    const calls = renderMonsters({}, [monsterDto({ skills: [] })])
    const dialog = await openEdit()
    await userEvent.click(within(dialog).getByRole("button", { name: "新增技能" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "技能 1" }), "水彈")
    await setNumber(dialog, "技能等級 1", "4")
    await setNumber(dialog, "發動率 1", "10")

    await save(dialog)

    expect(await within(dialog).findByText("水彈的等級上限是 3")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })

  it("checks the level again when the skill changes to one with a lower maximum", async () => {
    const calls = renderMonsters({}, [
      monsterDto({ skills: [{ skillId: "skill-1", level: 5, activationRate: 10 }] }),
    ])
    const dialog = await openEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "技能 1" }), "水彈")
    await save(dialog)

    expect(await within(dialog).findByText("水彈的等級上限是 3")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })

  it("keeps the dialog open and names the field when the skill no longer exists", async () => {
    renderMonsters({
      "PUT /admin/content/monsters/monster-1": respond(400, {
        field: "skills[0].skillId",
        message: "技能 skill-1 不存在",
      }),
    })
    const dialog = await openEdit()

    await save(dialog)

    expect(await within(dialog).findByText(/skills\[0\]\.skillId/)).toBeInTheDocument()
    expect(screen.getByRole("dialog")).toBeInTheDocument()
  })

  it("hides the skills of an NPC", async () => {
    renderMonsters()
    const dialog = await openEdit()

    await userEvent.click(within(dialog).getByRole("checkbox", { name: "NPC" }))

    expect(within(dialog).queryByRole("button", { name: "新增技能" })).not.toBeInTheDocument()
  })
})
