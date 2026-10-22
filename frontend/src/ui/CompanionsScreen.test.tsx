import { render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { MercenaryContract } from "../game/types"
import {
  makeCharacter,
  makeMercenaryOverview,
  makePet,
  makePets,
  makeWorld,
} from "../test/fixtures"
import { CompanionsScreen, type CompanionsApi } from "./CompanionsScreen"

const past = "2000-01-01T00:00:00Z"
const future = "2099-01-01T00:00:00Z"

const slime = makePet({ id: 1, name: "小黏", species: "草原史萊姆", age: 2, carried: true })
const wolf = makePet({
  id: 2,
  monsterId: "test-wolf",
  name: "寒狼",
  species: "測試寒狼",
  age: 0,
  personality: "Ironwall",
  attack: 30,
  magic: 4,
  defense: 22,
  spirit: 5,
  satiety: 64,
  intimacy: 12,
  nextFeedAt: past,
})

const resting: MercenaryContract = {
  mercenaryId: "mercenary-f",
  name: "阿福",
  remaining: 97,
  placeId: null,
  placeName: null,
  fighting: false,
  nextFightAt: null,
}

const fighting: MercenaryContract = {
  ...resting,
  placeId: 1,
  placeName: "中央平原",
  fighting: true,
  nextFightAt: "2026-10-21T00:01:40Z",
}

const createFakeApi = (overrides: Partial<CompanionsApi> = {}): CompanionsApi => ({
  getPets: vi.fn().mockResolvedValue(makePets([slime, wolf])),
  carryPet: vi.fn(),
  storePet: vi.fn(),
  releasePet: vi.fn(),
  renamePet: vi.fn(),
  feedPet: vi.fn(),
  getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview()),
  hireMercenary: vi.fn(),
  deployMercenary: vi.fn(),
  restMercenary: vi.fn(),
  renameMercenary: vi.fn(),
  dismissMercenary: vi.fn(),
  getWorld: vi.fn().mockResolvedValue(makeWorld()),
  ...overrides,
})

const renderCompanions = (api: CompanionsApi = createFakeApi()) => {
  const onCharacterChanged = vi.fn()
  render(<CompanionsScreen api={api} onCharacterChanged={onCharacterChanged} />)
  return { api, onCharacterChanged }
}

const card = (name: string) => screen.findByRole("article", { name })

test("CompanionsScreen_PetsPageListsCardsAndCount", async () => {
  renderCompanions()

  const tabs = within(await screen.findByRole("tablist", { name: "同伴分頁" })).getAllByRole("tab")
  expect(tabs.map((tab) => tab.textContent)).toEqual(["寵物", "傭兵"])
  expect(await screen.findByRole("heading", { name: "寵物 2/3" })).toBeInTheDocument()
  expect(screen.getByText("餵食初級須花費20元，中級須花費50元")).toBeInTheDocument()
  expect(
    screen.getByText("選擇攜帶時，寵物會根據親密值及主人魅力值，隨機出現支援戰鬥或抵擋敵人攻擊"),
  ).toBeInTheDocument()

  const carried = await card("小黏－草原史萊姆 2歲")
  expect(within(carried).getByText("攜帶中")).toBeInTheDocument()
  expect(within(carried).getByRole("button", { name: "放回獸欄" })).toBeInTheDocument()

  const stabled = await card("寒狼－測試寒狼 0歲")
  expect(within(stabled).getByText("獸欄")).toBeInTheDocument()
  const stats = within(within(stabled).getByRole("list", { name: "能力" }))
    .getAllByRole("listitem")
    .map((item) => item.textContent)
  expect(stats).toEqual(["AT30", "MT4", "DEF22", "飽腹64", "個性鐵壁", "出擊值5", "親密度12"])
})

test("CompanionsScreen_ShowsEmptyPetsHint", async () => {
  renderCompanions(createFakeApi({ getPets: vi.fn().mockResolvedValue(makePets()) }))

  expect(
    await screen.findByText("還沒有寵物。在補給欄裝上捕獸道具，打贏時有機會捕捉。"),
  ).toBeInTheDocument()
  expect(screen.getByRole("heading", { name: "寵物 0/3" })).toBeInTheDocument()
})

test("CompanionsScreen_CarryCallsApiAndRedraws", async () => {
  const api = createFakeApi({
    carryPet: vi.fn().mockResolvedValue(
      makePets([
        { ...slime, carried: false },
        { ...wolf, carried: true },
      ]),
    ),
  })
  renderCompanions(api)

  await userEvent.click(
    within(await card("寒狼－測試寒狼 0歲")).getByRole("button", { name: "攜帶" }),
  )

  expect(api.carryPet).toHaveBeenCalledWith(2)
  expect(within(await card("寒狼－測試寒狼 0歲")).getByText("攜帶中")).toBeInTheDocument()
  expect(within(await card("小黏－草原史萊姆 2歲")).getByText("獸欄")).toBeInTheDocument()
})

