import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { useState } from "react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError, type ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { InnBookVo, InnVo, type InnRoom, type Lodging } from "../../game/inn"
import { InnThresholdSaveDelayMilliseconds, PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { InnScreen, type InnApi } from "./InnScreen"

const at = (hour: number, minute = 0) => new Date(Date.UTC(2026, 10, 12, hour, minute))

const room = (
  id: string,
  name: string,
  kind: InnRoom["kind"],
  multiplier: number,
  price: number,
): InnRoom => ({
  id,
  name,
  kind,
  multiplier,
  seconds: 28_800,
  price,
})

const rooms = [
  room("apprentice-loft", "學徒閣樓", "Experience", 1.1, 600),
  room("training-chamber", "修練廂房", "JobExperience", 1.1, 600),
  room("fighters-bunk", "鬥士通鋪", "Attack", 1.05, 300),
  room("stargazer-tower", "星見塔房", "Magic", 1.05, 300),
  room("bulwark-room", "鐵壁客房", "Defense", 1.05, 300),
  room("quiet-chapel", "靜謐禱室", "MagicDefense", 1.05, 300),
]

const innWith = (lodging: Lodging | null, restFee = 0) => InnVo.create({ rooms, lodging, restFee })

const character = makeCharacter({ level: 6, money: 1000 })

const createApi = (inn = innWith(null)): InnApi & Pick<ApiClient, "advance"> => ({
  getInn: vi.fn().mockResolvedValue(inn),
  bookInn: vi.fn(),
  updateSettings: vi.fn(async (settings: CharacterVo["settings"]) => ({ ...character, settings })),
  getMyCharacter: vi.fn(),
  advance: vi.fn(),
})

// 角色由外層（GameScreen）保管：入住成功後換成回應的角色
const Harness = ({ api, initial }: { api: InnApi; initial: CharacterVo }) => {
  const [current, setCurrent] = useState(initial)
  return (
    <StageContext value={{ height: 624, scale: 1 }}>
      <InnScreen api={api} character={current} onCharacterChange={setCurrent} onBack={vi.fn()} />
    </StageContext>
  )
}

const renderInn = async ({
  api = createApi(),
  initial = character,
}: { api?: ReturnType<typeof createApi>; initial?: CharacterVo } = {}) => {
  render(<Harness api={api} initial={initial} />)
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })
  return api
}

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const bubble = () => screen.getByTestId("innkeeper-line")
const banner = () => screen.getByRole("region", { name: "目前入住" })
const roomCard = (name: string) => screen.getByRole("button", { name: new RegExp(`^${name}`) })
const sheet = () => screen.getByRole("dialog", { name: "入住" })
const seal = () => within(sheet()).getByRole("button", { name: "入住" })

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
  vi.setSystemTime(at(8, 1))
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 inn-screen：Banner for an active stay
test("InnScreen_ShowsTheActiveStay", async () => {
  await renderInn({
    api: createApi(innWith({ roomId: "apprentice-loft", startsAt: at(8), endsAt: at(16) })),
  })

  expect(within(banner()).getByText("學徒閣樓")).toBeInTheDocument()
  expect(within(banner()).getByText("經驗 ×1.10")).toBeInTheDocument()
  expect(within(banner()).getByText("剩 7:59")).toBeInTheDocument()
  expect(bubble()).toHaveTextContent("房間還幫你留著呢。要換一間，還是續住？")
})

test("InnScreen_ShowsNoStay", async () => {
  await renderInn()

  expect(banner()).toHaveTextContent("目前沒有住宿加成")
  expect(bubble()).toHaveTextContent("歡迎光臨暖爐亭！今晚想住哪一間？")
})

// 規格 inn-screen：Room cards
test("InnScreen_RoomCardsShowLengthAndPrice", async () => {
  await renderInn({
    api: createApi(innWith({ roomId: "fighters-bunk", startsAt: at(8), endsAt: at(16) })),
  })

  const list = screen.getByRole("list", { name: "客房" })
  expect(within(list).getAllByRole("button")).toHaveLength(6)
  const loft = roomCard("學徒閣樓")
  expect(loft).toHaveTextContent("經驗 ×1.10")
  expect(loft).toHaveTextContent("8 小時")
  expect(loft).toHaveTextContent("600")
  expect(roomCard("鬥士通鋪")).toHaveTextContent("入住中")
  expect(loft).not.toHaveTextContent("入住中")
})

