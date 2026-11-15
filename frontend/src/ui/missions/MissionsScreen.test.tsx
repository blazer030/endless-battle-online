import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError } from "../../api/client"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter, makeMissionBoard } from "../../test/fixtures"
import { MissionCompletionVo, type MissionBoardVo } from "../../game/missions"
import { StageContext } from "../stage/stageContext"
import { MissionsScreen } from "./MissionsScreen"
import { createMissionsApi, daily, lv150Board, regular } from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderMissions = async (
  api = createMissionsApi(lv150Board()),
  options: { level?: number; onBack?: () => void; onCharacterChange?: () => void } = {},
) => {
  const onBack = options.onBack ?? vi.fn()
  const onCharacterChange = options.onCharacterChange ?? vi.fn()
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <MissionsScreen
        api={api}
        character={makeCharacter({ level: options.level ?? 150 })}
        onCharacterChange={onCharacterChange}
        onBack={onBack}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack, onCharacterChange }
}

const board = () => screen.getByRole("region", { name: "佈告欄" })
const pageButton = (name: "每日委託" | "一般委託") =>
  within(screen.getByRole("group", { name: "佈告欄頁" })).getByRole("button", {
    name: new RegExp(`^${name}`),
  })
const currentPage = () =>
  within(screen.getByRole("group", { name: "佈告欄頁" }))
    .getAllByRole("button")
    .find((button) => button.getAttribute("aria-pressed") === "true")
    ?.textContent?.replace(/\d+/g, "")
const slips = () => within(board()).queryAllByTestId("board-slip")
const swipe = (dx: number) => {
  fireEvent.pointerDown(board(), { clientX: 200 })
  fireEvent.pointerUp(board(), { clientX: 200 + dx })
}

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  cleanup()
  vi.useRealTimers()
})

// 規格 missions-screen：Opening page and counts（Lv 150）
test("OpensOnTheDailyPageWhenItHasSlips", async () => {
  await renderMissions()

  expect(currentPage()).toBe("每日委託")
  expect(slips()).toHaveLength(4)
})

// 規格 Opening page and counts（Lv 20，每日未開放）
test("OpensOnTheRegularPageWithoutDailies", async () => {
  await renderMissions(
    createMissionsApi(makeMissionBoard({ available: [regular("mission-1", 20)] })),
    {
      level: 20,
    },
  )

  expect(currentPage()).toBe("一般委託")
  expect(slips()).toHaveLength(1)
})

test("SwipeChangesPageWithoutOpeningASlip", async () => {
  await renderMissions()

  swipe(-60)
  fireEvent.click(slips()[0])

  expect(currentPage()).toBe("一般委託")
  expect(screen.queryByRole("dialog")).toBeNull()
  swipe(-60)
  expect(currentPage()).toBe("一般委託")
  swipe(60)
  expect(currentPage()).toBe("每日委託")
  swipe(30)
  expect(currentPage()).toBe("每日委託")
})

test("PaperBehindSwitchesPages", async () => {
  await renderMissions()

  fireEvent.click(pageButton("一般委託"))

  expect(currentPage()).toBe("一般委託")
  expect(slips()).toHaveLength(21)
})

test("PaperBehindShowsTheCount", async () => {
  await renderMissions()

  expect(pageButton("一般委託")).toHaveTextContent("21")
  expect(pageButton("每日委託")).not.toHaveTextContent(/\d/)
  fireEvent.click(pageButton("一般委託"))
  expect(pageButton("每日委託")).toHaveTextContent("3")
  expect(pageButton("一般委託")).not.toHaveTextContent(/\d/)
})

// 規格 Completed daily stays in place
test("CompletedDailyStaysFadedInPlace", async () => {
  await renderMissions()

  const titles = slips().map((slip) => within(slip).getByTestId("slip-title").textContent)
  expect(titles).toEqual([
    "委託mission-222",
    "委託mission-225",
    "委託mission-226",
    "委託mission-227",
  ])
  expect(slips()[1]).toHaveAttribute("data-done", "true")
  expect(within(slips()[1]).getByText("完")).toBeInTheDocument()
  expect(slips()[0]).toHaveAttribute("data-done", "false")
  expect(within(slips()[0]).getByText("討伐")).toBeInTheDocument()
  expect(within(slips()[0]).getByText("Lv 50")).toBeInTheDocument()
  expect(screen.getByText(/小時後重置$/)).toBeInTheDocument()
})

