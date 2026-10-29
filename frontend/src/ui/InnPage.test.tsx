import { render, screen, within } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { errorMessage } from "../game/inventory"
import type { CharacterView, InnRoomView, InnView } from "../game/types"
import { makeCharacter } from "../test/fixtures"
import { user } from "../test/user"
import { InnPage, type InnApi } from "./InnPage"
import type { Mutate } from "./bagWidgets"

const room = (overrides: Partial<InnRoomView> = {}): InnRoomView => ({
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Exp",
  multiplier: 1.1,
  seconds: 28_800,
  price: 600,
  ...overrides,
})

const rooms = [
  room(),
  room({ id: "bulwark-room", name: "鐵壁客房", kind: "Defense", multiplier: 1.05, price: 300 }),
]

const createFakeApi = (inn: InnView = { rooms, lodging: null }): InnApi => ({
  getInn: vi.fn().mockResolvedValue(inn),
  bookInn: vi.fn().mockResolvedValue({
    character: makeCharacter({ money: 700 }),
    lodging: { roomId: "apprentice-loft", endsAt: new Date(Date.now() + 28_800_000).toISOString() },
    cost: 600,
  }),
  updateSettings: vi.fn().mockResolvedValue(makeCharacter()),
})

const renderInn = (api: InnApi, character: CharacterView = makeCharacter({ money: 1300 })) => {
  const onCharacterChanged = vi.fn()
  const mutate: Mutate = async (action) => {
    try {
      const outcome = await action()
      if (outcome.character !== undefined) onCharacterChanged(outcome.character)
      return null
    } catch (error) {
      return error instanceof ApiError ? errorMessage(error.detail, error.reasons) : "錯誤"
    }
  }
  render(<InnPage api={api} character={character} busy={false} mutate={mutate} />)
  return { onCharacterChanged }
}

test("InnPage_ListsRoomsWithEffectAndPrice", async () => {
  renderInn(createFakeApi())

  const list = await screen.findByRole("list", { name: "客房" })
  const items = within(list).getAllByRole("listitem")
  expect(items[0]).toHaveTextContent("學徒閣樓")
  expect(items[0]).toHaveTextContent("經驗 ×1.10")
  expect(items[0]).toHaveTextContent("8 小時")
  expect(items[0]).toHaveTextContent("600 金錢")
  expect(items[1]).toHaveTextContent("鐵壁客房")
  expect(screen.getByText("目前沒有住宿加成")).toBeInTheDocument()
})

test("InnPage_DisablesBookingWhenMoneyIsShort", async () => {
  renderInn(createFakeApi(), makeCharacter({ money: 599 }))

  const buttons = await screen.findAllByRole("button", { name: "入住" })
  expect(buttons[0]).toBeDisabled()
  expect(buttons[1]).toBeEnabled()
})

test("InnPage_BookingCallsApiAndUpdatesCharacter", async () => {
  const api = createFakeApi()
  const { onCharacterChanged } = renderInn(api)

  await user.click((await screen.findAllByRole("button", { name: "入住" }))[0])

  expect(api.bookInn).toHaveBeenCalledWith("apprentice-loft")
  expect(onCharacterChanged).toHaveBeenCalledWith(expect.objectContaining({ money: 700 }))
  expect(await screen.findByText(/學徒閣樓・經驗 ×1\.10（剩 8:00）/)).toBeInTheDocument()
})

test("InnPage_ShowsReplaceHintForOtherRooms", async () => {
  const lodging = {
    roomId: "apprentice-loft",
    endsAt: new Date(Date.now() + 3_600_000).toISOString(),
  }
  renderInn(createFakeApi({ rooms, lodging }))

  expect(await screen.findByText(/學徒閣樓・經驗 ×1\.10（剩 1:00）/)).toBeInTheDocument()
  expect(screen.getByText("會取代目前的學徒閣樓")).toBeInTheDocument()
  expect(screen.getAllByText(/會取代目前的/)).toHaveLength(1)
})

test("InnPage_ShowsBookingErrorReason", async () => {
  const api = createFakeApi()
  vi.mocked(api.bookInn).mockRejectedValue(new ApiError(400, "x", ["unknownRoom"]))
  renderInn(api)

  await user.click((await screen.findAllByRole("button", { name: "入住" }))[0])

  expect(await screen.findByRole("alert")).toHaveTextContent("沒有這個房間")
})

test("InnPage_SavesRestThresholdsWithTheFullSettings", async () => {
  const api = createFakeApi()
  const character = makeCharacter({
    money: 1300,
    specialAttackName: "星落",
    settings: {
      attack: "Magic",
      potionHpPercent: 20,
      potionSpPercent: 90,
      restHpPercent: 15,
      restSpPercent: 15,
      escapeHpPercent: 5,
    },
  })
  const { onCharacterChanged } = renderInn(api, character)

  const hp = await screen.findByRole("textbox", { name: "住宿 HP 門檻" })
  await user.clear(hp)
  await user.type(hp, "40")
  await user.click(screen.getByRole("button", { name: "儲存" }))

  expect(api.updateSettings).toHaveBeenCalledWith({
    attack: "Magic",
    potionHpPercent: 20,
    potionSpPercent: 90,
    restHpPercent: 40,
    restSpPercent: 15,
    escapeHpPercent: 5,
    specialAttackName: "星落",
  })
  expect(onCharacterChanged).toHaveBeenCalled()
})

test("InnPage_RejectsOutOfRangeRestThreshold", async () => {
  const api = createFakeApi()
  renderInn(api)

  const sp = await screen.findByRole("textbox", { name: "住宿 SP 門檻" })
  await user.clear(sp)
  await user.type(sp, "101")
  await user.click(screen.getByRole("button", { name: "儲存" }))

  expect(api.updateSettings).not.toHaveBeenCalled()
  expect(await screen.findByRole("alert")).toBeInTheDocument()
})