// 規格 inn-screen：Replacing another room
test("InnScreen_SheetShowsTheReplacement", async () => {
  await renderInn({
    api: createApi(innWith({ roomId: "fighters-bunk", startsAt: at(1, 43), endsAt: at(9, 43) })),
  })

  fireEvent.click(roomCard("學徒閣樓"))

  expect(sheet()).toHaveTextContent("會取代目前的鬥士通鋪（物攻 ×1.05，剩 1:42）")
})

// 規格 inn-screen：Not enough money in the sheet
test("InnScreen_SealDisabledWithoutMoney", async () => {
  await renderInn({ initial: makeCharacter({ level: 6, money: 500 }) })

  fireEvent.click(roomCard("學徒閣樓"))

  expect(seal()).toBeDisabled()
  expect(within(sheet()).getByTestId("inn-cost")).toHaveAttribute("data-short", "true")
})

// 規格 inn-screen：Booking succeeds
test("InnScreen_BookingUpdatesBannerMoneyAndLine", async () => {
  const api = createApi()
  vi.mocked(api.bookInn).mockResolvedValue(
    InnBookVo.create({
      character: makeCharacter({ level: 6, money: 400 }),
      lodging: { roomId: "apprentice-loft", startsAt: at(8, 1), endsAt: at(16, 1) },
      cost: 600,
    }),
  )
  await renderInn({ api })

  fireEvent.click(roomCard("學徒閣樓"))
  fireEvent.click(seal())
  await flush()

  expect(api.bookInn).toHaveBeenCalledWith("apprentice-loft")
  expect(within(banner()).getByText("學徒閣樓")).toBeInTheDocument()
  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("400")
  expect(screen.queryByRole("dialog", { name: "入住" })).not.toBeInTheDocument()
  expect(bubble()).toHaveTextContent("學徒閣樓收拾好了，好好睡一覺吧！")
})

// 規格 inn-screen：Money spent elsewhere
test("InnScreen_NotEnoughMoneyReloadsTheCharacter", async () => {
  const api = createApi()
  vi.mocked(api.bookInn).mockRejectedValue(new ApiError(400, "錢不夠", ["notEnoughMoney"]))
  vi.mocked(api.getMyCharacter).mockResolvedValue(makeCharacter({ level: 6, money: 500 }))
  await renderInn({ api })

  fireEvent.click(roomCard("學徒閣樓"))
  fireEvent.click(seal())
  await flush()

  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("500")
  expect(seal()).toBeDisabled()
  expect(bubble()).toHaveTextContent("哎呀，這間的錢好像還差一點呢。")
})

// 規格 inn-screen：Connection failure
test("InnScreen_OtherFailureKeepsTheSheet", async () => {
  const api = createApi()
  vi.mocked(api.bookInn).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderInn({ api })

  fireEvent.click(roomCard("學徒閣樓"))
  fireEvent.click(seal())
  await flush()

  expect(seal()).toBeEnabled()
  expect(bubble()).toHaveTextContent("咦，登記簿沒寫上去……等會兒再來吧。")
  expect(api.getMyCharacter).not.toHaveBeenCalled()
})

// 規格 inn-screen：No adventure requests in the inn
test("InnScreen_MakesNoAdventureRequests", async () => {
  const api = await renderInn()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 5)
  })

  expect(api.advance).not.toHaveBeenCalled()
})

const withThresholds = (hp: number, sp: number, overrides: Partial<CharacterVo> = {}) => {
  const base = makeCharacter({ level: 6, money: 1000, ...overrides })
  return {
    ...base,
    settings: { ...base.settings, restHealthPointsPercent: hp, restManaPointsPercent: sp },
  }
}

