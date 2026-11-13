import { act, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../../api/client"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter, makeMercenaryOverview, makePets } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { CompanionsScreen } from "./CompanionsScreen"
import { createCompanionsApi } from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderCompanions = async (api = createCompanionsApi(), onBack = vi.fn()) => {
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <CompanionsScreen
        api={api}
        character={makeCharacter({ money: 1000 })}
        onCharacterChange={vi.fn()}
        onBack={onBack}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack }
}

const door = (name: "獸欄" | "傭兵所") =>
  screen.getByRole("button", { name: new RegExp(`^${name}`) })

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 companions-screen：Entrance with a carried pet and a fighting mercenary
test("EntranceShowsTheKeepersLinesAndChips", async () => {
  await renderCompanions()

  expect(screen.getByTestId("mira-line")).toHaveTextContent(
    "小風今天也很有精神喔！要來看看牠們嗎？",
  )
  expect(door("獸欄")).toHaveTextContent("寵物 2 / 3・小風 攜帶中")
  expect(screen.getByTestId("barton-line")).toHaveTextContent(
    "葛雷還在迷霧森林幹活，契約我幫你顧著。",
  )
  expect(door("傭兵所")).toHaveTextContent("葛雷・戰鬥中")
})

test("EntranceWithoutPetsOrContract", async () => {
  await renderCompanions(
    createCompanionsApi(makePets([]), makeMercenaryOverview({ contract: null })),
  )

  expect(screen.getByTestId("mira-line")).toHaveTextContent(
    "獸欄空蕩蕩的……抓到寵物就帶來給我照顧吧！",
  )
  expect(door("獸欄")).toHaveTextContent("寵物 0 / 3")
  expect(screen.getByTestId("barton-line")).toHaveTextContent("想找人手？名冊上的傢伙個個都能打。")
  expect(door("傭兵所")).toHaveTextContent("沒有契約")
})

// 規格 companions-screen：Back from the stable／Back from the entrance
test("DoorOpensTheStableAndBackReturns", async () => {
  const { onBack } = await renderCompanions()

  fireEvent.click(door("獸欄"))
  await flush()
  expect(screen.getByRole("region", { name: "獸欄" })).toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "同伴" }))
  expect(screen.queryByRole("region", { name: "獸欄" })).not.toBeInTheDocument()
  expect(door("傭兵所")).toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "城門" }))
  expect(onBack).toHaveBeenCalledTimes(1)
})

// 規格 companions-screen：Companions load failure（入口）
test("EntranceLoadFailureShowsTheRetrySeal", async () => {
  const api = createCompanionsApi()
  vi.mocked(api.getMercenaries).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderCompanions(api)

  expect(screen.getByTestId("mira-line")).toHaveTextContent("咦，獸欄的名簿一下子找不到了……")
  expect(screen.getByTestId("barton-line")).toHaveTextContent("嘖，名冊一時翻不到……")
  expect(door("獸欄")).not.toHaveTextContent("寵物")
  expect(screen.getByRole("button", { name: "再試一次" })).toBeInTheDocument()
  expect(door("傭兵所")).toBeEnabled()
})

// 規格 companions-screen：Retry at the entrance
test("EntranceRetryRestoresTheGreetings", async () => {
  const api = createCompanionsApi()
  vi.mocked(api.getMercenaries).mockRejectedValueOnce(new TypeError("Failed to fetch"))
  await renderCompanions(api)

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(screen.getByTestId("barton-line")).toHaveTextContent(
    "葛雷還在迷霧森林幹活，契約我幫你顧著。",
  )
  expect(door("傭兵所")).toHaveTextContent("葛雷・戰鬥中")
  expect(screen.queryByRole("button", { name: "再試一次" })).not.toBeInTheDocument()
})

// 規格 companions-screen：Enter and leave（不呼叫冒險推進）
test("MakesNoAdventureRequests", async () => {
  const { api } = await renderCompanions()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 5)
  })

  expect((api as unknown as Pick<ApiClient, "advance">).advance).not.toHaveBeenCalled()
})
