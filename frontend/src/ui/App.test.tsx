import { act, render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError, type ApiClient } from "../api/client"
import { readAccessToken, saveAccessToken } from "../api/authStorage"
import type { CharacterVo } from "../game/character"
import {
  PassageLeaveMilliseconds,
  PassageMilliseconds,
  RegisterSealSettleMilliseconds,
} from "../motion/timings"
import { makeCharacter, makeCreationOptions, makePublicJobs } from "../test/fixtures"
import { App, type SessionExpiryListener } from "./App"

// 路由測試只關心切到哪個畫面，冒險畫面本身另有測試
vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({
    initialCharacter,
    onSignOut,
  }: {
    initialCharacter: CharacterVo
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

// 過場的計時器在 effect 裡設，可能比畫面晚一步；推進到真的進遊戲為止
const finishPassage = () =>
  vi.waitFor(() => {
    act(() => vi.advanceTimersByTime(PassageMilliseconds))
    expect(screen.getByText("冒險中：青衣")).toBeInTheDocument()
  })

// 有角色時先播歡迎回來過場，播完才進遊戲
const passIntoGame = async () => {
  await screen.findByText("歡迎回來")
  await finishPassage()
}

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

// result after loading：登入、有角色 → 歡迎回來＋角色名（不是帳號），停一下再進遊戲
test("App_SignInWithCharacter_WelcomesBackThenOpensGame", async () => {
  const user = setupUser()
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })
  renderApp(api)

  await signIn(user)

  expect(await screen.findByText("歡迎回來")).toBeInTheDocument()
  expect(screen.getByText("青衣")).toBeInTheDocument()
  expect(screen.queryByText("ethan")).not.toBeInTheDocument()
  expect(screen.queryByText("冒險中：青衣")).not.toBeInTheDocument()

  await finishPassage()
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
  await user.type(blankCard().getByLabelText("確認密碼"), "abcdefgh")
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
  await user.type(blankCard().getByLabelText("確認密碼"), "abcdefgh")
  await user.click(blankCard().getByRole("button", { name: "登記" }))
  act(() => vi.advanceTimersByTime(RegisterSealSettleMilliseconds))
  await screen.findByText("建立角色")

  await user.click(screen.getByRole("button", { name: "先離開" }))

  expect(ownCard().getByLabelText("帳號")).toHaveValue("newbie")
  expect(ownCard().getByLabelText("密碼")).toHaveValue("")
  expect(ownCard().getByLabelText("帳號")).toBeEnabled()
})

// 已登入打開網頁：還沒讀到角色時就停在過場背景＋墨點，看不到招牌、登記證
test("App_StoredToken_OpensOnPassageBackdropWithInkDots", () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockReturnValue(new Promise(() => {})) })

  const { container } = renderApp(api)

  expect(screen.getByRole("status", { name: "讀取中" })).toBeInTheDocument()
  expect(
    screen.queryByRole("heading", { name: "無盡戰記 Endless Battle Online" }),
  ).not.toBeInTheDocument()
  // 木桌是裝飾、沒有可查的角色，直接看標題的木桌元素在不在
  expect(container.querySelector(".title-desk")).toBeNull()
  expect(screen.queryByRole("button", { name: "入城" })).not.toBeInTheDocument()
  expect(screen.queryByText("歡迎回來")).not.toBeInTheDocument()
})

// 已登入打開網頁、有角色 → 不顯示登記證，跟登入一樣先歡迎回來＋角色名，停一下再進遊戲
test("App_StoredTokenWithCharacter_WelcomesBackThenOpensGame", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })

  renderApp(api)

  expect(screen.queryByRole("button", { name: "入城" })).not.toBeInTheDocument()
  expect(await screen.findByText("歡迎回來")).toBeInTheDocument()
  expect(screen.getByText("青衣")).toBeInTheDocument()
  // 墨點換成歡迎詞，背後還是過場背景，不是標題的招牌
  expect(screen.queryByRole("status", { name: "讀取中" })).not.toBeInTheDocument()
  expect(
    screen.queryByRole("heading", { name: "無盡戰記 Endless Battle Online" }),
  ).not.toBeInTheDocument()
  expect(screen.queryByText("冒險中：青衣")).not.toBeInTheDocument()

  await finishPassage()
})