test("CompanionsScreen_StoreCallsApi", async () => {
  const api = createFakeApi({
    storePet: vi.fn().mockResolvedValue(makePets([{ ...slime, carried: false }, wolf])),
  })
  renderCompanions(api)

  await userEvent.click(
    within(await card("小黏－草原史萊姆 2歲")).getByRole("button", { name: "放回獸欄" }),
  )

  expect(api.storePet).toHaveBeenCalledWith(1)
  expect(within(await card("小黏－草原史萊姆 2歲")).getByText("獸欄")).toBeInTheDocument()
})

test("CompanionsScreen_FeedSendsChosenFoodAndUpdatesCharacter", async () => {
  const fed = makeCharacter({ money: 950 })
  const api = createFakeApi({
    feedPet: vi.fn().mockResolvedValue({
      pets: makePets([slime, { ...wolf, magic: 8, nextFeedAt: future }]),
      character: fed,
    }),
  })
  const { onCharacterChanged } = renderCompanions(api)
  const stabled = await card("寒狼－測試寒狼 0歲")

  const menu = within(stabled).getByRole("combobox", { name: "餵食項目" })
  expect(
    within(menu)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toContain("中級香氣果實（MT+4，50 金）")
  await userEvent.selectOptions(menu, "中級香氣果實（MT+4，50 金）")
  await userEvent.click(within(stabled).getByRole("button", { name: "餵食" }))

  expect(api.feedPet).toHaveBeenCalledWith(2, "fruit-aroma-medium")
  expect(onCharacterChanged).toHaveBeenCalledWith(fed)
  const redrawn = await card("寒狼－測試寒狼 0歲")
  expect(within(redrawn).getByText("MT").nextSibling).toHaveTextContent("8")
  expect(within(redrawn).getByRole("button", { name: "餵食" })).toBeDisabled()
})

test("CompanionsScreen_FeedDisabledUntilNextFeedTime", async () => {
  renderCompanions(
    createFakeApi({
      getPets: vi.fn().mockResolvedValue(makePets([{ ...slime, nextFeedAt: future }])),
    }),
  )

  expect(
    within(await card("小黏－草原史萊姆 2歲")).getByRole("button", { name: "餵食" }),
  ).toBeDisabled()
})

test("CompanionsScreen_FeedShowsMappedError", async () => {
  const api = createFakeApi({
    feedPet: vi.fn().mockRejectedValue(new ApiError(400, "無法餵食", ["overOwner"])),
  })
  renderCompanions(api)
  const stabled = await card("寒狼－測試寒狼 0歲")

  await userEvent.click(within(stabled).getByRole("button", { name: "餵食" }))

  expect(api.feedPet).toHaveBeenCalledWith(2, "fruit-vigor-small")
  expect(within(stabled).getByRole("alert")).toHaveTextContent("寵物屬性不能超過玩家")
})

test("CompanionsScreen_RenameSendsTheName", async () => {
  const api = createFakeApi({
    renamePet: vi.fn().mockResolvedValue(makePets([slime, { ...wolf, name: "小白" }])),
  })
  renderCompanions(api)
  const stabled = await card("寒狼－測試寒狼 0歲")

  const input = within(stabled).getByRole("textbox", { name: "名字" })
  await userEvent.clear(input)
  await userEvent.type(input, "小白")
  await userEvent.click(within(stabled).getByRole("button", { name: "改名" }))

  expect(api.renamePet).toHaveBeenCalledWith(2, "小白")
  expect(await card("小白－測試寒狼 0歲")).toBeInTheDocument()
})

test("CompanionsScreen_ReleaseNeedsConfirmation", async () => {
  const api = createFakeApi({ releasePet: vi.fn().mockResolvedValue(makePets([slime])) })
  renderCompanions(api)
  const stabled = await card("寒狼－測試寒狼 0歲")

  await userEvent.click(within(stabled).getByRole("button", { name: "放生" }))
  expect(api.releasePet).not.toHaveBeenCalled()
  await userEvent.click(within(stabled).getByRole("button", { name: "取消" }))
  expect(within(stabled).queryByRole("button", { name: "確定放生" })).not.toBeInTheDocument()

  await userEvent.click(within(stabled).getByRole("button", { name: "放生" }))
  await userEvent.click(within(stabled).getByRole("button", { name: "確定放生" }))

  expect(api.releasePet).toHaveBeenCalledWith(2)
  expect(await screen.findByRole("heading", { name: "寵物 1/3" })).toBeInTheDocument()
  expect(screen.queryByRole("article", { name: "寒狼－測試寒狼 0歲" })).not.toBeInTheDocument()
})

const openMercenaries = async () => {
  await userEvent.click(await screen.findByRole("tab", { name: "傭兵" }))
}

test("CompanionsScreen_MercenaryCatalogueHidesHireForHonourOnly", async () => {
  const hired = makeCharacter({ money: 7000 })
  const api = createFakeApi({
    hireMercenary: vi.fn().mockResolvedValue({
      mercenaries: makeMercenaryOverview({ contract: { ...resting, name: "F級傭兵" } }),
      character: hired,
    }),
  })
  const { onCharacterChanged } = renderCompanions(api)
  await openMercenaries()

  expect(
    await screen.findByText(
      "歡迎來到傭兵所，請選擇欲雇用的傭兵。傭兵的能力與技能和你相同，不同等級的傭兵戰鬥獲得的金錢、寶物與經驗比例不同；死亡消耗為傭兵戰敗時扣減的戰鬥次數。",
    ),
  ).toBeInTheDocument()
  const honourOnly = await card("A級傭兵")
  expect(within(honourOnly).getByText("勳章所兌換")).toBeInTheDocument()
  expect(within(honourOnly).queryByRole("button", { name: "僱用" })).not.toBeInTheDocument()

  const mercenaryF = await card("F級傭兵")
  const fields = within(within(mercenaryF).getByRole("list", { name: "條件" }))
    .getAllByRole("listitem")
    .map((item) => item.textContent)
  expect(fields).toEqual(["價格3,000", "戰鬥次數100", "金錢30%", "寶物30%", "經驗65%", "死亡消耗5"])
  await userEvent.click(within(mercenaryF).getByRole("button", { name: "僱用" }))

  expect(api.hireMercenary).toHaveBeenCalledWith("mercenary-f")
  expect(onCharacterChanged).toHaveBeenCalledWith(hired)
  expect(await screen.findByText("狀態：休息中")).toBeInTheDocument()
})

test("CompanionsScreen_HireButtonsDisabledWhileHired", async () => {
  renderCompanions(
    createFakeApi({
      getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview({ contract: resting })),
    }),
  )
  await openMercenaries()

  const mercenaryF = await card("F級傭兵")
  expect(within(mercenaryF).getByRole("button", { name: "僱用" })).toBeDisabled()
})