const openAutoRest = () => fireEvent.click(screen.getByRole("tab", { name: "自動住宿" }))
const thresholdText = (label: "HP" | "SP") => screen.getByTestId(`threshold-${label.toLowerCase()}`)
const step = (label: "HP" | "SP", direction: "增加" | "減少") =>
  fireEvent.click(screen.getByRole("button", { name: `${label} 門檻${direction}` }))

// 規格 inn-screen：Stepping a threshold——畫面上立刻換
test("InnScreen_ThresholdStepsShowAtOnce", async () => {
  await renderInn({ initial: withThresholds(33, -1) })
  openAutoRest()

  step("HP", "增加")

  expect(thresholdText("HP")).toHaveTextContent("35%")
  expect(thresholdText("SP")).toHaveTextContent("略過")
  expect(screen.getByRole("button", { name: "SP 門檻減少" })).toBeDisabled()
})

// 規格 inn-screen：Several steps save once
test("InnScreen_SeveralStepsSaveOnce", async () => {
  const api = createApi()
  const initial = withThresholds(15, 15)
  vi.mocked(api.updateSettings).mockImplementation(async (settings) => ({ ...initial, settings }))
  await renderInn({ api, initial })
  openAutoRest()

  step("HP", "增加")
  step("HP", "增加")
  step("HP", "增加")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })

  expect(api.updateSettings).toHaveBeenCalledTimes(1)
  expect(api.updateSettings).toHaveBeenCalledWith({
    ...initial.settings,
    restHealthPointsPercent: 30,
  })
  expect(thresholdText("HP")).toHaveTextContent("30%")
})

// 規格 inn-screen：Saving fails
test("InnScreen_SavingFailureRestoresTheThreshold", async () => {
  const api = createApi()
  vi.mocked(api.updateSettings).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderInn({ api, initial: withThresholds(15, 15) })
  openAutoRest()

  step("HP", "增加")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })

  expect(thresholdText("HP")).toHaveTextContent("15%")
  expect(bubble()).toHaveTextContent("剛剛那個沒記下來，再調一次好嗎？")
})

// 離開旅館時還沒送出的修改立刻送
test("InnScreen_LeavingSavesAPendingStep", async () => {
  const api = createApi()
  const initial = withThresholds(15, 15)
  vi.mocked(api.updateSettings).mockImplementation(async (settings) => ({ ...initial, settings }))
  const { unmount } = render(<Harness api={api} initial={initial} />)
  await flush()
  openAutoRest()

  step("SP", "減少")
  unmount()

  expect(api.updateSettings).toHaveBeenCalledWith({
    ...initial.settings,
    restManaPointsPercent: 10,
  })
})

// 規格 inn-screen：Rest fee text
test.each([
  [6, 0, "Lv 6", "免費"],
  [120, 1200, "Lv 120", "1,200 起"],
])("InnScreen_RestFeeShowsFreeOrFrom %i", async (level, fee, levelText, feeText) => {
  await renderInn({
    api: createApi(innWith(null, fee)),
    initial: withThresholds(15, 15, { level }),
  })
  openAutoRest()

  const box = screen.getByRole("group", { name: "住宿費" })
  expect(box).toHaveTextContent(levelText)
  expect(box).toHaveTextContent(`自動住宿一次${feeText}`)
})

// 規格 inn-screen：Inn load failure——木台上只剩「再試一次」
test("InnScreen_LoadFailureShowsOnlyTheRetrySeal", async () => {
  const api = createApi()
  vi.mocked(api.getInn).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderInn({ api })

  expect(screen.getByRole("button", { name: "再試一次" })).toBeInTheDocument()
  expect(screen.queryByRole("tablist")).not.toBeInTheDocument()
  expect(screen.queryByRole("region", { name: "目前入住" })).not.toBeInTheDocument()
  expect(bubble()).toHaveTextContent("哎呀，今天的房帳一時找不到……")
})

