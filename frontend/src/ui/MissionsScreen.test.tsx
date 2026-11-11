import { act, render, screen, within } from "@testing-library/react"
import { PanelRefreshMilliseconds } from "../motion/timings"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { MissionBoard, MissionCompletion } from "../game/types"
import { makeCharacter, makeMission, makeMissionBoard } from "../test/fixtures"
import { emptyPublicNames } from "../game/publicNames"
import { MissionsScreen, type MissionsApi } from "./MissionsScreen"

const hunt = makeMission()
const daily = makeMission({
  id: "test-daily",
  title: "每日討伐：新手",
  giver: "公會櫃檯",
  daily: true,
})
const delivery = makeMission({
  id: "test-delivery",
  title: "藥草採集",
  kills: [],
  deliver: [{ itemId: "herb", name: "藥草", quantity: 5, owned: 5 }],
  ready: true,
})

const availableBoard = makeMissionBoard({ available: [hunt, daily] })
const acceptedBoard = makeMissionBoard({ available: [daily], active: [hunt] })

const createFakeApi = (overrides: Partial<MissionsApi> = {}): MissionsApi => ({
  getMissions: vi.fn().mockResolvedValue(availableBoard),
  acceptMission: vi.fn().mockResolvedValue(acceptedBoard),
  abandonMission: vi.fn().mockResolvedValue(availableBoard),
  completeMission: vi.fn(),
  ...overrides,
})

const renderMissions = (api: MissionsApi = createFakeApi()) => {
  const onCharacterChanged = vi.fn()
  render(
    <MissionsScreen api={api} names={emptyPublicNames} onCharacterChanged={onCharacterChanged} />,
  )
  return { api, onCharacterChanged }
}

const openSubPage = async (name: string) => {
  await user.click(await screen.findByRole("tab", { name }))
}

const card = (title: string) => screen.getByRole("article", { name: title })

const boardWith = (board: Partial<MissionBoard>) =>
  createFakeApi({ getMissions: vi.fn().mockResolvedValue(makeMissionBoard(board)) })

test("MissionsScreen_ShowsThreeSubPages", async () => {
  renderMissions(boardWith({ completed: [makeMission({ id: "done", title: "舊委託", level: 3 })] }))

  const tabs = within(await screen.findByRole("tablist", { name: "任務分頁" })).getAllByRole("tab")
  expect(tabs.map((tab) => tab.textContent)).toEqual(["可接", "進行中", "已完成"])
  expect(await screen.findByText("目前無任務")).toBeInTheDocument()

  await openSubPage("進行中")
  expect(screen.getByText("沒有進行中的任務")).toBeInTheDocument()

  await openSubPage("已完成")
  const done = card("舊委託")
  expect(within(done).getByText("委託者：村長")).toBeInTheDocument()
  expect(within(done).getByText("Lv 3")).toBeInTheDocument()
})

test("MissionsScreen_ListsEveryKillTarget", async () => {
  const fakes = makeMission({
    title: "冒牌傳說",
    kills: [
      {
        monsterId: "monster-111",
        monsterName: "冒牌英雄",
        placeId: 7,
        placeName: "王者之路",
        count: 3,
        progress: 3,
      },
      {
        monsterId: "monster-112",
        monsterName: "冒牌召喚師",
        placeId: 7,
        placeName: "王者之路",
        count: 3,
        progress: 1,
      },
    ],
  })
  renderMissions(boardWith({ active: [fakes] }))

  await openSubPage("進行中")

  const active = card("冒牌傳說")
  expect(within(active).getByText("擊敗 冒牌英雄（王者之路） 3/3")).toBeInTheDocument()
  expect(within(active).getByText("擊敗 冒牌召喚師（王者之路） 1/3")).toBeInTheDocument()
})

test("MissionsScreen_EmptyCompletedPage", async () => {
  renderMissions()

  await openSubPage("已完成")
  expect(screen.getByText("還沒有完成的任務")).toBeInTheDocument()
})

test("MissionsScreen_ListsDailyBeforeRegularMissions", async () => {
  renderMissions()

  const dailyHeading = await screen.findByRole("heading", { name: "每日任務" })
  const regularHeading = screen.getByRole("heading", { name: "一般任務" })
  expect(
    dailyHeading.compareDocumentPosition(regularHeading) & Node.DOCUMENT_POSITION_FOLLOWING,
  ).toBeTruthy()
  expect(within(card("每日討伐：新手")).getByText(/^※ 每日任務將在 \d+ 小時後重置$/)).toBeVisible()

  const regular = card("綠地上的黏液")
  expect(within(regular).getByText("委託者：村長")).toBeInTheDocument()
  expect(within(regular).getByText("綠地上的史萊姆越來越多了。")).toBeInTheDocument()
  expect(within(regular).getByText("委託內容")).toBeInTheDocument()
  expect(within(regular).getByText("委託報酬")).toBeInTheDocument()
  expect(within(regular).getByText("經驗 190")).toBeInTheDocument()
  expect(within(regular).getByText("Lv 1")).toBeInTheDocument()
})

