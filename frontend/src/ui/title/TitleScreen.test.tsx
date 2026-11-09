import { render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../../api/client"
import { TitleScreen, type TitleApi } from "./TitleScreen"

const makeApi = (overrides: Partial<TitleApi> = {}): TitleApi => ({
  login: vi.fn().mockResolvedValue(undefined),
  register: vi.fn().mockResolvedValue(undefined),
  ...overrides,
})

const renderTitle = (api: TitleApi, onSignedIn = vi.fn()) => {
  render(<TitleScreen api={api} loading={null} onSignedIn={onSignedIn} onRetry={vi.fn()} />)
  return { onSignedIn }
}

const ownCard = () => within(screen.getByRole("region", { name: "我的冒險者登記證" }))
const blankCard = () => within(screen.getByRole("region", { name: "空白登記證" }))

test("TitleScreen_ShowsLoginErrorDetailOnOwnCard", async () => {
  const user = userEvent.setup()
  const api = makeApi({ login: vi.fn().mockRejectedValue(new ApiError(401, "帳號或密碼錯誤")) })
  const { onSignedIn } = renderTitle(api)

  await user.type(ownCard().getByLabelText("帳號"), "ethan")
  await user.type(ownCard().getByLabelText("密碼"), "wrongpass1")
  await user.click(ownCard().getByRole("button", { name: "入城" }))

  expect(await ownCard().findByRole("alert")).toHaveTextContent("帳號或密碼錯誤")
  expect(ownCard().getByLabelText("帳號")).toHaveValue("ethan")
  expect(ownCard().getByLabelText("密碼")).toHaveValue("wrongpass1")
  expect(onSignedIn).not.toHaveBeenCalled()
})

test("TitleScreen_DisablesEnterSealUntilBothFieldsFilled", async () => {
  const user = userEvent.setup()
  renderTitle(makeApi())

  await user.type(ownCard().getByLabelText("帳號"), "ethan")

  expect(ownCard().getByRole("button", { name: "入城" })).toBeDisabled()

  await user.type(ownCard().getByLabelText("密碼"), "abcdefgh")

  expect(ownCard().getByRole("button", { name: "入城" })).toBeEnabled()
})

test("TitleScreen_SignsInWithEnter", async () => {
  const user = userEvent.setup()
  const api = makeApi()
  const { onSignedIn } = renderTitle(api)

  await user.type(ownCard().getByLabelText("帳號"), "ethan")
  await user.type(ownCard().getByLabelText("密碼"), "abcdefgh{Enter}")

  expect(api.login).toHaveBeenCalledWith("ethan", "abcdefgh")
  await vi.waitFor(() => expect(onSignedIn).toHaveBeenCalledWith("ethan", "login"))
})

test("TitleScreen_ShowsAccountTakenOnBlankCard", async () => {
  const user = userEvent.setup()
  const api = makeApi({ register: vi.fn().mockRejectedValue(new ApiError(409, "帳號已被使用")) })
  renderTitle(api)

  await user.click(screen.getByRole("button", { name: "拿一張空白登記證" }))
  await user.type(blankCard().getByLabelText("帳號"), "ethan")
  await user.type(blankCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(blankCard().getByRole("button", { name: "登記" }))

  expect(await blankCard().findByRole("alert")).toHaveTextContent("帳號已被使用")
  expect(api.login).not.toHaveBeenCalled()
})

test("TitleScreen_RegisterThenSignsInWithSameAccount", async () => {
  const user = userEvent.setup()
  const api = makeApi()
  const { onSignedIn } = renderTitle(api)

  await user.click(screen.getByRole("button", { name: "拿一張空白登記證" }))
  await user.type(blankCard().getByLabelText("帳號"), "newbie")
  await user.type(blankCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(blankCard().getByRole("button", { name: "登記" }))

  expect(api.register).toHaveBeenCalledWith("newbie", "abcdefgh")
  await vi.waitFor(() => expect(api.login).toHaveBeenCalledWith("newbie", "abcdefgh"))
  await vi.waitFor(() => expect(onSignedIn).toHaveBeenCalledWith("newbie", "reg"), {
    timeout: 2000,
  })
})

test("TitleScreen_ShowsGenericErrorWhenNoApiResponse", async () => {
  const user = userEvent.setup()
  renderTitle(makeApi({ login: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) }))

  await user.type(ownCard().getByLabelText("帳號"), "ethan")
  await user.type(ownCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(ownCard().getByRole("button", { name: "入城" }))

  expect(await ownCard().findByRole("alert")).toHaveTextContent("發生錯誤，請稍後再試")
})
