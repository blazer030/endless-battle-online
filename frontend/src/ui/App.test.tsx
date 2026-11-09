import { act, render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError, type ApiClient } from "../api/client"
import { readAccessToken, saveAccessToken } from "../api/authStorage"
import type { CharacterView } from "../game/types"
import { PassageMilliseconds, RegisterSealSettleMilliseconds } from "../motion/timings"
import { makeCharacter, makeCreationOptions, makePublicJobs } from "../test/fixtures"
import { App, type SessionExpiryListener } from "./App"

// 路由測試只關心切到哪個畫面，冒險畫面本身另有測試
vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({
    initialCharacter,
    onSignOut,
  }: {
    initialCharacter: CharacterView
    onSignOut: () => void
  }) => (
    <>
      <p>冒險中：{initialCharacter.name}</p>
      <button type="button" onClick={onSignOut}>
        登出
      </button>
    </>
  ),
}))

vi.mock("./create/CreateScreen", () => ({
  CreateScreen: ({ onLeave }: { onLeave: () => void }) => (
    <>
      <p>建立角色</p>
      <button type="button" onClick={onLeave}>
        先離開
      </button>
    </>
  ),
}))

const character = makeCharacter({ name: "青衣" })

const createFakeApi = (overrides: Partial<ApiClient> = {}): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn().mockResolvedValue(null),
  createCharacter: vi.fn(),
  advance: vi.fn(),
  getContent: vi.fn(),
  getPublicJobs: vi.fn().mockResolvedValue(makePublicJobs()),
  getCreationOptions: vi.fn().mockResolvedValue(makeCreationOptions()),
  getJobs: vi.fn(),
  changeJob: vi.fn(),
  getSkills: vi.fn(),
  learnSkill: vi.fn(),
  setSkillSlots: vi.fn(),
  getWorld: vi.fn(),
  moveTo: vi.fn(),
  getInventory: vi.fn(),
  equip: vi.fn(),
  unequip: vi.fn(),
  useItem: vi.fn(),
  getShop: vi.fn(),
  buy: vi.fn(),
  sell: vi.fn(),
  getDepot: vi.fn(),
  store: vi.fn(),
  take: vi.fn(),
  deposit: vi.fn(),
  withdraw: vi.fn(),
  refine: vi.fn(),
  socket: vi.fn(),
  unsocket: vi.fn(),
  drill: vi.fn(),
  mergeStones: vi.fn(),
  mergeCrossStones: vi.fn(),
  getRecipes: vi.fn().mockResolvedValue([]),
  synthesize: vi.fn(),
  getMissions: vi.fn(),
  acceptMission: vi.fn(),
  abandonMission: vi.fn(),
  completeMission: vi.fn(),
  getPets: vi.fn(),
  carryPet: vi.fn(),
  storePet: vi.fn(),
  releasePet: vi.fn(),
  renamePet: vi.fn(),
  feedPet: vi.fn(),
  getMercenaries: vi.fn(),
  hireMercenary: vi.fn(),
  deployMercenary: vi.fn(),
  restMercenary: vi.fn(),
  renameMercenary: vi.fn(),
  dismissMercenary: vi.fn(),
  getHonourShop: vi.fn(),
  exchangeHonour: vi.fn(),
  updateSettings: vi.fn(),
  getBuffs: vi.fn().mockResolvedValue([]),
  getInn: vi.fn().mockResolvedValue({ rooms: [], lodging: null }),
  bookInn: vi.fn(),
  ...overrides,
})

beforeEach(() => {
  localStorage.clear()
  vi.useFakeTimers({ shouldAdvanceTime: true })
})
afterEach(() => {
  vi.useRealTimers()
  localStorage.clear()
})

const setupUser = () => userEvent.setup({ advanceTimers: vi.advanceTimersByTime })

const renderApp = (
  api: ApiClient,
  subscribeToSessionExpiry: SessionExpiryListener = () => () => {},
) => render(<App api={api} subscribeToSessionExpiry={subscribeToSessionExpiry} />)