// 讀到角色時不換背景：墨點和歡迎詞在同一個過場元素裡，背景不重畫、不閃
test("App_StoredTokenWithCharacter_KeepsPassageBackdrop", async () => {
  saveAccessToken("token")
  let finishLoad: (loaded: CharacterVo) => void = () => {}
  renderApp(
    createFakeApi({
      getMyCharacter: vi
        .fn()
        .mockReturnValue(new Promise<CharacterVo>((resolve) => (finishLoad = resolve))),
    }),
  )
  const backdrop = screen.getByRole("status", { name: "讀取中" }).parentElement

  await act(async () => finishLoad(character))

  expect(screen.getByText("歡迎回來").parentElement).toBe(backdrop)
})

// 過場停 PassageMilliseconds：差 1 毫秒還在過場，滿了才進遊戲
test("App_WelcomePassage_LastsPassageMilliseconds", async () => {
  // 時鐘不跟真實時間走，機器忙的時候才量得準
  vi.useFakeTimers({ shouldAdvanceTime: false })
  saveAccessToken("token")
  renderApp(createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) }))
  // findByText 要等計時器、vi.waitFor 會推時鐘；只用 act 讓讀角色與過場的 effect 跑完，時鐘停在 0
  for (let flush = 0; flush < 10 && screen.queryByText("歡迎回來") === null; flush++)
    await act(async () => {})
  expect(screen.getByText("歡迎回來")).toBeInTheDocument()

  act(() => vi.advanceTimersByTime(PassageMilliseconds - 1))
  expect(screen.queryByText("冒險中：青衣")).not.toBeInTheDocument()

  act(() => vi.advanceTimersByTime(1))
  expect(screen.getByText("冒險中：青衣")).toBeInTheDocument()
})

// 已登入打開網頁、沒角色 → 建角
test("App_StoredTokenWithoutCharacter_OpensCharacterCreation", async () => {
  saveAccessToken("token")
  const api = createFakeApi()

  renderApp(api)

  expect(await screen.findByText("建立角色")).toBeInTheDocument()
})

// 打開網頁沒角色 → 建角已在底下，過場背景留在上面淡出，PassageLeaveMilliseconds 後拿掉
test("App_StoredTokenWithoutCharacter_FadesPassageOverCreation", async () => {
  saveAccessToken("token")
  const { container } = renderApp(createFakeApi())

  await screen.findByText("建立角色")
  expect(container.querySelector(".passage")).not.toBeNull()
  expect(screen.queryByRole("status", { name: "讀取中" })).not.toBeInTheDocument()

  act(() => vi.advanceTimersByTime(PassageLeaveMilliseconds))
  expect(container.querySelector(".passage")).toBeNull()
})

// 打開網頁讀不到 → 登記證已在底下，過場背景留在上面淡出，PassageLeaveMilliseconds 後拿掉
test("App_StoredTokenLoadFails_FadesPassageOverRegistryCards", async () => {
  saveAccessToken("token")
  const { container } = renderApp(
    createFakeApi({ getMyCharacter: vi.fn().mockRejectedValue(new ApiError(500, "伺服器錯誤")) }),
  )

  await screen.findByRole("region", { name: "我的冒險者登記證" })
  expect(container.querySelector(".passage")).not.toBeNull()
  expect(screen.queryByRole("status", { name: "讀取中" })).not.toBeInTheDocument()

  act(() => vi.advanceTimersByTime(PassageLeaveMilliseconds))
  expect(container.querySelector(".passage")).toBeNull()
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

  const { container } = renderApp(api, (listener) => {
    expire = listener
    return () => {}
  })

  expect(await ownCard().findByRole("button", { name: "入城" })).toBeInTheDocument()
  // 401 由登出通知切回登記證，過場背景一樣淡出
  expect(container.querySelector(".passage")).not.toBeNull()
  expect(ownCard().getByLabelText("帳號")).toHaveValue("")
  expect(readAccessToken()).toBeNull()
  expect(screen.queryByRole("alert", { name: "讀取失敗" })).not.toBeInTheDocument()
})