test("RegularPageListsEveryAvailable", async () => {
  await renderMissions()
  fireEvent.click(pageButton("一般委託"))

  expect(slips()).toHaveLength(21)
  expect(within(board()).getByTestId("board-notes")).toHaveAttribute("data-scroll", "true")
  expect(screen.queryByText(/小時後重置$/)).toBeNull()
})

// 規格 Completed pile
test("PileOpensTheCompletedList", async () => {
  await renderMissions()
  fireEvent.click(pageButton("一般委託"))

  fireEvent.click(screen.getByRole("button", { name: "完成的委託 2 件" }))
  const list = screen.getByRole("dialog", { name: "完成的委託" })
  const entries = within(list).getAllByRole("button", { name: /^委託/ })
  expect(entries.map((entry) => entry.getAttribute("aria-label"))).toEqual([
    "委託mission-90",
    "委託mission-80",
  ])

  fireEvent.click(entries[0])
  const sheet = screen.getByRole("dialog", { name: "委託mission-90" })
  expect(within(sheet).getByText("完")).toBeInTheDocument()
  fireEvent.click(within(sheet).getByRole("button", { name: "關閉" }))
  expect(screen.getByRole("dialog", { name: "完成的委託" })).toBeInTheDocument()
})

test("LoadingShowsBlankSlips", async () => {
  const api = createMissionsApi()
  vi.mocked(api.getMissions).mockReturnValue(new Promise<MissionBoardVo>(() => undefined))
  await renderMissions(api)

  const blanks = within(board()).getAllByTestId("board-blank")
  expect(blanks).toHaveLength(6)
  expect(slips()).toHaveLength(0)
  expect(pageButton("一般委託")).not.toHaveTextContent(/\d/)
  expect(screen.queryByText(/小時後重置$/)).toBeNull()
  expect(screen.getByTestId("hand-count")).toHaveTextContent("")
})

test("RetryAfterLoadFailure", async () => {
  const api = createMissionsApi(lv150Board())
  vi.mocked(api.getMissions).mockRejectedValueOnce(new Error("offline"))
  await renderMissions(api)

  expect(within(board()).getByText("哎呀，委託單一時找不到了……")).toBeInTheDocument()
  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(slips()).toHaveLength(4)
  expect(currentPage()).toBe("每日委託")
})

// 規格 Daily missions not open yet
test("DailyPageShowsFirstDailyLevelBeforeOpening", async () => {
  await renderMissions(
    createMissionsApi(
      makeMissionBoard({ available: [regular("mission-1", 20)], firstDailyLevel: 50 }),
    ),
    {
      level: 20,
    },
  )
  fireEvent.click(pageButton("每日委託"))

  expect(slips()).toHaveLength(0)
  expect(within(board()).getByText("Lv 50")).toBeInTheDocument()
})

test("BackReturnsToTheGuildSquare", async () => {
  const { onBack } = await renderMissions()

  fireEvent.click(screen.getByRole("button", { name: "公會前" }))

  expect(onBack).toHaveBeenCalledOnce()
})

test("MakesNoAdventureRequests", async () => {
  const { api } = await renderMissions()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 3)
  })

  expect(api.advance).not.toHaveBeenCalled()
  expect(api.getMissions).toHaveBeenCalledOnce()
  void daily
})

// ── 手邊（規格 Missions in hand）──

const hand = () => screen.getByRole("group", { name: "手邊的委託" })
const handSlips = () => within(hand()).queryAllByRole("button")
const deliver = (name: string, quantity: number, owned: number) => ({
  itemId: name,
  name,
  quantity,
  owned,
  category: "Material" as const,
})

test("HandShowsActiveMissionsInOrder", async () => {
  await renderMissions(
    createMissionsApi(
      lv150Board({ active: [regular("mission-31", 30), daily("mission-223", 50)] }),
    ),
  )

  expect(handSlips().map((slip) => within(slip).getByTestId("hand-title").textContent)).toEqual([
    "委託mission-31",
    "委託mission-223",
  ])
  expect(handSlips()[1]).toHaveAttribute("data-daily", "true")
  expect(within(hand()).getAllByTestId("hand-empty")).toHaveLength(1)
  expect(screen.getByTestId("hand-count")).toHaveTextContent("2")
})