// 規格 inn-screen：Retry after a failed load
test("InnScreen_RetryShowsTheRooms", async () => {
  const api = createApi()
  vi.mocked(api.getInn)
    .mockRejectedValueOnce(new TypeError("Failed to fetch"))
    .mockResolvedValue(innWith(null))
  await renderInn({ api })

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(within(screen.getByRole("list", { name: "客房" })).getAllByRole("button")).toHaveLength(6)
  expect(screen.queryByRole("button", { name: "再試一次" })).not.toBeInTheDocument()
  expect(bubble()).toHaveTextContent("歡迎光臨暖爐亭！今晚想住哪一間？")
})

// 剛入住時剩餘不會超過 8 小時：伺服器的開始時間比畫面的「現在」晚 30 秒
test("InnScreen_JustBookedStayShowsItsFullLength", async () => {
  const api = createApi()
  vi.mocked(api.bookInn).mockResolvedValue(
    InnBookVo.create({
      character: makeCharacter({ level: 6, money: 400 }),
      lodging: {
        roomId: "apprentice-loft",
        startsAt: new Date(at(8, 1).getTime() + 30_000),
        endsAt: new Date(at(16, 1).getTime() + 30_000),
      },
      cost: 600,
    }),
  )
  await renderInn({ api })

  fireEvent.click(roomCard("學徒閣樓"))
  fireEvent.click(seal())
  await flush()

  expect(within(banner()).getByText("剩 8:00")).toBeInTheDocument()
})

const openRooms = () => fireEvent.click(screen.getByRole("tab", { name: "客房" }))

// review 補修：切到客房再切回來，畫面要是剛存的值，下一次存檔不會把它改回去
test("InnScreen_SwitchingTabsKeepsTheSavedThreshold", async () => {
  const api = createApi()
  const initial = withThresholds(30, 15)
  vi.mocked(api.updateSettings).mockImplementation(async (settings) => ({ ...initial, settings }))
  await renderInn({ api, initial })
  openAutoRest()

  step("HP", "增加")
  openRooms()
  openAutoRest()
  await flush()

  expect(thresholdText("HP")).toHaveTextContent("35%")
  step("SP", "增加")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })
  expect(api.updateSettings).toHaveBeenLastCalledWith({
    ...initial.settings,
    restHealthPointsPercent: 35,
    restManaPointsPercent: 20,
  })
})

// review 補修：切走後才失敗也要由瑪莎說，數值退回
test("InnScreen_SaveFailureAfterSwitchingTabsIsSpoken", async () => {
  const api = createApi()
  vi.mocked(api.updateSettings).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderInn({ api, initial: withThresholds(15, 15) })
  openAutoRest()

  step("HP", "增加")
  openRooms()
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })

  expect(bubble()).toHaveTextContent("剛剛那個沒記下來，再調一次好嗎？")
  openAutoRest()
  expect(thresholdText("HP")).toHaveTextContent("15%")
})

// verify 補修：按「市集」的那一刻就送出還沒送的修改
test("InnScreen_BackSendsAPendingStepAtOnce", async () => {
  const api = createApi()
  const initial = withThresholds(15, 15)
  await renderInn({ api, initial })
  openAutoRest()

  step("HP", "增加")
  fireEvent.click(screen.getByRole("button", { name: "市集" }))

  expect(api.updateSettings).toHaveBeenCalledWith({
    ...initial.settings,
    restHealthPointsPercent: 20,
  })
})

// review 補修：較早的存檔成功、較新的失敗時，旅館畫面退回伺服器實際存的值
test("InnScreen_NewerFailureShowsTheSavedThreshold", async () => {
  const api = createApi()
  const initial = withThresholds(15, 15)
  const calls: { resolve: () => void; reject: (error: Error) => void }[] = []
  vi.mocked(api.updateSettings).mockImplementation(
    (settings) =>
      new Promise((resolve, reject) =>
        calls.push({ resolve: () => resolve({ ...initial, settings }), reject }),
      ),
  )
  await renderInn({ api, initial })
  openAutoRest()

  step("HP", "增加")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })
  step("HP", "增加")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })
  await act(async () => calls[0]!.resolve())
  await act(async () => calls[1]!.reject(new TypeError("Failed to fetch")))

  expect(thresholdText("HP")).toHaveTextContent("20%")
  expect(bubble()).toHaveTextContent("剛剛那個沒記下來，再調一次好嗎？")
})
