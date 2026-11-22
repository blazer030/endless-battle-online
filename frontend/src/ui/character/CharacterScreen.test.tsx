import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { CharacterScreen, type CharacterScreenApi } from "./CharacterScreen"
import { createCharacterApi, Now, warrior } from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

type Options = {
  api?: CharacterScreenApi
  onBack?: () => void
  onCharacterChange?: () => void
  onSignOut?: () => void
  onOpenJobChange?: () => void
  height?: number
}

const renderCharacter = async ({
  api = createCharacterApi(),
  onBack = vi.fn(),
  onCharacterChange = vi.fn(),
  onSignOut = vi.fn(),
  onOpenJobChange = vi.fn(),
  height = 624,
}: Options = {}) => {
  render(
    <StageContext value={{ height, scale: 1 }}>
      <CharacterScreen
        api={api}
        character={warrior}
        onCharacterChange={onCharacterChange}
        onBack={onBack}
        onSignOut={onSignOut}
        onOpenJobChange={onOpenJobChange}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack, onCharacterChange, onSignOut, onOpenJobChange }
}

const card = () => screen.getByRole("group", { name: "冒險者登記證" })

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
  vi.setSystemTime(Now)
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 character-screen「Loading and load failure」：打開時讀六個 API
test("LoadsSixRequestsOnOpen", async () => {
  const { api } = await renderCharacter()

  for (const call of [
    api.getMyCharacter,
    api.getAccount,
    api.getBuffs,
    api.getInn,
    api.getSkills,
    api.getCreationOptions,
  ]) {
    expect(call).toHaveBeenCalledTimes(1)
  }
})

// 規格「Loading」：登記證不寫字、手帳畫空白線、立繪不出
test("LoadingShowsFaintBlocks", async () => {
  await renderCharacter({
    api: createCharacterApi({ getSkills: vi.fn(() => new Promise<never>(() => {})) }),
  })

  expect(card()).toHaveAttribute("aria-busy", "true")
  expect(within(card()).queryByText("收尾者")).toBeNull()
  expect(screen.getByLabelText("讀取中")).toBeInTheDocument()
  expect(document.querySelector(".character__portrait img")).toBeNull()
  expect(screen.getByLabelText("持有金錢")).not.toHaveTextContent("128,400")
})

// 任何一個失敗就整頁讀取失敗；回 null（沒有角色）也算
test("AnyFailureShowsRetry", async () => {
  await renderCharacter({
    api: createCharacterApi({ getMyCharacter: vi.fn().mockResolvedValue(null) }),
  })

  expect(screen.getByText("登記證的資料讀不到。")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "再試一次" })).toBeInTheDocument()
})

// 規格「Retry after a failure」
test("RetryLoadsAgain", async () => {
  const getSkills = vi.fn().mockRejectedValueOnce(new Error("斷線"))
  const api = createCharacterApi()
  getSkills.mockImplementation(api.getSkills)
  await renderCharacter({ api: { ...api, getSkills } })

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(getSkills).toHaveBeenCalledTimes(2)
  expect(api.getMyCharacter).toHaveBeenCalledTimes(2)
  expect(within(card()).getByText("收尾者")).toBeInTheDocument()
  expect(screen.queryByText("登記證的資料讀不到。")).toBeNull()
})

// 規格「Card values」：用讀回來的角色
test("CardShowsTheCharacter", async () => {
  await renderCharacter()

  const registration = card()
  for (const text of [
    "收尾者",
    "戰士",
    "Lv 162",
    "70%",
    "2,450 / 3,500",
    "男",
    "火",
    "中央大陸",
    "6,212 / 9,640",
    "980 / 1,205",
    "42%",
  ]) {
    expect(within(registration).getByText(text)).toBeInTheDocument()
  }
  expect(within(registration).getByRole("button", { name: "轉職" })).toBeInTheDocument()
})

// 規格「Portrait by sex」
test("PortraitBySex", async () => {
  await renderCharacter({
    api: createCharacterApi({
      getMyCharacter: vi
        .fn()
        .mockResolvedValue({ ...warrior, jobId: "job-7", jobName: "術士", sex: 2 }),
    }),
  })

  expect(document.querySelector(".character__portrait img")).toHaveAttribute(
    "data-asset",
    "portraits/job-7-female",
  )
})

// 規格「Job without a portrait」：清單沒有這個職業的立繪時顯示職業圖示＋立繪待補
test("JobWithoutPortraitShowsPlaceholder", async () => {
  await renderCharacter({
    api: createCharacterApi({
      getMyCharacter: vi.fn().mockResolvedValue({ ...warrior, jobId: "job-10", jobName: "狂戰士" }),
    }),
  })

  const portrait = document.querySelector(".character__portrait")!
  expect(portrait).toHaveTextContent("立繪待補")
  expect(portrait.querySelector("img")).toHaveAttribute("data-asset", "job-icons/job-10")
})

// 規格「Wallet」
test("WalletShowsMoney", async () => {
  await renderCharacter()

  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("128,400")
})

// 規格「Open from the gate」：先停能力頁
test("OpensOnTheAbilitiesPage", async () => {
  await renderCharacter()

  expect(screen.getByRole("tab", { name: "能力" })).toHaveAttribute("aria-selected", "true")
  expect(screen.getByRole("tab", { name: "技能" })).toHaveAttribute("aria-selected", "false")
  fireEvent.click(screen.getByRole("tab", { name: "技能" }))
  expect(screen.getByRole("tab", { name: "技能" })).toHaveAttribute("aria-selected", "true")
})

// 「城鎮」返回；讀到的角色交回外層
test("BackReturnsToTown", async () => {
  const latest = makeCharacter({ ...warrior, money: 99 })
  const { onBack, onCharacterChange } = await renderCharacter({
    api: createCharacterApi({ getMyCharacter: vi.fn().mockResolvedValue(latest) }),
  })

  fireEvent.click(screen.getByRole("button", { name: "城鎮" }))

  expect(onBack).toHaveBeenCalledTimes(1)
  expect(onCharacterChange).toHaveBeenCalledWith(latest)
})

// 「轉職」交出角色給外層開舊外殼技能分頁
test("JobChangeButtonCallsBack", async () => {
  const { onOpenJobChange } = await renderCharacter()

  fireEvent.click(within(card()).getByRole("button", { name: "轉職" }))

  expect(onOpenJobChange).toHaveBeenCalledTimes(1)
})