// 登入後讀角色 401 → 清 token、回到登記證，帳號留著，不攤字條
test("App_SignInThenUnauthorizedLoad_ReturnsToRegistryCards", async () => {
  const user = setupUser()
  let expire = () => {}
  const api = createFakeApi({
    login: vi.fn().mockImplementation(async () => saveAccessToken("token")),
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

  await signIn(user)

  await vi.waitFor(() => expect(api.getMyCharacter).toHaveBeenCalled())
  expect(await ownCard().findByRole("button", { name: "入城" })).toBeInTheDocument()
  expect(ownCard().getByLabelText("帳號")).toHaveValue("ethan")
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
  // 字條＋重試只在登入、註冊之後；登入成功時 client 會存 token
  const user = setupUser()
  const api = createFakeApi({
    login: vi.fn().mockImplementation(async () => saveAccessToken("token")),
    ...overrides,
  })
  renderApp(api)

  await signIn(user)

  const note = await screen.findByRole("alert", { name: "讀取失敗" })
  expect(note).toHaveTextContent("公會那邊沒有回音……")
  expect(within(note).getByRole("button", { name: "重試" })).toBeInTheDocument()
  expect(readAccessToken()).toBe("token")
})

// 已登入打開網頁讀不到（斷線、伺服器錯誤）先當成登入失敗：清 token、回到登記證，不攤字條
test.each([
  {
    failure: "character server error",
    overrides: { getMyCharacter: vi.fn().mockRejectedValue(new ApiError(500, "伺服器錯誤")) },
  },
  {
    failure: "character network error",
    overrides: { getMyCharacter: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) },
  },
  {
    failure: "creation data network error",
    overrides: { getPublicJobs: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) },
  },
])(
  "App_StoredTokenLoadFails_ClearsTokenAndShowsRegistryCards ($failure)",
  async ({ overrides }) => {
    saveAccessToken("token")

    renderApp(createFakeApi(overrides))

    await screen.findByRole("region", { name: "我的冒險者登記證" })
    expect(ownCard().getByRole("button", { name: "入城" })).toBeInTheDocument()
    expect(ownCard().getByLabelText("帳號")).toHaveValue("")
    expect(readAccessToken()).toBeNull()
    expect(screen.queryByRole("alert", { name: "讀取失敗" })).not.toBeInTheDocument()
  },
)

// 重試再讀一次角色：登入後讀不到 → 重試時回到讀取中（字條收起、看不到登記證）→ 成功後跟第一次就成功一樣先歡迎回來
test("App_RetryRepeatsLoad", async () => {
  const user = setupUser()
  let finishRetry: (loaded: CharacterVo) => void = () => {}
  const api = createFakeApi({
    getMyCharacter: vi
      .fn()
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockReturnValueOnce(new Promise<CharacterVo>((resolve) => (finishRetry = resolve))),
  })
  renderApp(api)
  await signIn(user)

  const note = await screen.findByRole("alert", { name: "讀取失敗" })
  await user.click(within(note).getByRole("button", { name: "重試" }))

  await vi.waitFor(() => expect(api.getMyCharacter).toHaveBeenCalledTimes(2))
  expect(screen.queryByRole("alert", { name: "讀取失敗" })).not.toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "入城" })).not.toBeInTheDocument()
  expect(screen.queryByText("歡迎回來")).not.toBeInTheDocument()

  await act(async () => finishRetry(character))

  expect(await screen.findByText("歡迎回來")).toBeInTheDocument()
})

test("App_ReturnsToRegistryCards_WhenSessionExpires", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })
  let expire = () => {}
  renderApp(api, (listener) => {
    expire = listener
    return () => {}
  })
  await passIntoGame()

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
  await passIntoGame()

  await user.click(screen.getByRole("button", { name: "登出" }))

  expect(readAccessToken()).toBeNull()
  expect(ownCard().getByLabelText("帳號")).toHaveValue("")
  expect(ownCard().getByRole("button", { name: "入城" })).toBeInTheDocument()
})
