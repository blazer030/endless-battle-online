import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { HonourScreen } from "./HonourScreen"
import { createHonourApi, medals, shopWith } from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderHonour = async (api = createHonourApi(), onBack = vi.fn()) => {
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <HonourScreen
        api={api}
        character={makeCharacter()}
        onCharacterChange={vi.fn()}
        onBack={onBack}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack }
}

const medalCase = () => screen.getByRole("group", { name: "持有勳章" })
const medalButton = (name: string) =>
  within(medalCase()).getByRole("button", { name: new RegExp(`^${name}`) })
const clerk = () => screen.getByTestId("clerk-line")

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 honour-screen：Counter with medals and a contract
test("CounterShowsMedalCountsAndGreeting", async () => {
  await renderHonour(createHonourApi(shopWith({ hiredMercenaryName: "葛雷" })))

  expect(
    within(medalCase())
      .getAllByRole("button")
      .map((button) => button.textContent),
  ).toEqual(["14", "6", "3", "0", "2"])
  expect(screen.getByText("僱用中").parentElement).toHaveTextContent("僱用中葛雷")
  expect(clerk().textContent).toBe("歡迎來到勳章所！\n今天想兌換些什麼呢？")
})

// 規格 honour-screen：Counter without medals
test("CounterWithoutMedals", async () => {
  await renderHonour(
    createHonourApi(shopWith({ medals: medals.map((medal) => ({ ...medal, owned: 0 })) })),
  )

  expect(clerk().textContent).toBe("啊，您還沒有勳章呢……\n有了再來找我喔！")
  expect(medalButton("大陸勳章")).toBeEnabled()
  expect(screen.queryByText("僱用中")).not.toBeInTheDocument()
})

// 規格 honour-screen：Back from the rack and the counter
test("MedalLowersTheRackAndBackReturns", async () => {
  const { onBack } = await renderHonour()

  fireEvent.click(medalButton("王國勳章"))
  expect(screen.getByRole("region", { name: "王國勳章陳列板" })).toBeVisible()
  expect(screen.queryByTestId("clerk-line")).not.toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  expect(screen.queryByRole("region", { name: /陳列板$/ })).not.toBeInTheDocument()
  expect(clerk().textContent).toBe("慢慢挑沒關係，\n陳列板不會跑掉的～")

  fireEvent.click(screen.getByRole("button", { name: "公會前" }))
  expect(onBack).toHaveBeenCalledOnce()
})

// 規格 honour-screen：Honour loading and load failure
test("LoadingHidesCountsAndBlocksMedals", async () => {
  const api = createHonourApi()
  api.getHonourShop = vi.fn().mockReturnValue(new Promise(() => undefined))
  await renderHonour(api)

  expect(clerk().textContent).toBe("歡迎來到勳章所！\n我翻一下您的帳冊喔……")
  const buttons = within(medalCase()).getAllByRole("button")
  expect(buttons).toHaveLength(5)
  buttons.forEach((button) => {
    expect(button).toBeDisabled()
    expect(button.textContent?.trim()).toBe("")
  })
})

// 規格 honour-screen：Retry after a load failure
test("RetryAfterLoadFailure", async () => {
  const api = createHonourApi()
  api.getHonourShop = vi
    .fn()
    .mockRejectedValueOnce(new Error("offline"))
    .mockResolvedValueOnce(shopWith())
  await renderHonour(api)

  expect(clerk().textContent).toBe("哎呀，帳冊一時\n翻不到了……")
  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(screen.queryByRole("button", { name: "再試一次" })).not.toBeInTheDocument()
  expect(medalButton("大陸勳章")).toHaveTextContent("14")
  expect(clerk().textContent).toBe("歡迎來到勳章所！\n今天想兌換些什麼呢？")
})

// 規格 honour-screen：No adventure requests
test("MakesNoAdventureRequests", async () => {
  const { api } = await renderHonour()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 3)
  })

  expect(api.advance).not.toHaveBeenCalled()
})
