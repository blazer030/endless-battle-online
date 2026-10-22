import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { makeHonourShop, makeMercenaryOverview } from "../test/fixtures"
import { HonourPage, type HonourApi } from "./HonourPage"

const createFakeApi = (overrides: Partial<HonourApi> = {}): HonourApi => ({
  getHonourShop: vi.fn().mockResolvedValue(makeHonourShop()),
  getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview()),
  exchangeHonour: vi.fn(),
  ...overrides,
})

const renderHonour = (api: HonourApi = createFakeApi()) => {
  const onExchanged = vi.fn().mockResolvedValue(undefined)
  render(<HonourPage api={api} onExchanged={onExchanged} />)
  return { api, onExchanged }
}

test("HonourPage_ListsHeldMedals", async () => {
  renderHonour()

  const held = within(await screen.findByRole("list", { name: "持有勳章" }))
    .getAllByRole("listitem")
    .map((item) => item.textContent)
  expect(held).toEqual(["白銀勳章 ×0", "大陸勳章 ×2", "王國勳章 ×0", "雨林勳章 ×0", "王城勳章 ×0"])
})

test("HonourPage_GroupsExchangesAndDisablesUnaffordable", async () => {
  renderHonour()

  const mercenaries = await screen.findByRole("region", { name: "傭兵" })
  const mercenary = within(mercenaries).getByRole("article", { name: "B級傭兵 ×1" })
  expect(within(mercenary).getByRole("button", { name: "兌換" })).toBeDisabled()
  const costs = within(mercenary).getAllByRole("listitem")
  expect(costs.map((cost) => cost.textContent)).toEqual(["大陸勳章 ×3", "王國勳章 ×3"])
  expect(costs[0]).toHaveClass("honour__cost--short")

  const items = screen.getByRole("region", { name: "道具" })
  const stone = within(items).getByRole("article", { name: "攻擊魔石 ×1" })
  expect(within(stone).getByRole("listitem")).not.toHaveClass("honour__cost--short")
  expect(within(stone).getByRole("button", { name: "兌換" })).toBeEnabled()
})

test("HonourPage_ExchangeRedrawsFromTheResponse", async () => {
  const spent = makeHonourShop({
    medals: makeHonourShop().medals.map((medal) => ({ ...medal, owned: 0 })),
    exchanges: makeHonourShop().exchanges.map((exchange) => ({
      ...exchange,
      cost: exchange.cost.map((cost) => ({ ...cost, owned: 0 })),
      affordable: false,
    })),
  })
  const api = createFakeApi({ exchangeHonour: vi.fn().mockResolvedValue(spent) })
  const { onExchanged } = renderHonour(api)

  const stone = await screen.findByRole("article", { name: "攻擊魔石 ×1" })
  await userEvent.click(within(stone).getByRole("button", { name: "兌換" }))

  expect(api.exchangeHonour).toHaveBeenCalledWith("honour-attack-stone")
  expect(onExchanged).toHaveBeenCalledTimes(1)
  const held = screen.getByRole("list", { name: "持有勳章" })
  expect(await within(held).findByText("大陸勳章 ×0")).toBeInTheDocument()
  expect(
    within(screen.getByRole("article", { name: "攻擊魔石 ×1" })).getByRole("button", {
      name: "兌換",
    }),
  ).toBeDisabled()
})

test("HonourPage_ShowsMappedError", async () => {
  const api = createFakeApi({
    exchangeHonour: vi.fn().mockRejectedValue(new ApiError(400, "無法兌換", ["medalsMissing"])),
  })
  const { onExchanged } = renderHonour(api)

  const stone = await screen.findByRole("article", { name: "攻擊魔石 ×1" })
  await userEvent.click(within(stone).getByRole("button", { name: "兌換" }))

  expect(within(stone).getByRole("alert")).toHaveTextContent("勳章不足")
  expect(onExchanged).not.toHaveBeenCalled()
})

test("HonourPage_MercenaryExchangeDisabledWhileHired", async () => {
  const contract = {
    mercenaryId: "mercenary-f",
    name: "阿福",
    remaining: 97,
    placeId: null,
    placeName: null,
    fighting: false,
    nextFightAt: null,
  }
  const affordable = makeHonourShop()
  const api = createFakeApi({
    getHonourShop: vi.fn().mockResolvedValue({
      ...affordable,
      exchanges: affordable.exchanges.map((exchange) => ({ ...exchange, affordable: true })),
    }),
    getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview({ contract })),
  })
  renderHonour(api)

  const mercenary = await screen.findByRole("article", { name: "B級傭兵 ×1" })
  await waitFor(() =>
    expect(within(mercenary).getByRole("button", { name: "兌換" })).toBeDisabled(),
  )
  const stone = screen.getByRole("article", { name: "攻擊魔石 ×1" })
  expect(within(stone).getByRole("button", { name: "兌換" })).toBeEnabled()
})