test("MissionsScreen_AcceptRedrawsFromReturnedBoard", async () => {
  const { api } = renderMissions()

  await user.click(
    within(await screen.findByRole("article", { name: "綠地上的黏液" })).getByRole("button", {
      name: "接取",
    }),
  )

  expect(api.acceptMission).toHaveBeenCalledWith("test-hunt")
  expect(screen.queryByRole("article", { name: "綠地上的黏液" })).not.toBeInTheDocument()
  await openSubPage("進行中")
  expect(card("綠地上的黏液")).toBeInTheDocument()
})

test("MissionsScreen_DisablesAcceptAtActiveLimit", async () => {
  renderMissions(
    boardWith({
      available: [daily],
      active: [
        makeMission({ id: "a", title: "甲" }),
        makeMission({ id: "b", title: "乙" }),
        makeMission({ id: "c", title: "丙" }),
      ],
    }),
  )

  expect(await screen.findByText("進行中的任務已達 3 個")).toBeInTheDocument()
  expect(within(card("每日討伐：新手")).getByRole("button", { name: "接取" })).toBeDisabled()
})

test("MissionsScreen_ShowsProgressAndDisablesCompleteUntilReady", async () => {
  renderMissions(boardWith({ active: [hunt] }))

  await openSubPage("進行中")
  const active = card("綠地上的黏液")
  expect(within(active).getByText("擊敗 草原史萊姆（中央平原） 3/8")).toBeInTheDocument()
  expect(within(active).getByRole("progressbar")).toHaveAttribute("aria-valuenow", "38")
  expect(within(active).getByRole("button", { name: "完成" })).toBeDisabled()
})

test("MissionsScreen_AbandonNeedsConfirmation", async () => {
  const { api } = renderMissions(boardWith({ active: [hunt] }))

  await openSubPage("進行中")
  await user.click(within(card("綠地上的黏液")).getByRole("button", { name: "放棄" }))
  expect(api.abandonMission).not.toHaveBeenCalled()

  await user.click(within(card("綠地上的黏液")).getByRole("button", { name: "取消" }))
  expect(within(card("綠地上的黏液")).getByRole("button", { name: "放棄" })).toBeInTheDocument()

  await user.click(within(card("綠地上的黏液")).getByRole("button", { name: "放棄" }))
  await user.click(within(card("綠地上的黏液")).getByRole("button", { name: "確定放棄" }))

  expect(api.abandonMission).toHaveBeenCalledWith("test-hunt")
  expect(await screen.findByText("沒有進行中的任務")).toBeInTheDocument()
})

test("MissionsScreen_DeliveryMissionShowsDeliverButton", async () => {
  renderMissions(boardWith({ active: [delivery] }))

  await openSubPage("進行中")
  const active = card("藥草採集")
  expect(within(active).getByText("交付 藥草 5/5")).toBeInTheDocument()
  expect(within(active).getByRole("button", { name: "交付" })).toBeEnabled()
})

test("MissionsScreen_CompleteShowsResultAndUpdatesCharacter", async () => {
  const updated = makeCharacter({ money: 50, experience: 190 })
  const result: MissionCompletion = {
    board: makeMissionBoard({ completed: [delivery] }),
    character: updated,
    completionMessage: "謝謝你，藥草夠用了。",
    exp: 190,
    money: 50,
    items: [{ itemId: "small-potion", name: "小補給藥劑", quantity: 5 }],
    skill: null,
    skillGranted: false,
    levelsGained: 0,
    pet: null,
  }
  const { api, onCharacterChanged } = renderMissions(
    createFakeApi({
      getMissions: vi.fn().mockResolvedValue(makeMissionBoard({ active: [delivery] })),
      completeMission: vi.fn().mockResolvedValue(result),
    }),
  )

  await openSubPage("進行中")
  await user.click(within(card("藥草採集")).getByRole("button", { name: "交付" }))

  expect(api.completeMission).toHaveBeenCalledWith("test-delivery")
  const message = await screen.findByRole("status")
  expect(within(message).getByText("謝謝你，藥草夠用了。")).toBeInTheDocument()
  expect(within(message).getByText("獲得經驗 190、金錢 50")).toBeInTheDocument()
  expect(within(message).getByText("獲得 小補給藥劑 ×5")).toBeInTheDocument()
  expect(onCharacterChanged).toHaveBeenCalledWith(updated)
  expect(screen.getByText("沒有進行中的任務")).toBeInTheDocument()
})

