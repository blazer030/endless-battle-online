import { act, render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { readAccessToken, saveAccessToken } from "../../api/authStorage"
import { ApiError } from "../../api/client"
import { PassageMilliseconds } from "../../motion/timings"
import { makeCharacter, makeCreationOptions, makePublicJobs } from "../../test/fixtures"
import { CreateScreen, type CreateApi } from "./CreateScreen"

beforeEach(() => vi.useFakeTimers({ shouldAdvanceTime: true }))
afterEach(() => {
  vi.useRealTimers()
  localStorage.clear()
})

const setupUser = () => userEvent.setup({ advanceTimers: vi.advanceTimersByTime })

const renderCreate = (api: CreateApi, onCreated = vi.fn(), onLeave = vi.fn()) => {
  render(
    <CreateScreen
      api={api}
      jobs={makePublicJobs()}
      options={makeCreationOptions()}
      account="newbie"
      onCreated={onCreated}
      onLeave={onLeave}
    />,
  )
  return { onCreated, onLeave }
}

const row = (label: string) => screen.getByRole("button", { name: new RegExp(`^${label}`) })
const answer = () => screen.getByRole("dialog", { name: "回答" })
const lise = () => screen.getByTestId("lise-line")
const depart = () => screen.getByRole("button", { name: "出發" })

const fillCard = async (user: ReturnType<typeof setupUser>, name = "艾倫") => {
  await user.click(row("名字"))
  await user.type(within(answer()).getByRole("textbox", { name: "名字" }), name)
  await user.click(within(answer()).getByRole("button", { name: "就這樣" }))
  await user.click(row("性別"))
  await user.click(within(answer()).getByRole("button", { name: "小姐" }))
  await user.click(row("出身"))
  await user.click(within(answer()).getByRole("button", { name: "熱帶雨林" }))
  await user.click(row("屬性"))
  await user.click(within(answer()).getByRole("button", { name: "風" }))
  await user.click(row("職業"))
  await user.click(screen.getByRole("button", { name: "就職" }))
  await user.click(row("能力"))
  for (let point = 0; point < 6; point++)
    await user.click(within(answer()).getByRole("button", { name: "增加力量" }))
  await user.click(within(answer()).getByRole("button", { name: "就這樣" }))
}

test("CreateScreen_DepartSendsTheCardAndShowsThePassage", async () => {
  const user = setupUser()
  const character = makeCharacter({ name: "艾倫" })
  const api = { createCharacter: vi.fn().mockResolvedValue(character) }
  const { onCreated } = renderCreate(api)
  await fillCard(user)

  await user.click(depart())

  expect(api.createCharacter).toHaveBeenCalledWith({
    name: "艾倫",
    sex: 2,
    birth: 3,
    element: 5,
    jobId: "job-6",
    allocation: { str: 7, smart: 1, life: 1, agi: 1 },
  })
  expect(await screen.findByText("踏上旅途")).toBeInTheDocument()
  expect(onCreated).not.toHaveBeenCalled()

  act(() => vi.advanceTimersByTime(PassageMilliseconds))

  expect(onCreated).toHaveBeenCalledWith(character)
})

test("CreateScreen_NameTakenOpensNameRow", async () => {
  const user = setupUser()
  const api = {
    createCharacter: vi
      .fn()
      .mockRejectedValue(new ApiError(409, "此帳號已有角色，或角色名稱已被使用")),
  }
  renderCreate(api)
  await fillCard(user, "收尾者")

  await user.click(depart())

  expect(
    await screen.findByText("哎呀，「收尾者」已經有人登記了……換一個名字好嗎？"),
  ).toBeInTheDocument()
  expect(within(answer()).getByRole("textbox", { name: "名字" })).toHaveValue("收尾者")
})

test("CreateScreen_BadRequestShowsDetail", async () => {
  const user = setupUser()
  const api = {
    createCharacter: vi.fn().mockRejectedValue(new ApiError(400, "能力點數必須剛好分完 10 點")),
  }
  renderCreate(api)
  await fillCard(user)

  await user.click(depart())

  await vi.waitFor(() => expect(lise()).toHaveTextContent("能力點數必須剛好分完 10 點"))
  expect(depart()).toBeEnabled()
})

test("CreateScreen_NetworkFailureKeepsCard", async () => {
  const user = setupUser()
  const api = { createCharacter: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) }
  renderCreate(api)
  await fillCard(user)

  await user.click(depart())

  await vi.waitFor(() => expect(lise()).toHaveTextContent("咦，登記簿那邊沒有回應……"))
  expect(row("名字")).toHaveAccessibleName("名字 艾倫")
  expect(row("性別")).toHaveAccessibleName("性別 女")
  expect(row("出身")).toHaveAccessibleName("出身 熱帶雨林")
  expect(row("屬性")).toHaveAccessibleName("屬性 風")
  expect(row("職業")).toHaveAccessibleName("職業 戰士")
  expect(row("能力")).not.toHaveTextContent("剩")
  expect(depart()).toBeEnabled()

  // 點一欄就換掉這句
  await user.click(row("性別"))
  expect(lise()).toHaveTextContent("請問是先生還是小姐呢？")
})

test("CreateScreen_LeaveClearsTokenAndKeepsAccount", async () => {
  const user = setupUser()
  saveAccessToken("token")
  const { onLeave } = renderCreate({ createCharacter: vi.fn() })

  await user.click(screen.getByRole("button", { name: "離開，回到標題" }))

  expect(lise()).toHaveTextContent("登記證還沒填完呢，確定現在就要離開嗎？")
  await user.click(within(answer()).getByRole("button", { name: "繼續填寫" }))
  expect(screen.queryByRole("dialog", { name: "回答" })).not.toBeInTheDocument()
  expect(readAccessToken()).toBe("token")

  await user.click(screen.getByRole("button", { name: "離開，回到標題" }))
  await user.click(within(answer()).getByRole("button", { name: "先離開" }))

  expect(readAccessToken()).toBeNull()
  expect(onLeave).toHaveBeenCalled()
})
