import { render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError, type ApiClient } from "../api/client"
import type { ContentNames } from "../game/types"
import { makeCharacter, makeContent, makeJob } from "../test/fixtures"
import { CreateCharacterScreen } from "./CreateCharacterScreen"

const job = (id: string, name: string, tier = 1) =>
  makeJob({
    id,
    name,
    tier,
    parentId: tier === 1 ? null : "warrior",
    unlockLevel: tier === 1 ? 1 : 30,
  })

const warrior = {
  ...job("warrior", "戰士"),
  growth: {
    str: [2, 3],
    smart: [0, 1],
    life: [1, 2],
    vit: [2, 3],
    agi: [0, 1],
    au: [0, 1],
    be: [0, 1],
  },
}

const content: ContentNames = makeContent({
  jobs: [
    warrior,
    job("sorcerer", "術士"),
    job("thief", "盜賊"),
    job("bard", "樂手"),
    job("berserker", "狂戰士", 2),
  ],
  elements: ["地", "水", "火", "木", "風", "毒"].map((name, index) => ({
    id: String(index + 1),
    name,
  })),
  births: ["中央大陸", "魔法王國", "熱帶雨林", "末日王城"].map((name, index) => ({
    id: String(index + 1),
    name,
  })),
  sexes: [
    { id: "1", name: "男" },
    { id: "2", name: "女" },
  ],
})

const renderScreen = (createCharacter: ApiClient["createCharacter"] = vi.fn()) => {
  const onCreated = vi.fn()
  render(
    <CreateCharacterScreen api={{ createCharacter }} content={content} onCreated={onCreated} />,
  )
  return { onCreated }
}

const group = (name: string) => screen.getByRole("group", { name })

test("CreateCharacterScreen_StartsWithSixPointsLeft", () => {
  renderScreen()

  expect(screen.getByText("剩餘 6 點")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "減少力量" })).toBeDisabled()
})

test("CreateCharacterScreen_ListsOnlyFirstTierJobs", () => {
  renderScreen()

  const jobs = within(group("職業")).getAllByRole("button")
  expect(jobs).toHaveLength(4)
  expect(jobs[0]).toHaveTextContent("戰士")
  expect(jobs[0]).toHaveTextContent("近身前排，攻守兼備")
  expect(jobs[0]).toHaveTextContent("成長：力量／體質 2～3")
  expect(screen.queryByText("狂戰士")).not.toBeInTheDocument()
})

test("CreateCharacterScreen_BlocksAllocationBeyondTenPoints", async () => {
  renderScreen()
  const addStrength = screen.getByRole("button", { name: "增加力量" })

  for (let click = 0; click < 6; click++) await userEvent.click(addStrength)

  expect(screen.getByText("剩餘 0 點")).toBeInTheDocument()
  expect(addStrength).toBeDisabled()
  expect(screen.getByRole("button", { name: "增加敏捷" })).toBeDisabled()
  await userEvent.click(addStrength)
  expect(screen.getByText("剩餘 0 點")).toBeInTheDocument()
})

test("CreateCharacterScreen_PreviewsHpAndSpMax", async () => {
  renderScreen()
  const preview = screen.getByRole("region", { name: "能力預覽" })
  expect(preview).toHaveTextContent("HP 上限 325")
  expect(preview).toHaveTextContent("SP 上限 88")

  await userEvent.click(screen.getByRole("button", { name: "增加生命" }))
  await userEvent.click(screen.getByRole("button", { name: "增加智力" }))

  expect(preview).toHaveTextContent("HP 上限 350")
  expect(preview).toHaveTextContent("SP 上限 91")
})

test("CreateCharacterScreen_SubmitsTheSelections", async () => {
  const created = makeCharacter({ name: "青衣" })
  const createCharacter = vi.fn<ApiClient["createCharacter"]>().mockResolvedValue(created)
  const { onCreated } = renderScreen(createCharacter)

  await userEvent.type(screen.getByLabelText("名稱"), "青衣")
  await userEvent.type(screen.getByLabelText("必殺技名稱"), "流星火雨")
  await userEvent.click(within(group("性別")).getByRole("button", { name: "女" }))
  await userEvent.click(within(group("屬性")).getByRole("button", { name: "火" }))
  await userEvent.click(within(group("出生地")).getByRole("button", { name: "末日王城" }))
  await userEvent.click(within(group("職業")).getByRole("button", { name: /術士/ }))
  for (let click = 0; click < 4; click++) {
    await userEvent.click(screen.getByRole("button", { name: "增加智力" }))
  }
  await userEvent.click(screen.getByRole("button", { name: "增加生命" }))
  await userEvent.click(screen.getByRole("button", { name: "增加生命" }))
  await userEvent.click(screen.getByRole("button", { name: "減少生命" }))
  await userEvent.click(screen.getByRole("button", { name: "踏上旅途" }))

  await vi.waitFor(() => expect(onCreated).toHaveBeenCalledWith(created))
  expect(createCharacter).toHaveBeenCalledWith({
    name: "青衣",
    sex: 2,
    element: 3,
    birth: 4,
    jobId: "sorcerer",
    allocation: { str: 1, smart: 5, life: 2, agi: 1 },
    specialAttackName: "流星火雨",
  })
})

test("CreateCharacterScreen_MapsEachStepperToItsOwnStat", async () => {
  const createCharacter = vi.fn<ApiClient["createCharacter"]>().mockResolvedValue(makeCharacter())
  renderScreen(createCharacter)

  const clicks: [string, number][] = [
    ["增加力量", 3],
    ["增加智力", 2],
    ["增加敏捷", 1],
  ]
  for (const [label, count] of clicks) {
    for (let click = 0; click < count; click++) {
      await userEvent.click(screen.getByRole("button", { name: label }))
    }
  }
  await userEvent.click(screen.getByRole("button", { name: "踏上旅途" }))

  await vi.waitFor(() => expect(createCharacter).toHaveBeenCalled())
  const { allocation } = createCharacter.mock.calls[0][0]
  expect(allocation.str).toBe(4)
  expect(allocation.smart).toBe(3)
  expect(allocation.life).toBe(1)
  expect(allocation.agi).toBe(2)
})

test("CreateCharacterScreen_ShowsValidationDetail", async () => {
  const createCharacter = vi
    .fn<ApiClient["createCharacter"]>()
    .mockRejectedValue(new ApiError(400, "必殺技名稱必須是 1～8 個字"))
  const { onCreated } = renderScreen(createCharacter)

  await userEvent.type(screen.getByLabelText("名稱"), "青衣")
  await userEvent.click(screen.getByRole("button", { name: "踏上旅途" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("必殺技名稱必須是 1～8 個字")
  expect(onCreated).not.toHaveBeenCalled()
  expect(screen.getByRole("button", { name: "踏上旅途" })).toBeEnabled()
})