// 規格 Several objectives
test("HandSlipShowsFirstObjectiveAndMore", async () => {
  const many = regular("mission-40", 40, {
    kills: [],
    deliver: [deliver("火", 3, 2), deliver("雷", 3, 0), deliver("風", 1, 0), deliver("土", 1, 0)],
  })
  await renderMissions(createMissionsApi(lv150Board({ active: [many] })))

  const slip = handSlips()[0]
  expect(within(slip).getByText("交付 火")).toBeInTheDocument()
  expect(within(slip).getByText("2 / 3")).toBeInTheDocument()
  expect(within(slip).getByText("＋3 項")).toBeInTheDocument()
  expect(within(slip).getByRole("progressbar")).toHaveAttribute("aria-valuenow", "25")
})

test("HandStampsReadyMissions", async () => {
  await renderMissions(
    createMissionsApi(
      lv150Board({
        active: [
          regular("mission-41", 1, {
            ready: true,
            kills: [{ ...regular("x").kills[0], progress: 8 }],
          }),
          regular("mission-42", 1, { ready: true, kills: [], deliver: [deliver("火", 1, 1)] }),
        ],
      }),
    ),
  )

  expect(within(handSlips()[0]).getByText("達成")).toBeInTheDocument()
  expect(within(handSlips()[1]).getByText("可交")).toBeInTheDocument()
})

// 規格 Only the skill is missing
test("HandDoesNotStampWhenOnlySkillIsMissing", async () => {
  const skillOnly = regular("mission-318", 5500, {
    kills: [{ ...regular("x").kills[0], count: 3, progress: 3 }],
    requiredSkill: { skillId: "skill-414", name: "施毒", level: 3, met: false, learnedLevel: 2 },
    ready: false,
  })
  await renderMissions(createMissionsApi(lv150Board({ active: [skillOnly] })))

  const slip = handSlips()[0]
  expect(within(slip).queryByText("達成")).toBeNull()
  expect(within(slip).getByText("＋1 項")).toBeInTheDocument()
  expect(Number(within(slip).getByRole("progressbar").getAttribute("aria-valuenow"))).toBeLessThan(
    100,
  )
})

test("HandSlipOpensItsSheet", async () => {
  await renderMissions(createMissionsApi(lv150Board({ active: [regular("mission-31", 30)] })))

  fireEvent.click(handSlips()[0])

  expect(screen.getByRole("dialog", { name: "委託mission-31" })).toBeInTheDocument()
})

// ── 委託單（規格 Mission sheet）──

const mission1 = regular("mission-1", 20, {
  title: "製作漁網",
  giver: "漁村村長",
  description: "幫我們做一張漁網吧。",
  kills: [],
  deliver: [deliver("蜘蛛絲", 1, 0)],
  rewards: {
    experience: 3800,
    money: 0,
    items: [{ itemId: "item-342", name: "白色布料", quantity: 1, category: "Material" }],
    skill: null,
    pet: null,
  },
})
const openSheet = (name: RegExp | string) => {
  fireEvent.click(within(board()).getByRole("button", { name }))
  return screen.getByRole("dialog")
}

// 規格 Sheet of an available mission
test("SheetOfAnAvailableMission", async () => {
  await renderMissions(createMissionsApi(makeMissionBoard({ available: [mission1] })))

  const sheet = openSheet(/^製作漁網/)
  expect(within(sheet).getByText(/^委託者.漁村村長$/)).toBeInTheDocument()
  expect(within(sheet).getByText("幫我們做一張漁網吧。")).toBeInTheDocument()
  expect(within(sheet).getByText("Lv 20")).toBeInTheDocument()
  const goal = within(sheet).getByTestId("goal-0")
  expect(goal).toHaveTextContent("交付 蜘蛛絲")
  expect(goal).toHaveTextContent("×1")
  expect(within(sheet).getByText("經驗 3,800")).toBeInTheDocument()
  expect(within(sheet).getByText("白色布料 ×1")).toBeInTheDocument()
  expect(screen.queryByRole("progressbar", { name: "委託進度" })).toBeNull()
})