test("CompanionsScreen_RestingMercenaryDeploysToAChosenPlace", async () => {
  const api = createFakeApi({
    getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview({ contract: resting })),
    deployMercenary: vi.fn().mockResolvedValue(
      makeMercenaryOverview({
        contract: { ...fighting, placeId: 2, placeName: "試鍊洞窟" },
      }),
    ),
  })
  renderCompanions(api)
  await openMercenaries()

  const contract = await screen.findByRole("region", { name: "僱用中的傭兵" })
  expect(within(contract).getByText("狀態：休息中")).toBeInTheDocument()
  expect(within(contract).getByText("剩餘次數 97")).toBeInTheDocument()
  const places = await within(contract).findByRole("combobox", { name: "冒險地" })
  expect(
    within(places)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toEqual(["中央平原", "黑暗沼澤", "試鍊洞窟", "黑曜神廟"])
  await userEvent.selectOptions(places, "試鍊洞窟")
  await userEvent.click(within(contract).getByRole("button", { name: "開始戰鬥" }))

  expect(api.deployMercenary).toHaveBeenCalledWith(2)
  expect(await within(contract).findByText("狀態：戰鬥中")).toBeInTheDocument()
  expect(within(contract).getByText("冒險地：試鍊洞窟")).toBeInTheDocument()
  expect(within(contract).getByRole("button", { name: "停止戰鬥" })).toBeInTheDocument()
})

test("CompanionsScreen_FightingMercenaryRestsRenamesAndDismissesWithConfirmation", async () => {
  const api = createFakeApi({
    getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview({ contract: fighting })),
    restMercenary: vi.fn().mockResolvedValue(makeMercenaryOverview({ contract: resting })),
    renameMercenary: vi
      .fn()
      .mockResolvedValue(makeMercenaryOverview({ contract: { ...resting, name: "阿德" } })),
    dismissMercenary: vi.fn().mockResolvedValue(makeMercenaryOverview()),
  })
  renderCompanions(api)
  await openMercenaries()
  const contract = await screen.findByRole("region", { name: "僱用中的傭兵" })

  await userEvent.click(within(contract).getByRole("button", { name: "停止戰鬥" }))
  expect(api.restMercenary).toHaveBeenCalledTimes(1)
  expect(await within(contract).findByText("狀態：休息中")).toBeInTheDocument()

  const name = within(contract).getByRole("textbox", { name: "名字" })
  await userEvent.clear(name)
  await userEvent.type(name, "阿德")
  await userEvent.click(within(contract).getByRole("button", { name: "改名" }))
  expect(api.renameMercenary).toHaveBeenCalledWith("阿德")

  await userEvent.click(within(contract).getByRole("button", { name: "解除僱用" }))
  expect(api.dismissMercenary).not.toHaveBeenCalled()
  await userEvent.click(within(contract).getByRole("button", { name: "確定解除" }))

  expect(api.dismissMercenary).toHaveBeenCalledTimes(1)
  expect(screen.queryByRole("region", { name: "僱用中的傭兵" })).not.toBeInTheDocument()
})

test("CompanionsScreen_MercenaryShowsMappedError", async () => {
  const api = createFakeApi({
    hireMercenary: vi.fn().mockRejectedValue(new ApiError(400, "無法僱用", ["mercenaryHired"])),
  })
  renderCompanions(api)
  await openMercenaries()

  await userEvent.click(within(await card("F級傭兵")).getByRole("button", { name: "僱用" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("只能僱用一位傭兵")
})
