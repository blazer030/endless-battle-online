import { render, screen, within } from "@testing-library/react"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { makeCharacter, makeWorld } from "../test/fixtures"
import { WorldPanel, type WorldApi } from "./WorldPanel"

const character = makeCharacter()

const createApi = (overrides: Partial<WorldApi> = {}): WorldApi => ({
  getWorld: vi.fn().mockResolvedValue(makeWorld()),
  moveTo: vi.fn().mockResolvedValue(character),
  ...overrides,
})

const renderPanel = (api: WorldApi = createApi()) => {
  const onCharacterChange = vi.fn()
  const onClose = vi.fn()
  render(
    <WorldPanel
      api={api}
      character={character}
      onCharacterChange={onCharacterChange}
      onClose={onClose}
    />,
  )
  return { api, onCharacterChange, onClose }
}

const area = async (name: string) => screen.findByRole("region", { name })

const placeButton = async (name: string) =>
  screen.findByRole("button", { name: new RegExp(`^${name}`) })

test("WorldPanel_GroupsPlacesByAreaWithLabels", async () => {
  renderPanel()

  const central = await area("中央大陸")
  const plain = within(central).getByRole("button", { name: /^中央平原/ })
  expect(plain).toHaveTextContent("中央平原Lv 1–10推薦目前")
  expect(within(plain).getByText("推薦")).toHaveAttribute("data-tone", "recommended")
  const swamp = within(central).getByRole("button", { name: /^黑暗沼澤/ })
  expect(swamp).toHaveTextContent("黑暗沼澤Lv 10–20偏難")
  expect(within(swamp).getByText("偏難")).toHaveAttribute("data-tone", "too-hard")
  expect(within(swamp).queryByText("目前")).not.toBeInTheDocument()

  const magic = await area("魔法王國")
  const cave = within(magic).getByRole("button", { name: /^試鍊洞窟/ })
  expect(within(cave).getByText("偏易")).toHaveAttribute("data-tone", "too-easy")

  const forbidden = await area("禁區")
  expect(within(forbidden).getByRole("button", { name: /^黑曜神廟/ })).toHaveTextContent(
    "黑曜神廟Lv 130–160",
  )
})

test("WorldPanel_DisablesRestrictedPlaces", async () => {
  renderPanel()

  const abandoned = await placeButton("廢棄洞窟")

  expect(abandoned).toBeDisabled()
  expect(abandoned).toHaveTextContent("需要鑰匙")
})

test("WorldPanel_ExpandingAPlaceShowsItsSubPlaces", async () => {
  renderPanel()
  const cave = await placeButton("試鍊洞窟")
  expect(cave).toHaveAttribute("aria-expanded", "false")
  expect(screen.queryByRole("button", { name: "燈影隧道" })).not.toBeInTheDocument()

  await user.click(cave)

  expect(cave).toHaveAttribute("aria-expanded", "true")
  const subPlaces = within(screen.getByRole("group", { name: "試鍊洞窟的子地區" }))
    .getAllByRole("button")
    .map((button) => button.textContent)
  expect(subPlaces).toEqual(["燈影隧道", "晶礦源地", "狹窄地道", "地底空洞"])
})

test("WorldPanel_MarksTheCurrentSubPlace", async () => {
  renderPanel()

  await user.click(await placeButton("中央平原"))

  const current = screen.getByRole("button", { name: "香茅綠地" })
  expect(current).toBeDisabled()
  expect(current).toHaveAttribute("aria-current", "location")
  expect(screen.getByRole("button", { name: "綠風草地" })).toBeEnabled()
})

test("WorldPanel_ChoosingASubPlaceMovesAndCloses", async () => {
  const moved = makeCharacter({ placeId: 2, placeName: "試鍊洞窟", subPlaceName: "燈影隧道" })
  const { api, onCharacterChange, onClose } = renderPanel(
    createApi({ moveTo: vi.fn().mockResolvedValue(moved) }),
  )

  await user.click(await placeButton("試鍊洞窟"))
  await user.click(screen.getByRole("button", { name: "燈影隧道" }))

  expect(api.moveTo).toHaveBeenCalledWith(2, 1)
  expect(onCharacterChange).toHaveBeenCalledWith(moved)
  expect(onClose).toHaveBeenCalled()
})

test("WorldPanel_PlaceWithoutSubPlacesMovesWithNull", async () => {
  const { api, onClose } = renderPanel()

  await user.click(await placeButton("黑曜神廟"))

  expect(screen.queryByRole("group", { name: "黑曜神廟的子地區" })).not.toBeInTheDocument()
  await user.click(screen.getByRole("button", { name: "前往" }))

  expect(api.moveTo).toHaveBeenCalledWith(20, null)
  expect(onClose).toHaveBeenCalled()
})

test("WorldPanel_ShowsMoveReasonsInChinese", async () => {
  const { onClose } = renderPanel(
    createApi({
      moveTo: vi
        .fn()
        .mockRejectedValue(new ApiError(400, "無法移動", ["restricted", "subPlaceRequired"])),
    }),
  )

  await user.click(await placeButton("試鍊洞窟"))
  await user.click(screen.getByRole("button", { name: "燈影隧道" }))

  const reasons = within(await screen.findByRole("alert"))
    .getAllByRole("listitem")
    .map((item) => item.textContent)
  expect(reasons).toEqual(["此地區需要鑰匙", "請選擇子地區"])
  expect(onClose).not.toHaveBeenCalled()
})

test("WorldPanel_ShowsRetryHint_OnConflict", async () => {
  renderPanel(createApi({ moveTo: vi.fn().mockRejectedValue(new ApiError(409, "資料衝突")) }))

  await user.click(await placeButton("試鍊洞窟"))
  await user.click(screen.getByRole("button", { name: "燈影隧道" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("角色資料已被其他請求更新，請再試一次")
})

test("WorldPanel_ShowsFallback_OnConnectionError", async () => {
  renderPanel(createApi({ moveTo: vi.fn().mockRejectedValue(new TypeError("Failed to fetch")) }))

  await user.click(await placeButton("試鍊洞窟"))
  await user.click(screen.getByRole("button", { name: "燈影隧道" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("發生錯誤，請稍後再試")
})

test("WorldPanel_ShowsLoadingThenRetriesAfterLoadFailure", async () => {
  const getWorld = vi
    .fn()
    .mockRejectedValueOnce(new TypeError("Failed to fetch"))
    .mockResolvedValue(makeWorld())
  renderPanel(createApi({ getWorld }))
  expect(screen.getByText("載入中…")).toBeInTheDocument()

  expect(await screen.findByRole("alert")).toHaveTextContent("無法載入地區資料。")
  await user.click(screen.getByRole("button", { name: "重試" }))

  expect(await area("中央大陸")).toBeInTheDocument()
  expect(getWorld).toHaveBeenCalledTimes(2)
})

test("WorldPanel_CloseButtonCloses", async () => {
  const { onClose } = renderPanel()

  await user.click(screen.getByRole("button", { name: "關閉" }))

  expect(onClose).toHaveBeenCalled()
})