// 規格 Sheet of an active mission with an unmet skill
test("SheetOfAnActiveMissionMarksUnmetSkill", async () => {
  const skillMission = regular("mission-318", 5500, {
    title: "輝煌都市傳說(1)",
    requiredSkill: { skillId: "skill-414", name: "施毒", level: 2, met: false, learnedLevel: 1 },
  })
  await renderMissions(createMissionsApi(lv150Board({ active: [skillMission] })))

  fireEvent.click(within(hand()).getByRole("button", { name: /^輝煌都市傳說/ }))
  const sheet = screen.getByRole("dialog")

  const skill = within(sheet).getByTestId("goal-1")
  expect(skill).toHaveTextContent("技能 施毒 Lv2")
  expect(skill).toHaveTextContent("目前 Lv1")
  expect(skill).toHaveAttribute("data-short", "true")
  expect(within(sheet).queryByText(/^Lv 5,500/)).toBeNull()
  expect(within(sheet).getByRole("progressbar", { name: "委託進度" })).toBeInTheDocument()
})

test("SheetShowsEveryObjective", async () => {
  const many = regular("mission-40", 40, {
    title: "紅蓮與奔雷",
    kills: [],
    deliver: [deliver("火", 3, 2), deliver("雷", 3, 3), deliver("風", 1, 0), deliver("土", 1, 0)],
    deliverPet: { monsterId: "monster-1", name: "飛兔", carried: true },
  })
  await renderMissions(createMissionsApi(lv150Board({ active: [many] })))

  fireEvent.click(within(hand()).getByRole("button", { name: /^紅蓮與奔雷/ }))
  const sheet = screen.getByRole("dialog")

  const goals = within(sheet).getAllByTestId(/^goal-/)
  expect(goals.map((goal) => goal.textContent)).toEqual([
    "交付 火2 / 3",
    "交付 雷3 / 3",
    "交付 風0 / 1",
    "交付 土0 / 1",
    "交付寵物 飛兔攜帶中",
  ])
  expect(goals[1]).toHaveAttribute("data-met", "true")
  expect(goals[2]).toHaveAttribute("data-short", "true")
})

test("RewardItemsUseCategoryPlaceholders", async () => {
  await renderMissions(createMissionsApi(makeMissionBoard({ available: [mission1] })))

  const sheet = openSheet(/^製作漁網/)

  const pictures = Array.from(sheet.querySelectorAll("img[data-asset]"))
  expect(pictures.map((picture) => picture.getAttribute("data-asset"))).toEqual([
    "items/蜘蛛絲",
    "items/item-342",
  ])
})

test("DailySheetShowsResetHours", async () => {
  await renderMissions()

  const sheet = openSheet(/^委託mission-222/)

  expect(within(sheet).getByText(/^每日委託・\d+ 小時後重置$/)).toBeInTheDocument()
  expect(within(sheet).getByRole("heading", { name: "委託mission-222" })).toBeInTheDocument()
})

// ── 接取、放棄、完成與莉瑟傳話（規格 Accept, abandon and complete from the sheet、Lise speaks feedback and errors）──

const seal = (sheet: HTMLElement) =>
  within(sheet).getByRole("button", { name: /^(接取|交付|完成)$/ })
const liseSays = () => screen.queryByTestId("lise-say")
const ready = regular("mission-50", 1, {
  title: "擊敗青魔導士",
  ready: true,
  kills: [{ ...regular("x").kills[0], count: 8, progress: 8 }],
  rewards: {
    experience: 9500,
    money: 15000,
    items: [{ itemId: "item-396", name: "500經驗膠囊", quantity: 2, category: "Consumable" }],
    skill: null,
    pet: null,
  },
})
const completion = (overrides: Partial<Parameters<typeof MissionCompletionVo.create>[0]> = {}) =>
  MissionCompletionVo.create({
    board: lv150Board({ completed: [{ ...ready, completionMessage: "謝謝你！" }] }),
    character: makeCharacter({ level: 50 }),
    completionMessage: "謝謝你！",
    experience: 9500,
    money: 15000,
    items: ready.rewards.items,
    skill: null,
    skillGranted: false,
    levelsGained: 1,
    pet: null,
    ...overrides,
  })
const openHand = (name: RegExp) => {
  fireEvent.click(within(hand()).getByRole("button", { name }))
  return screen.getByRole("dialog")
}

