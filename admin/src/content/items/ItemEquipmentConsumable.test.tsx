import { screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { chooseOption } from "../../test/antd"
import { ContentParseError } from "../shared/parse"
import { ItemConverter } from "./ItemConverter"
import {
  lastItemBody,
  openItemCreate,
  openItemEdit,
  renderItemPage,
  saveItem,
} from "../../test/itemTestPage"
import { itemDto, items, stats } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const setNumber = async (dialog: HTMLElement, label: string, value: string) => {
  const input = within(dialog).getByLabelText(label)
  await userEvent.clear(input)
  await userEvent.type(input, value)
}

describe("ItemConverter", () => {
  it("reports the path of a missing field", () => {
    const dto = itemDto({ bonus: { attack: 1 } as never })

    expect(() => ItemConverter.toVo(dto, "items[0]")).toThrow(ContentParseError)
    expect(() => ItemConverter.toVo(dto, "items[0]")).toThrow("items[0].bonus.strength")
  })

  it("sends only the fields of the category", () => {
    const vo = ItemConverter.toVo(
      itemDto({
        category: "Body",
        bonus: stats({ defense: 5 }),
        element: "Fire",
        weaponSkill: { skillId: "skill-1", level: 1, activationRate: 5 },
        key: { placeId: 1, seconds: 60 },
        holes: 1,
      }),
    )

    expect(ItemConverter.toDto(vo)).toEqual(
      itemDto({ category: "Body", bonus: stats({ defense: 5 }), holes: 1 }),
    )
  })
})

describe("ItemListPage", () => {
  it("combines the category filter, the shop level filter and the search box", async () => {
    renderItemPage()
    await screen.findByRole("row", { name: /^item-2 / })

    await chooseOption(screen.getByRole("combobox", { name: "分類" }), "武器")
    expect(screen.queryByRole("row", { name: /^item-10 / })).not.toBeInTheDocument()
    expect(screen.getByRole("row", { name: /^item-3 / })).toBeInTheDocument()

    await chooseOption(screen.getByRole("combobox", { name: "商店等級" }), "2")
    expect(screen.queryByRole("row", { name: /^item-2 / })).not.toBeInTheDocument()
    expect(screen.getByRole("row", { name: /^item-3 / })).toBeInTheDocument()

    await userEvent.type(screen.getByPlaceholderText("搜尋 id 或名稱"), "木刀")
    expect(screen.queryByRole("row", { name: /^item-3 / })).not.toBeInTheDocument()
  })

  it("shows the category name", async () => {
    renderItemPage()

    const row = await screen.findByRole("row", { name: /^item-223 / })

    expect(within(row).getByText("消耗品")).toBeInTheDocument()
  })
})

describe("ItemFormModal equipment", () => {
  it("creates a new item from a copy with the other fields kept", async () => {
    const calls = renderItemPage()
    const dialog = await openItemCreate()

    await chooseOption(within(dialog).getByRole("combobox", { name: "複製自" }), "item-2 木刀")
    expect(within(dialog).getByLabelText("id")).toHaveValue("")
    await userEvent.type(within(dialog).getByLabelText("id"), "item-9001")
    await saveItem(dialog, calls, "POST")

    expect(lastItemBody(calls, "POST")).toEqual({ ...items[0], id: "item-9001" })
  })

  it("edits a stat bonus and leaves the other stats unchanged", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit()

    await setNumber(dialog, "加成 攻擊", "20")
    await setNumber(dialog, "需求 力量", "12")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT")).toEqual({
      ...items[0],
      bonus: stats({ attack: 20 }),
      requires: { strength: 12, intelligence: 0, agility: 0, charisma: 0 },
    })
  })

  it("has no element or weapon skill fields on body armor", async () => {
    renderItemPage()
    const dialog = await openItemEdit("item-10")

    expect(within(dialog).getByLabelText("孔數")).toBeInTheDocument()
    expect(within(dialog).queryByRole("combobox", { name: "屬性" })).not.toBeInTheDocument()
    expect(within(dialog).queryByRole("combobox", { name: "武器技能" })).not.toBeInTheDocument()
  })

  it("has no holes or refine maximum on an accessory", async () => {
    renderItemPage()
    const dialog = await openItemEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "分類" }), "飾品")

    expect(within(dialog).queryByLabelText("孔數")).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText("精鍊上限")).not.toBeInTheDocument()
    expect(within(dialog).getByLabelText("加成 攻擊")).toBeInTheDocument()
  })

  it("sends the chosen weapon skill with its level and activation rate", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "武器技能" }), "爆氣")
    await setNumber(dialog, "武器技能等級", "2")
    await setNumber(dialog, "武器技能發動率", "12")
    await chooseOption(within(dialog).getByRole("combobox", { name: "屬性" }), "火")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT")).toEqual({
      ...items[0],
      element: "Fire",
      weaponSkill: { skillId: "skill-1", level: 2, activationRate: 12 },
    })
  })

  it("removes the weapon skill when none is chosen", async () => {
    const list = [itemDto({ weaponSkill: { skillId: "skill-1", level: 2, activationRate: 12 } })]
    const calls = renderItemPage(list)
    const dialog = await openItemEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "武器技能" }), "無")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT").weaponSkill).toBeUndefined()
  })

  it("does not let the weapon skill level go above the skill's maximum", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "武器技能" }), "水彈")
    await setNumber(dialog, "武器技能等級", "4")
    await setNumber(dialog, "武器技能發動率", "12")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    expect(await within(dialog).findByText("水彈的等級上限是 3")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "PUT")).toBe(false)
  })
})