const ownCard = () => within(screen.getByRole("region", { name: "我的冒險者登記證" }))
const blankCard = () => within(screen.getByRole("region", { name: "空白登記證" }))

const signIn = async (user: ReturnType<typeof setupUser>, account = "ethan") => {
  await user.type(ownCard().getByLabelText("帳號"), account)
  await user.type(ownCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(ownCard().getByRole("button", { name: "入城" }))
}

test("App_ShowsRegistryCards_WhenNoToken", () => {
  const api = createFakeApi()

  renderApp(api)

  expect(ownCard().getByRole("button", { name: "入城" })).toBeInTheDocument()
  expect(api.getMyCharacter).not.toHaveBeenCalled()
})

// destination after loading：登入、有角色 → 歡迎回來＋帳號，停一下再進遊戲
test("App_SignInWithCharacter_WelcomesBackThenOpensGame", async () => {
  const user = setupUser()
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })
  renderApp(api)

  await signIn(user)

  expect(await screen.findByText("歡迎回來")).toBeInTheDocument()
  expect(screen.getByText("ethan")).toBeInTheDocument()
  expect(screen.queryByText("冒險中：青衣")).not.toBeInTheDocument()

  act(() => vi.advanceTimersByTime(PassageMilliseconds))

  expect(await screen.findByText("冒險中：青衣")).toBeInTheDocument()
})

// 登入、沒角色 → 建角
test("App_SignInWithoutCharacter_OpensCharacterCreation", async () => {
  const user = setupUser()
  const api = createFakeApi()
  renderApp(api)

  await signIn(user)

  expect(await screen.findByText("建立角色")).toBeInTheDocument()
  expect(screen.queryByText("歡迎回來")).not.toBeInTheDocument()
})

// 註冊 → 建角
test("App_Register_OpensCharacterCreation", async () => {
  const user = setupUser()
  const api = createFakeApi()
  renderApp(api)

  await user.click(screen.getByRole("button", { name: "拿一張空白登記證" }))
  await user.type(blankCard().getByLabelText("帳號"), "newbie")
  await user.type(blankCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(blankCard().getByRole("button", { name: "登記" }))
  await vi.waitFor(() => expect(api.login).toHaveBeenCalledWith("newbie", "abcdefgh"))
  act(() => vi.advanceTimersByTime(RegisterSealSettleMilliseconds))

  expect(await screen.findByText("建立角色")).toBeInTheDocument()
})

// 建角時離開 → 回到標題，自己那張登記證帶剛註冊的帳號、密碼空白
test("App_LeavingCreation_ReturnsToOwnCardWithAccount", async () => {
  const user = setupUser()
  renderApp(createFakeApi())
  await user.click(screen.getByRole("button", { name: "拿一張空白登記證" }))
  await user.type(blankCard().getByLabelText("帳號"), "newbie")
  await user.type(blankCard().getByLabelText("密碼"), "abcdefgh")
  await user.click(blankCard().getByRole("button", { name: "登記" }))
  act(() => vi.advanceTimersByTime(RegisterSealSettleMilliseconds))
  await screen.findByText("建立角色")

  await user.click(screen.getByRole("button", { name: "先離開" }))

  expect(ownCard().getByLabelText("帳號")).toHaveValue("newbie")
  expect(ownCard().getByLabelText("密碼")).toHaveValue("")
  expect(ownCard().getByLabelText("帳號")).toBeEnabled()
})

// 已登入打開網頁、有角色 → 直接進遊戲，不顯示登記證與歡迎回來
test("App_StoredTokenWithCharacter_OpensGameDirectly", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })

  renderApp(api)

  expect(screen.queryByRole("button", { name: "入城" })).not.toBeInTheDocument()
  expect(await screen.findByText("冒險中：青衣")).toBeInTheDocument()
  expect(screen.queryByText("歡迎回來")).not.toBeInTheDocument()
})

// 已登入打開網頁、沒角色 → 建角
test("App_StoredTokenWithoutCharacter_OpensCharacterCreation", async () => {
  saveAccessToken("token")
  const api = createFakeApi()

  renderApp(api)

  expect(await screen.findByText("建立角色")).toBeInTheDocument()
})

