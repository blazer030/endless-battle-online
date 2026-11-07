import { within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { chooseOption } from "../../test/antd"
import { readOfficialContent } from "../../test/officialContent"
import { ItemConverter, type ItemDto } from "./ItemConverter"
import { itemFromValues, itemToValues } from "./itemFormValues"
import {
  lastItemBody,
  openItemCreate,
  openItemEdit,
  renderItemPage,
  saveItem,
} from "./itemTestPage"
import { itemDto, items, stats } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const setNumber = async (dialog: HTMLElement, label: string, value: string) => {
  const input = within(dialog).getByLabelText(label)
  await userEvent.clear(input)
  await userEvent.type(input, value)
}

describe("ItemFormModal keys", () => {
  it("leaves only the key fields after changing a new item from weapon to key", async () => {
    const calls = renderItemPage()
    const dialog = await openItemCreate()
    await userEvent.type(within(dialog).getByLabelText("id"), "item-9002")
    await userEvent.type(within(dialog).getByLabelText("名稱"), "洞窟鑰匙")
    await setNumber(dialog, "加成 攻擊", "9")

    await chooseOption(within(dialog).getByRole("combobox", { name: "分類" }), "鑰匙")

    expect(within(dialog).queryByLabelText("加成 攻擊")).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText("孔數")).not.toBeInTheDocument()
    await chooseOption(within(dialog).getByRole("combobox", { name: "鑰匙地區" }), "灼熱荒漠")
    await setNumber(dialog, "鑰匙秒數", "86400")
    await saveItem(dialog, calls, "POST")

    const body = lastItemBody(calls, "POST")
    expect(body.key).toEqual({ placeId: 2, seconds: 86400 })
    expect(body.bonus).toBeUndefined()
    expect(body.requires).toBeUndefined()
  })

  it("requires a place for a key", async () => {
    const calls = renderItemPage([
      ...items,
      itemDto({ id: "item-1338", name: "鑰匙", category: "Material" }),
    ])
    const dialog = await openItemCreate()
    await userEvent.type(within(dialog).getByLabelText("id"), "item-9002")
    await userEvent.type(within(dialog).getByLabelText("名稱"), "鑰匙")
    await chooseOption(within(dialog).getByRole("combobox", { name: "分類" }), "鑰匙")

    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    expect(await within(dialog).findByText("請選擇地區")).toBeInTheDocument()
    expect(calls.some((call) => call.method === "POST")).toBe(false)
  })
})

describe("ItemFormModal stones", () => {
  const stone = itemDto({
    id: "item-568",
    name: "傷害魔石",
    category: "Stone",
    stone: { random: false, fixed: stats({ attack: 5 }), boxClass: 1 },
  })

  it("sends the fixed stats of a stone that is not random", async () => {
    const calls = renderItemPage([...items, stone])
    const dialog = await openItemEdit("item-568")

    await setNumber(dialog, "固定值 攻擊", "6")
    await setNumber(dialog, "寶箱等級", "2")
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT").stone).toEqual({
      random: false,
      fixed: stats({ attack: 6 }),
      boxClass: 2,
    })
  })

  it("sends minimum and maximum and no fixed for a random stone", async () => {
    const calls = renderItemPage([...items, stone])
    const dialog = await openItemEdit("item-568")

    await userEvent.click(within(dialog).getByRole("checkbox", { name: "隨機" }))
    expect(within(dialog).queryByLabelText("固定值 攻擊")).not.toBeInTheDocument()
    await setNumber(dialog, "最小 HP 上限", "700")
    await setNumber(dialog, "最大 HP 上限", "1900")
    await saveItem(dialog, calls, "PUT")

    const sent = lastItemBody(calls, "PUT").stone
    expect(sent?.fixed).toBeUndefined()
    expect(sent?.minimum).toEqual(stats({ maxHitPoints: 700 }))
    expect(sent?.maximum).toEqual(stats({ maxHitPoints: 1900 }))
  })
})

describe("ItemFormModal refine stones", () => {
  const refineStone = itemDto({
    id: "item-1863",
    name: "白精鍊石",
    category: "RefineStone",
    refine: { level: 1, kind: 1, strength: { minimum: 3, maximum: 4 } },
  })

  it("sends the level, series and only the stats that have a range", async () => {
    const calls = renderItemPage([...items, refineStone])
    const dialog = await openItemEdit("item-1863")
    expect(within(dialog).getByLabelText("精鍊 力量最小")).toHaveValue("3")

    await setNumber(dialog, "精鍊等級", "2")
    await setNumber(dialog, "系列", "8")
    await setNumber(dialog, "精鍊 攻擊最小", "2")
    await setNumber(dialog, "精鍊 攻擊最大", "3")
    await userEvent.clear(within(dialog).getByLabelText("精鍊 力量最小"))
    await userEvent.clear(within(dialog).getByLabelText("精鍊 力量最大"))
    await saveItem(dialog, calls, "PUT")

    expect(lastItemBody(calls, "PUT").refine).toEqual({
      level: 2,
      kind: 8,
      attack: { minimum: 2, maximum: 3 },
    })
  })
})

