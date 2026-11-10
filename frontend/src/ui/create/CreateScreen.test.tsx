import { render, screen, within } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import { makeCreationOptions, makePublicJobs } from "../../test/fixtures"
import { user } from "../../test/user"
import { CreateScreen } from "./CreateScreen"

const renderCreate = () =>
  render(
    <CreateScreen
      api={{ createCharacter: vi.fn(), getMyCharacter: vi.fn() }}
      jobs={makePublicJobs()}
      options={makeCreationOptions()}
      account="newbie"
      onCreated={vi.fn()}
      onLeave={vi.fn()}
    />,
  )

const row = (label: string) => screen.getByRole("button", { name: new RegExp(`^${label}`) })
const answer = () => screen.getByRole("dialog", { name: "回答" })
const lise = () => screen.getByTestId("lise-line")

const fillAllRows = async () => {
  await user.click(row("名字"))
  await user.type(within(answer()).getByRole("textbox", { name: "名字" }), "艾倫")
  await user.click(within(answer()).getByRole("button", { name: "就這樣" }))
  await user.click(row("性別"))
  await user.click(within(answer()).getByRole("button", { name: "先生" }))
  await user.click(row("出身"))
  await user.click(within(answer()).getByRole("button", { name: "魔法王國" }))
  await user.click(row("屬性"))
  await user.click(within(answer()).getByRole("button", { name: "火" }))
  await user.click(row("職業"))
  await user.click(screen.getByRole("tab", { name: "術士" }))
  await user.click(screen.getByRole("button", { name: "就職" }))
}

const fillStats = async () => {
  await user.click(row("能力"))
  for (let point = 0; point < 6; point++)
    await user.click(within(answer()).getByRole("button", { name: "增加智力" }))
  await user.click(within(answer()).getByRole("button", { name: "就這樣" }))
}

test("CreateScreen_GreetsWithAnEmptyCard", () => {
  renderCreate()

  expect(lise()).toHaveTextContent("初次見面！我是公會接待員莉瑟。請填寫這張登記證吧。")
  expect(screen.queryByRole("dialog", { name: "回答" })).not.toBeInTheDocument()
  expect(screen.getByRole("button", { name: "出發" })).toBeDisabled()
})

test("CreateScreen_OpensAnswerForTappedRow", async () => {
  renderCreate()

  await user.click(row("性別"))

  expect(lise()).toHaveTextContent("請問是先生還是小姐呢？")
  expect(within(answer()).getByRole("button", { name: "先生" })).toBeInTheDocument()
  expect(within(answer()).getByRole("button", { name: "小姐" })).toBeInTheDocument()

  await user.click(row("性別"))

  expect(screen.queryByRole("dialog", { name: "回答" })).not.toBeInTheDocument()
})

test("CreateScreen_ClosesAnswerOnDimTap", async () => {
  const { container } = renderCreate()
  await user.click(row("出身"))

  await user.click(container.querySelector<HTMLElement>(".create-dim")!)

  expect(screen.queryByRole("dialog", { name: "回答" })).not.toBeInTheDocument()
  expect(row("出身")).toHaveAccessibleName("出身")
})

test("CreateScreen_RejectsAShortName", async () => {
  renderCreate()
  await user.click(row("名字"))

  await user.type(within(answer()).getByRole("textbox", { name: "名字" }), "艾{Enter}")

  expect(lise()).toHaveTextContent("名字要 2～12 個字喔。")
  expect(row("名字")).toHaveAccessibleName("名字")
})

test("CreateScreen_ElementLineNamesStrongAndWeak", async () => {
  renderCreate()
  await user.click(row("屬性"))

  await user.click(within(answer()).getByRole("button", { name: "火" }))

  expect(lise()).toHaveTextContent("「火」屬性——剋木，怕水。")
  expect(row("屬性")).toHaveAccessibleName("屬性 火")
})

test("CreateScreen_DisablesStatsConfirmWhilePointsLeft", async () => {
  renderCreate()
  await user.click(row("能力"))

  expect(within(answer()).getByRole("button", { name: "就這樣" })).toBeDisabled()
  expect(row("能力")).toHaveTextContent("剩 6 點")
  for (let point = 0; point < 2; point++)
    await user.click(within(answer()).getByRole("button", { name: "增加力量" }))
  expect(row("能力")).toHaveTextContent("剩 4 點")
  expect(within(answer()).getByRole("button", { name: "就這樣" })).toBeDisabled()

  for (let point = 0; point < 4; point++)
    await user.click(within(answer()).getByRole("button", { name: "增加生命" }))

  expect(within(answer()).getByRole("button", { name: "增加力量" })).toBeDisabled()
  expect(within(answer()).getByRole("button", { name: "就這樣" })).toBeEnabled()
  expect(row("能力")).not.toHaveTextContent("剩")
})

test("CreateScreen_EnablesDepartOnlyWhenAllRowsFilled", async () => {
  renderCreate()

  await fillAllRows()

  expect(screen.getByRole("button", { name: "出發" })).toBeDisabled()

  await fillStats()

  expect(screen.getByRole("button", { name: "出發" })).toBeEnabled()
  expect(lise()).toHaveTextContent("都寫好了，艾倫！準備好就出發吧。")
  // HP／SP 照配點 1/7/1/1 算：生命 11 → 325、智力 17 → 106
  expect(screen.getByLabelText("生命與精神上限")).toHaveTextContent("HP325")
  expect(screen.getByLabelText("生命與精神上限")).toHaveTextContent("SP106")
})
