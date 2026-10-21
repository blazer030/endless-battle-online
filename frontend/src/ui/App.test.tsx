import { act, render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError, type ApiClient } from "../api/client"
import { readAccessToken, saveAccessToken } from "../api/authStorage"
import type { CharacterView } from "../game/types"
import { makeCharacter, makeContent, makeJob } from "../test/fixtures"
import { App } from "./App"

// 路由測試只關心切到哪個畫面，冒險畫面本身另有測試
vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({ initialCharacter }: { initialCharacter: CharacterView }) => (
    <p>冒險中：{initialCharacter.name}</p>
  ),
}))

const character = makeCharacter({ name: "青衣" })

const job = makeJob()

const createFakeApi = (overrides: Partial<ApiClient> = {}): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn().mockResolvedValue(null),
  createCharacter: vi.fn(),
  advance: vi.fn(),
  getContent: vi.fn(),
  getJobs: vi.fn(),
  changeJob: vi.fn(),
  getSkills: vi.fn(),
  learnSkill: vi.fn(),
  setSkillSlots: vi.fn(),
  ...overrides,
})

beforeEach(() => localStorage.clear())
afterEach(() => localStorage.clear())

test("App_ShowsLogin_WhenNoToken", () => {
  const api = createFakeApi()

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(screen.getByRole("button", { name: "登入" })).toBeInTheDocument()
  expect(api.getMyCharacter).not.toHaveBeenCalled()
})

test("App_ShowsCreateCharacter_WhenLoggedInWithoutCharacter", async () => {
  saveAccessToken("token")
  const content = makeContent({ jobs: [job] })
  const api = createFakeApi({
    getMyCharacter: vi.fn().mockResolvedValue(null),
    getContent: vi.fn().mockResolvedValue(content),
  })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByRole("button", { name: "踏上旅途" })).toBeInTheDocument()
  expect(screen.getByRole("button", { name: /戰士/ })).toBeInTheDocument()
})

test("App_KeepsLoading_UntilContentArrives", async () => {
  saveAccessToken("token")
  const api = createFakeApi({
    getMyCharacter: vi.fn().mockResolvedValue(null),
    getContent: vi.fn(() => new Promise<never>(() => {})),
  })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  await vi.waitFor(() => expect(api.getContent).toHaveBeenCalled())
  expect(screen.getByText("載入中…")).toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "踏上旅途" })).not.toBeInTheDocument()
})

test.each([
  {
    failure: "content network error",
    overrides: {
      getMyCharacter: vi.fn().mockResolvedValue(null),
      getContent: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")),
    },
  },
  {
    failure: "character server error",
    overrides: {
      getMyCharacter: vi.fn().mockRejectedValue(new ApiError(500, "伺服器錯誤")),
    },
  },
])("App_ShowsRetry_WhenLoadFails ($failure)", async ({ overrides }) => {
  saveAccessToken("token")
  const api = createFakeApi(overrides)

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByText("載入失敗，請稍後再試")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "重試" })).toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "登入" })).not.toBeInTheDocument()
  expect(readAccessToken()).toBe("token")
})

test("App_RetriesLoad_WhenRetryClicked", async () => {
  saveAccessToken("token")
  const api = createFakeApi({
    getMyCharacter: vi.fn().mockResolvedValue(null),
    getContent: vi
      .fn()
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockResolvedValueOnce(makeContent({ jobs: [job] })),
  })
  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  await userEvent.click(await screen.findByRole("button", { name: "重試" }))

  expect(await screen.findByRole("button", { name: "踏上旅途" })).toBeInTheDocument()
  expect(api.getMyCharacter).toHaveBeenCalledTimes(2)
  expect(api.getContent).toHaveBeenCalledTimes(2)
})

// 401 由 client 的 onUnauthorized 處理；載入流程不另外顯示錯誤面板
test("App_ShowsLogin_WhenLoadIsUnauthorized", async () => {
  saveAccessToken("token")
  const api = createFakeApi({
    getMyCharacter: vi.fn().mockRejectedValue(new ApiError(401, "未授權")),
  })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByRole("button", { name: "登入" })).toBeInTheDocument()
  expect(screen.queryByText("載入失敗，請稍後再試")).not.toBeInTheDocument()
})

test("App_ShowsAdventure_WhenCharacterExists", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByText("冒險中：青衣")).toBeInTheDocument()
})

test("App_ShowsLogin_WhenSessionExpires", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })
  let expire = () => {}
  const subscribeToSessionExpiry = (listener: () => void) => {
    expire = listener
    return () => {}
  }
  render(<App api={api} subscribeToSessionExpiry={subscribeToSessionExpiry} />)
  await screen.findByText("冒險中：青衣")

  act(() => expire())

  expect(screen.getByRole("button", { name: "登入" })).toBeInTheDocument()
})