describe("ItemFormModal materials and medals", () => {
  it.each(["素材", "勳章"])("%s has only the common fields and bound", async (categoryLabel) => {
    const calls = renderItemPage()
    const dialog = await openItemEdit()

    await chooseOption(within(dialog).getByRole("combobox", { name: "分類" }), categoryLabel)

    expect(within(dialog).queryByLabelText("加成 攻擊")).not.toBeInTheDocument()
    expect(within(dialog).queryByRole("combobox", { name: "消耗品種類" })).not.toBeInTheDocument()
    await userEvent.click(within(dialog).getByRole("checkbox", { name: "綁定" }))
    await saveItem(dialog, calls, "PUT")

    const body = lastItemBody(calls, "PUT")
    expect(body.bound).toBe(true)
    expect(body.bonus).toBeUndefined()
    expect(body.holes).toBeUndefined()
  })
})

// 內容檔的欄位轉成 API 的欄位；轉換規則獨立於 Converter
type FileRecord = Record<string, unknown>

const fileStatNames: Record<string, string> = {
  str: "strength",
  smart: "intelligence",
  life: "life",
  vit: "vitality",
  agi: "agility",
  au: "charisma",
  be: "faith",
  at: "attack",
  mat: "magicAttack",
  df: "defense",
  mdf: "magicDefense",
  hpMax: "maxHitPoints",
}

const elementNames = ["None", "Earth", "Water", "Fire", "Wood", "Wind", "Poison"]

const statsFromFile = (values: unknown) =>
  values === undefined
    ? undefined
    : stats(
        Object.fromEntries(
          Object.entries(values as FileRecord).map(([key, value]) => [fileStatNames[key], value]),
        ),
      )

function requiresFromFile(values: unknown) {
  if (values === undefined) {
    return undefined
  }
  const file = values as FileRecord
  return {
    strength: file.str ?? 0,
    intelligence: file.smart ?? 0,
    agility: file.agi ?? 0,
    charisma: file.au ?? 0,
  }
}

function refineFromFile(values: unknown) {
  if (values === undefined) {
    return undefined
  }
  const { level, kind, ...ranges } = values as FileRecord
  return {
    level,
    kind,
    ...Object.fromEntries(
      Object.entries(ranges).map(([key, range]) => {
        const [minimum, maximum] = range as number[]
        return [fileStatNames[key], { minimum, maximum }]
      }),
    ),
  }
}

function itemFromFile(file: FileRecord): ItemDto {
  const stone = file.stone as FileRecord | undefined
  const skill = file.weaponSkill as FileRecord | undefined
  const dto = {
    id: file.id,
    name: file.name,
    category: file.category,
    consumableKind: file.consumable,
    bonus: statsFromFile(file.bonus),
    supplyHitPoints: file.supplyHp,
    supplySkillPoints: file.supplySp,
    amount: file.amount,
    requires: requiresFromFile(file.requires),
    element: file.element === undefined ? undefined : elementNames[file.element as number],
    holes: file.holes,
    refineMax: file.refineMax,
    price: file.price,
    shopLevel: file.shopLevel,
    shopSellable: file.shopSellable,
    bound: file.bound === true,
    refine: refineFromFile(file.refine),
    stone: stone && {
      random: stone.random,
      fixed: statsFromFile(stone.fixed),
      minimum: statsFromFile(stone.min),
      maximum: statsFromFile(stone.max),
      boxClass: stone.boxClass,
    },
    key: file.key,
    elixir: file.elixir,
    weaponSkill: skill && {
      skillId: skill.skillId,
      level: skill.level,
      activationRate: skill.rate,
    },
  }
  return JSON.parse(JSON.stringify(dto)) as ItemDto
}

describe("official item content", () => {
  const officialItems = () =>
    (readOfficialContent("items.json") as { items: FileRecord[] }).items.map(itemFromFile)

  it("goes through the converter unchanged for all 3127 items", () => {
    const dtos = officialItems()

    expect(dtos).toHaveLength(3127)
    for (const dto of dtos) {
      expect(JSON.parse(JSON.stringify(ItemConverter.toDto(ItemConverter.toVo(dto))))).toEqual(dto)
    }
  })

  it("goes through the form values unchanged for all 3127 items", () => {
    for (const dto of officialItems()) {
      const vo = ItemConverter.toVo(dto)

      expect(
        JSON.parse(JSON.stringify(ItemConverter.toDto(itemFromValues(itemToValues(vo))))),
      ).toEqual(dto)
    }
  })
})
