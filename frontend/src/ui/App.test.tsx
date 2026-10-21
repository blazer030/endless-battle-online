import { act, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import { saveAccessToken } from "../api/authStorage"
import type { CharacterView } from "../game/types"
import { makeCharacter, makeContent } from "../test/fixtures"
import { App } from "./App"

// 路由測試只關心切到哪個畫面，歷練畫面本身另有測試
vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({ initialCharacter }: { initialCharacter: CharacterView }) => (
    <p>歷練中：{initialCharacter.name}</p>
  ),
}))

const character = makeCharacter({ name: "青衣" })

const job = {
  id: "warrior",
  name: "戰士",
  tier: 1,
  parentId: null,
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
}

const createFakeApi = (overrides: Partial<ApiClient> = {}): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn().mockResolvedValue(null),
  createCharacter: vi.fn(),
  advance: vi.fn(),
  getContent: vi.fn(),
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

test("App_ShowsAdventure_WhenCharacterExists", async () => {
  saveAccessToken("token")
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(character) })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByText("歷練中：青衣")).toBeInTheDocument()
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
  await screen.findByText("歷練中：青衣")

  act(() => expire())

  expect(screen.getByRole("button", { name: "登入" })).toBeInTheDocument()
})