// 規格 Accept keeps the sheet open
test("AcceptKeepsTheSheetOpen", async () => {
  const api = createMissionsApi(makeMissionBoard({ available: [mission1] }))
  vi.mocked(api.acceptMission).mockResolvedValue(makeMissionBoard({ active: [mission1] }))
  await renderMissions(api)

  fireEvent.click(seal(openSheet(/^製作漁網/)))
  await flush()

  const sheet = screen.getByRole("dialog", { name: "製作漁網" })
  expect(api.acceptMission).toHaveBeenCalledWith("mission-1")
  expect(seal(sheet)).toHaveTextContent("交付")
  expect(seal(sheet)).toBeDisabled()
  expect(within(sheet).getByRole("button", { name: "放棄" })).toBeInTheDocument()
  expect(liseSays()).toHaveTextContent("「製作漁網」就拜託您了，路上小心！")
  expect(within(hand()).getByRole("button", { name: /^製作漁網/ })).toBeInTheDocument()
})

// 規格 Hand is full
test("AcceptDisabledWhenHandIsFull", async () => {
  await renderMissions(
    createMissionsApi(
      lv150Board({ active: [regular("mission-61"), regular("mission-62"), regular("mission-63")] }),
    ),
  )

  const sheet = openSheet(/^委託mission-222/)

  expect(seal(sheet)).toHaveTextContent("接取")
  expect(seal(sheet)).toBeDisabled()
  expect(within(sheet).getByText("手邊已有 3 張")).toBeInTheDocument()
})

test("AbandonAsksAgain", async () => {
  const active = regular("mission-31", 30)
  const api = createMissionsApi(lv150Board({ active: [active] }))
  vi.mocked(api.abandonMission).mockResolvedValue(lv150Board({ available: [active] }))
  await renderMissions(api)
  const sheet = openHand(/^委託mission-31/)

  fireEvent.click(within(sheet).getByRole("button", { name: "放棄" }))
  fireEvent.click(within(sheet).getByRole("button", { name: "取消" }))
  expect(api.abandonMission).not.toHaveBeenCalled()
  fireEvent.click(within(sheet).getByRole("button", { name: "放棄" }))
  fireEvent.click(within(sheet).getByRole("button", { name: "確定放棄" }))
  await flush()

  expect(api.abandonMission).toHaveBeenCalledWith("mission-31")
  expect(seal(screen.getByRole("dialog"))).toHaveTextContent("接取")
  expect(liseSays()).toHaveTextContent("好的，這張先放回佈告欄囉。")
})

// 規格 Completing with a level up
test("CompleteShowsStampAndRewards", async () => {
  const api = createMissionsApi(lv150Board({ active: [ready] }))
  vi.mocked(api.completeMission).mockResolvedValue(completion())
  await renderMissions(api)
  const sheet = openHand(/^擊敗青魔導士/)

  fireEvent.click(seal(sheet))
  await flush()

  const done = screen.getByRole("dialog", { name: "擊敗青魔導士" })
  expect(within(done).getByText("完")).toBeInTheDocument()
  expect(within(done).getByText("謝謝你！")).toBeInTheDocument()
  const got = within(done).getByRole("list", { name: "這次拿到" })
  expect(
    within(got)
      .getAllByRole("listitem")
      .map((item) => item.textContent),
  ).toEqual(["獲得經驗 9,500、金錢 15,000", "獲得 500經驗膠囊 ×2", "升到 Lv50"])
  expect(within(done).queryByText("委託報酬")).toBeNull()
  expect(within(hand()).queryByRole("button", { name: /^擊敗青魔導士/ })).toBeNull()
})

// 規格 Completion without a message
test("CompleteWithoutMessageShowsOnlyRewards", async () => {
  const api = createMissionsApi(lv150Board({ active: [ready] }))
  vi.mocked(api.completeMission).mockResolvedValue(
    completion({
      completionMessage: "",
      levelsGained: 0,
      board: lv150Board({ completed: [ready] }),
    }),
  )
  await renderMissions(api)

  fireEvent.click(seal(openHand(/^擊敗青魔導士/)))
  await flush()

  const done = screen.getByRole("dialog")
  expect(within(done).queryByTestId("giver-quote")).toBeNull()
  expect(within(done).getByRole("list", { name: "這次拿到" })).toBeInTheDocument()
})

test("CompleteHandsBackTheCharacter", async () => {
  const api = createMissionsApi(lv150Board({ active: [ready] }))
  const result = completion()
  vi.mocked(api.completeMission).mockResolvedValue(result)
  const { onCharacterChange } = await renderMissions(api)

  fireEvent.click(seal(openHand(/^擊敗青魔導士/)))
  await flush()

  expect(onCharacterChange).toHaveBeenCalledWith(result.character)
})

