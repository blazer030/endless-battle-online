import { act, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import { saveAccessToken } from "../api/authStorage"
import type { CharacterView } from "../game/types"
import { App } from "./App"

// 路由測試只關心切到哪個畫面，歷練畫面本身另有測試
vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({ initialCharacter }: { initialCharacter: CharacterView }) => (
    <p>歷練中：{initialCharacter.name}</p>
  ),
}))

const character = { name: "青衣", experience: 0, spiritStones: 0, currentStageId: "stage-1" }

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
  const api = createFakeApi({ getMyCharacter: vi.fn().mockResolvedValue(null) })

  render(<App api={api} subscribeToSessionExpiry={() => () => {}} />)

  expect(await screen.findByRole("button", { name: "踏入仙途" })).toBeInTheDocument()
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