describe("ItemFormModal choices", () => {
  it("shows the path and blocks saving when the skill choices have a problem", async () => {
    renderItemPage(items, { "GET /admin/content/skills": [{ id: "skill-1" }] })
    const dialog = await openItemEdit()

    expect(await within(dialog).findByText(/skills\[0\]/)).toBeInTheDocument()
    expect(within(dialog).getByRole("button", { name: "儲存" })).toBeDisabled()
  })

  it("shows the path and blocks saving when the place choices have a problem", async () => {
    renderItemPage(items, { "GET /admin/content/places": [{ id: 1 }] })
    const dialog = await openItemEdit()

    expect(await within(dialog).findByText(/places\[0\]/)).toBeInTheDocument()
    expect(within(dialog).getByRole("button", { name: "儲存" })).toBeDisabled()
  })
})

describe("ItemFormModal copying", () => {
  const shield = itemDto({ id: "item-4", name: "木盾", bonus: stats({ defense: 2 }), holes: 1 })

  it("does not keep values of the first record after copying a second one", async () => {
    const calls = renderItemPage([...items, shield])
    const dialog = await openItemCreate()
    const copyFrom = within(dialog).getByRole("combobox", { name: "複製自" })

    await chooseOption(copyFrom, "item-2 木刀")
    await chooseOption(copyFrom, "item-4 木盾")
    await userEvent.type(within(dialog).getByLabelText("id"), "item-9001")
    await saveItem(dialog, calls, "POST")

    expect(lastItemBody(calls, "POST")).toEqual({ ...shield, id: "item-9001" })
  })

  it("goes back to a blank form when the copy is cleared", async () => {
    renderItemPage()
    const dialog = await openItemCreate()
    const copyFrom = within(dialog).getByRole("combobox", { name: "複製自" })
    await chooseOption(copyFrom, "item-2 木刀")
    expect(within(dialog).getByLabelText("名稱")).toHaveValue("木刀")

    const clear = copyFrom.closest(".ant-select")?.querySelector(".ant-select-clear")
    await userEvent.click(clear as Element)

    expect(within(dialog).getByLabelText("名稱")).toHaveValue("")
    expect(within(dialog).getByLabelText("加成 攻擊")).toHaveValue("")
  })
})

describe("ItemFormModal bonus", () => {
  it("edits the maximum HP bonus of equipment", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit()

    await setNumber(dialog, "加成 HP 上限", "100")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT").bonus).toEqual(stats({ attack: 15, maxHitPoints: 100 }))
  })
})

describe("ItemFormModal consumables", () => {
  it("shows only restored HP and SP for a supply", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit("item-223")

    expect(within(dialog).queryByLabelText("加成 攻擊")).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText("數值")).not.toBeInTheDocument()
    await setNumber(dialog, "回復 SP", "20")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT")).toEqual({ ...items[3], supplySkillPoints: 20 })
  })

  it.each([
    ["能力藥水", "加成 攻擊", { consumableKind: "StatPotion", bonus: stats({ attack: 7 }) }, "7"],
    ["捕捉道具", "數值", { consumableKind: "Capture", amount: 7 }, "7"],
  ])(
    "shows the fields of %s and sends only those",
    async (kindLabel, fieldLabel, expected, value) => {
      const calls = renderItemPage()
      const dialog = await openItemEdit("item-223")

      await chooseOption(within(dialog).getByRole("combobox", { name: "消耗品種類" }), kindLabel)
      expect(within(dialog).queryByLabelText("回復 HP")).not.toBeInTheDocument()
      await setNumber(dialog, fieldLabel, value)
      await saveItem(dialog, calls, "PUT")

      expect(lastItemBody(calls, "PUT")).toEqual(
        itemDto({
          id: "item-223",
          name: "回復劑",
          category: "Consumable",
          ...(expected as object),
        }),
      )
    },
  )

  it("edits an elixir's kind, multiplier and duration", async () => {
    const calls = renderItemPage()
    const dialog = await openItemEdit("item-223")

    await chooseOption(within(dialog).getByRole("combobox", { name: "消耗品種類" }), "密藥")
    await chooseOption(within(dialog).getByRole("combobox", { name: "密藥種類" }), "攻擊")
    await setNumber(dialog, "倍率", "1.5")
    await setNumber(dialog, "秒數", "7200")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT")).toEqual(
      itemDto({
        id: "item-223",
        name: "回復劑",
        category: "Consumable",
        consumableKind: "Elixir",
        elixir: { kind: "Attack", multiplier: 1.5, seconds: 7200 },
      }),
    )
  })
})