test("MissionsScreen_ShowsTooManyError", async () => {
  renderMissions(
    createFakeApi({
      acceptMission: vi.fn().mockRejectedValue(new ApiError(400, "無法接取任務", ["tooMany"])),
    }),
  )

  await user.click(
    within(await screen.findByRole("article", { name: "綠地上的黏液" })).getByRole("button", {
      name: "接取",
    }),
  )

  expect(await screen.findByRole("alert")).toHaveTextContent("同時最多進行 3 個任務")
})

test("MissionsScreen_RetriesAfterLoadFailure", async () => {
  const getMissions = vi
    .fn()
    .mockRejectedValueOnce(new Error("offline"))
    .mockResolvedValue(availableBoard)
  renderMissions(createFakeApi({ getMissions }))

  expect(await screen.findByText("無法載入任務。")).toBeInTheDocument()
  await user.click(screen.getByRole("button", { name: "重試" }))

  expect(await screen.findByRole("article", { name: "綠地上的黏液" })).toBeInTheDocument()
})

test("MissionsScreen_PetDeliveryAsksForTheCarriedPetAndAnnouncesPetRewards", async () => {
  const petDelivery = makeMission({
    id: "test-tamer",
    title: "馴獸師的考驗",
    kills: [],
    deliverPet: { monsterId: "grass-slime", name: "草原史萊姆", carried: true },
    rewards: {
      exp: 0,
      money: 50,
      items: [],
      skill: null,
      pet: { monsterId: "test-wolf", name: "測試寒狼", level: 1 },
    },
    ready: true,
  })
  const result: MissionCompletion = {
    board: makeMissionBoard({ completed: [petDelivery] }),
    character: makeCharacter({ money: 50 }),
    completionMessage: "牠很健康。",
    exp: 0,
    money: 50,
    items: [],
    skill: null,
    skillGranted: false,
    levelsGained: 0,
    pet: { monsterId: "test-wolf", name: "測試寒狼", level: 1 },
  }
  const { api } = renderMissions(
    createFakeApi({
      getMissions: vi.fn().mockResolvedValue(makeMissionBoard({ active: [petDelivery] })),
      completeMission: vi.fn().mockResolvedValue(result),
    }),
  )

  await openSubPage("進行中")
  const active = card("馴獸師的考驗")
  expect(within(active).getByText("交付寵物 草原史萊姆（需攜帶）")).toBeInTheDocument()
  expect(within(active).getByText("寵物 測試寒狼")).toBeInTheDocument()
  await user.click(within(active).getByRole("button", { name: "交付" }))

  expect(api.completeMission).toHaveBeenCalledWith("test-tamer")
  expect(within(screen.getByRole("status")).getByText("獲得寵物 測試寒狼")).toBeInTheDocument()
})

test("MissionsScreen_ShowsMissingPetError", async () => {
  const petDelivery = makeMission({
    id: "test-tamer",
    title: "馴獸師的考驗",
    kills: [],
    deliverPet: { monsterId: "grass-slime", name: "草原史萊姆", carried: true },
    ready: true,
  })
  renderMissions(
    createFakeApi({
      getMissions: vi.fn().mockResolvedValue(makeMissionBoard({ active: [petDelivery] })),
      completeMission: vi.fn().mockRejectedValue(new ApiError(400, "無法完成任務", ["petMissing"])),
    }),
  )

  await openSubPage("進行中")
  await user.click(within(card("馴獸師的考驗")).getByRole("button", { name: "交付" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("需要攜帶指定的寵物")
})

test("MissionsScreen_ReloadsWhenAMissionEventPlays", async () => {
  vi.useFakeTimers()
  try {
    const api = createFakeApi()
    const props = { api, names: emptyPublicNames, onCharacterChanged: vi.fn() }
    const { rerender } = render(<MissionsScreen {...props} refreshSignal={0} />)
    await act(async () => {
      await vi.advanceTimersByTimeAsync(PanelRefreshMilliseconds)
    })
    expect(api.getMissions).toHaveBeenCalledTimes(1)

    rerender(<MissionsScreen {...props} refreshSignal={1} />)
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(api.getMissions).toHaveBeenCalledTimes(2)
  } finally {
    vi.useRealTimers()
  }
})