// 讀角色 401 → 清 token、回到登記證（client 收到 401 會清 token 並通知）
test("App_UnauthorizedLoad_ReturnsToRegistryCards", async () => {
  saveAccessToken("token")
  let expire = () => {}
  const api = createFakeApi({
    getMyCharacter: vi.fn().mockImplementation(() => {
      localStorage.clear()
      expire()
      return Promise.reject(new ApiError(401, "未授權"))
    }),
  })

  renderApp(api, (listener) => {
    expire = listener
    return () => {}
  })

  expect(await ownCard().findByRole("button", { name: "入城" })).toBeInTheDocument()
  expect(readAccessToken()).toBeNull()
  expect(screen.queryByRole("alert", { name: "讀取失敗" })).not.toBeInTheDocument()
})

test.each([
  {
    failure: "character server error",
    overrides: { getMyCharacter: vi.fn().mockRejectedValue(new ApiError(500, "伺服器錯誤")) },
  },
  {
    failure: "creation data network error",
    overrides: { getPublicJobs: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) },
  },
])("App_FailedLoad_ShowsNoteWithRetry ($failure)", async ({ overrides }) => {
  saveAccessToken("token")
  const api = createFakeApi(overrides)

  renderApp(api)

  const note = await screen.findByRole("alert", { name: "讀取失敗" })
  expect(note).toHaveTextContent("公會那邊沒有回音……")
  expect(within(note).getByRole("button", { name: "重試" })).toBeInTheDocument()
  expect(readAccessToken()).toBe("token")
})

// 重試沿用第一次的目的地：登入後讀不到 → 重試成功 → 一樣先歡迎回來
test("App_RetryRepeatsLoadWithSameDestination", async () => {
  const user = setupUser()
  const api = createFakeApi({
    getMyCharacter: vi
      .fn()
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockResolvedValueOnce(character),
  })
  renderApp(api)
  await signIn(user)

  const note = await screen.findByRole("alert", { name: "讀取失敗" })
  await user.click(within(note).getByRole("button", { name: "重試" }))

  expect(await screen.findByText("歡迎回來")).toBeInTheDocument()
  expect(screen.queryByRole("alert", { name: "讀取失敗" })).not.toBeInTheDocument()
  expect(api.getMyCharacter).toHaveBeenCalledTimes(2)
})

test("App_ReturnsToRegistryCards_WhenSessionExpires", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })
  let expire = () => {}
  renderApp(api, (listener) => {
    expire = listener
    return () => {}
  })
  await screen.findByText("冒險中：青衣")

  act(() => expire())

  expect(ownCard().getByRole("button", { name: "入城" })).toBeInTheDocument()
})

// 登入時密碼錯（401）也會觸發 client 的登出通知，登記證上的錯誤要留著
test("App_KeepsLoginError_WhenWrongPasswordTriggersSessionExpiry", async () => {
  const user = setupUser()
  let expire = () => {}
  const api = createFakeApi({
    login: vi.fn().mockImplementation(() => {
      expire()
      return Promise.reject(new ApiError(401, "帳號或密碼錯誤"))
    }),
  })
  renderApp(api, (listener) => {
    expire = listener
    return () => {}
  })

  await signIn(user)

  expect(await ownCard().findByRole("alert")).toHaveTextContent("帳號或密碼錯誤")
  expect(ownCard().getByLabelText("帳號")).toHaveValue("ethan")
})

test("App_SignOut_ClearsTokenAndReturnsToRegistryCards", async () => {
  const user = setupUser()
  saveAccessToken("token")
  renderApp(createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) }))
  await screen.findByText("冒險中：青衣")

  await user.click(screen.getByRole("button", { name: "登出" }))

  expect(readAccessToken()).toBeNull()
  expect(ownCard().getByLabelText("帳號")).toHaveValue("")
  expect(ownCard().getByRole("button", { name: "入城" })).toBeInTheDocument()
})