test("SendingBlocksTheSheet", async () => {
  const api = createMissionsApi(lv150Board({ active: [ready] }))
  vi.mocked(api.completeMission).mockReturnValue(new Promise(() => undefined))
  const { onBack } = await renderMissions(api)
  const sheet = openHand(/^擊敗青魔導士/)

  fireEvent.click(seal(sheet))

  expect(seal(sheet)).toBeDisabled()
  expect(seal(sheet)).toHaveAttribute("aria-busy", "true")
  expect(within(sheet).getByRole("button", { name: "放棄" })).toBeDisabled()
  expect(within(sheet).getByRole("button", { name: "關閉" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "公會前" })).toBeDisabled()
  fireEvent.click(screen.getByRole("button", { name: "公會前" }))
  expect(onBack).not.toHaveBeenCalled()
})

// 規格 Bag full names the category
test("BagFullNamesTheCategory", async () => {
  const material = regular("mission-99", 20, {
    title: "製作漁網",
    ready: true,
    kills: [],
    deliver: [deliver("蜘蛛絲", 1, 1)],
    rewards: { ...mission1.rewards },
  })
  const api = createMissionsApi(lv150Board({ active: [material] }))
  vi.mocked(api.completeMission).mockRejectedValue(new ApiError(400, "無法完成任務", ["bagFull"]))
  await renderMissions(api)

  fireEvent.click(seal(openHand(/^製作漁網/)))
  await flush()

  expect(liseSays()).toHaveTextContent("哎呀，材料的格子已經滿了……")
  expect(liseSays()).toHaveAttribute("data-failed", "true")
  expect(screen.getByRole("dialog", { name: "製作漁網" })).toBeInTheDocument()
})

test("FailureReloadsTheBoard", async () => {
  const api = createMissionsApi(lv150Board())
  vi.mocked(api.acceptMission).mockRejectedValue(new ApiError(400, "無法接取任務", ["tooMany"]))
  await renderMissions(api)

  fireEvent.click(seal(openSheet(/^委託mission-222/)))
  await flush()

  expect(liseSays()).toHaveTextContent("您手邊已經有 3 張委託了喔。")
  expect(api.getMissions).toHaveBeenCalledTimes(2)
})

test("LateReloadDoesNotUndoALaterSuccess", async () => {
  const before = makeMissionBoard({ available: [mission1] })
  const api = createMissionsApi(before)
  let answerReload: (board: MissionBoardVo) => void = () => undefined
  vi.mocked(api.getMissions)
    .mockResolvedValueOnce(before)
    .mockReturnValueOnce(new Promise((resolve) => (answerReload = resolve)))
  vi.mocked(api.acceptMission)
    .mockRejectedValueOnce(new TypeError("Failed to fetch"))
    .mockResolvedValueOnce(makeMissionBoard({ active: [mission1] }))
  await renderMissions(api)

  const sheet = openSheet(/^製作漁網/)
  fireEvent.click(seal(sheet))
  await flush()
  fireEvent.click(seal(sheet))
  await flush()
  answerReload(before)
  await flush()

  expect(seal(screen.getByRole("dialog", { name: "製作漁網" }))).toHaveTextContent("交付")
  expect(within(hand()).getByRole("button", { name: /^製作漁網/ })).toBeInTheDocument()
})

// 規格 Network failure
test("NetworkFailureIsSpoken", async () => {
  const api = createMissionsApi(lv150Board())
  vi.mocked(api.acceptMission).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderMissions(api)

  fireEvent.click(seal(openSheet(/^委託mission-222/)))
  await flush()

  expect(liseSays()).toHaveTextContent("咦，剛剛沒送出去……再試一次吧？")
  expect(api.getMissions).toHaveBeenCalledTimes(2)
})

test("NextActionClearsTheLine", async () => {
  const api = createMissionsApi(lv150Board())
  vi.mocked(api.acceptMission).mockRejectedValue(new TypeError("Failed to fetch"))
  await renderMissions(api)
  fireEvent.click(seal(openSheet(/^委託mission-222/)))
  await flush()
  expect(liseSays()).not.toBeNull()

  fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "關閉" }))
  openSheet(/^委託mission-222/)

  expect(liseSays()).toBeNull()
})
