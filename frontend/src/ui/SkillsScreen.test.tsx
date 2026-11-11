import { act, render, screen, within } from "@testing-library/react"
import { PanelRefreshMilliseconds } from "../motion/timings"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { JobOption, SkillOverview, SkillView } from "../game/types"
import { makeCharacter, makeJob } from "../test/fixtures"
import { SkillsScreen, type SkillsApi } from "./SkillsScreen"

const jobOption = (overrides: Partial<JobOption>): JobOption => ({
  ...makeJob(),
  parentName: null,
  current: false,
  proficiencyPercent: 0,
  eligible: true,
  reasons: [],
  ...overrides,
})

const jobs: JobOption[] = [
  jobOption({
    id: "warrior",
    name: "戰士",
    current: true,
    proficiencyPercent: 42,
    eligible: false,
    reasons: ["sameJob"],
  }),
  jobOption({ id: "thief", name: "盜賊" }),
  jobOption({
    id: "knight",
    name: "騎士",
    tier: 2,
    parentId: "warrior",
    parentName: "戰士",
    unlockLevel: 30,
    proficiencyRequired: 3500,
    statRequirements: [
      { stat: "vitality", minimum: 60 },
      { stat: "strength", minimum: 20 },
    ],
    eligible: false,
    reasons: ["levelTooLow", "parentProficiencyTooLow", "statTooLow:vitality"],
  }),
]

// 效果文字由後端依每級的條件、消耗、效果產生，畫面照樣顯示
const next = (overrides: Partial<NonNullable<SkillView["next"]>> = {}) => ({
  level: 1,
  money: 500,
  proficiency: 300,
  summary: "機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合",
  canLearn: true,
  reasons: [],
  ...overrides,
})

const skill = (overrides: Partial<SkillView>): SkillView => ({
  id: "burst",
  name: "爆氣",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "提高物理攻擊力",
  icon: null,
  level: 0,
  maxLevel: 5,
  summary: null,
  prerequisite: null,
  next: next(),
  ...overrides,
})

const overview: SkillOverview = {
  jobId: "warrior",
  skillPoints: 1200,
  skills: [
    skill({}),
    skill({
      id: "iron-skin",
      name: "硬甲",
      slotType: "Passive",
      description: "提高物理防禦",
      level: 2,
      summary: "常駐｜自己物防 +2%",
      next: next({
        level: 3,
        summary: "常駐｜自己物防 +3%",
        money: 4500,
        canLearn: false,
        reasons: ["notEnoughMoney"],
      }),
    }),
    skill({
      id: "combo",
      name: "連擊",
      level: 5,
      summary: "機率 80%｜消耗 SP 17｜物理連擊 6 次，3 回合",
      next: null,
    }),
    skill({
      id: "swift-shadow",
      name: "急影",
      slotType: "Passive",
      description: "提高敏捷",
      level: 1,
      summary: "常駐｜自己速度 +1%",
      next: next({ level: 2, summary: "常駐｜自己速度 +2%", money: 2000 }),
    }),
    skill({
      id: "heal",
      name: "治癒",
      description: "回復 HP",
      level: 1,
      summary: "HP 低於 50%｜消耗 SP 5｜回復 HP 智力×100%，1 回合｜冷卻 4 回合",
      next: next({
        level: 2,
        summary: "HP 低於 50%｜消耗 SP 8｜回復 HP 智力×137.5%，1 回合｜冷卻 4 回合",
        money: 2000,
      }),
    }),
    skill({
      id: "parry",
      name: "架式",
      slotType: "Passive",
      timing: "AfterHit",
      description: "命中後提高物理防禦",
      level: 1,
      summary: "機率 40%｜自己物防 +1%，2 回合｜冷卻 3 回合",
      next: next({ level: 2, summary: "機率 50%｜自己物防 +2%，2 回合｜冷卻 3 回合", money: 2000 }),
    }),
  ],
  slots: [{ slot: 4, skillId: "iron-skin" }],
}

const learned: SkillOverview = {
  ...overview,
  skillPoints: 900,
  skills: overview.skills.map((entry) =>
    entry.id === "burst"
      ? {
          ...entry,
          level: 1,
          summary: "機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合",
          next: next({ level: 2, summary: "機率 50%｜消耗 SP 8｜自己物攻 +2%，3 回合" }),
        }
      : entry,
  ),
}

const createApi = (overrides: Partial<SkillsApi> = {}): SkillsApi => ({
  getJobs: vi.fn().mockResolvedValue(jobs),
  changeJob: vi.fn(),
  getSkills: vi.fn().mockResolvedValue(overview),
  learnSkill: vi.fn().mockResolvedValue(learned),
  setSkillSlots: vi.fn().mockResolvedValue(overview),
  getMyCharacter: vi.fn().mockResolvedValue(null),
  ...overrides,
})

const renderScreen = (api: SkillsApi = createApi()) => {
  const onCharacterChange = vi.fn()
  render(
    <SkillsScreen
      api={api}
      character={makeCharacter({ jobId: "warrior", money: 800 })}
      onCharacterChange={onCharacterChange}
    />,
  )
  return { api, onCharacterChange }
}

const jobCard = async (name: string) =>
  (await screen.findByRole("heading", { name })).closest("li") as HTMLElement

const skillCard = async (name: string) =>
  (await screen.findByRole("heading", { name })).closest("li") as HTMLElement

// 達標與否除了顏色，還要有 ✓／✗ 與可讀出的文字
const expectRequirement = (card: HTMLElement, text: string, met: boolean) => {
  const row = within(card)
    .getAllByRole("listitem")
    .find((item) => item.textContent?.endsWith(text))
  expect(row).toHaveAttribute("data-met", String(met))
  expect(row).toHaveTextContent(`${met ? "✓" : "✗"}${text}`)
  expect(
    within(row as HTMLElement).getByRole("img", { name: met ? "已達成" : "未達成" }),
  ).toBeInTheDocument()
}

test("SkillsScreen_ListsJobsWithRequirementsAndReasons", async () => {
  renderScreen()

  const warrior = await jobCard("戰士（基礎）")
  expect(within(warrior).getByText("目前")).toBeInTheDocument()
  expectRequirement(warrior, "Lv 1", true)

  const knight = await jobCard("騎士（進階）")
  expect(within(knight).queryByText("目前")).not.toBeInTheDocument()
  expectRequirement(knight, "Lv 30", false)
  expectRequirement(knight, "戰士熟練度 42%／100%", false)
  expectRequirement(knight, "體質 ≥ 60", false)
  expectRequirement(knight, "力量 ≥ 20", true)
  const change = within(knight).getByRole("button", { name: "轉職" })
  expect(change).toBeDisabled()
  expect(change).toHaveAttribute("title", "等級不足、前置職業熟練度不足、體質不足")

  expect(within(warrior).getByRole("button", { name: "轉職" })).toBeDisabled()
  expect(within(await jobCard("盜賊（基礎）")).getByRole("button", { name: "轉職" })).toBeEnabled()
})

test("SkillsScreen_ChangeJobReportsCharacterAndReloads", async () => {
  const changed = makeCharacter({ jobId: "thief", jobName: "盜賊" })
  const api = createApi({ changeJob: vi.fn().mockResolvedValue(changed) })
  const { onCharacterChange } = renderScreen(api)

  await user.click(within(await jobCard("盜賊（基礎）")).getByRole("button", { name: "轉職" }))

  expect(api.changeJob).toHaveBeenCalledWith("thief")
  expect(onCharacterChange).toHaveBeenCalledWith(changed)
  expect(api.getJobs).toHaveBeenCalledTimes(2)
  expect(api.getSkills).toHaveBeenCalledTimes(2)
})

test("SkillsScreen_KeepsTheJobChange_WhenReloadFails", async () => {
  const changed = makeCharacter({ jobId: "thief", jobName: "盜賊" })
  const api = createApi({
    changeJob: vi.fn().mockResolvedValue(changed),
    getJobs: vi.fn().mockResolvedValueOnce(jobs).mockRejectedValueOnce(new TypeError("offline")),
  })
  const { onCharacterChange } = renderScreen(api)

  await user.click(within(await jobCard("盜賊（基礎）")).getByRole("button", { name: "轉職" }))

  expect(onCharacterChange).toHaveBeenCalledWith(changed)
  expect(await screen.findByRole("alert")).toHaveTextContent("已轉職，重新載入失敗，請稍後再試")
  expect(screen.queryByText(/發生錯誤/)).not.toBeInTheDocument()
  const thief = await jobCard("盜賊（基礎）")
  expect(within(thief).getByText("目前")).toBeInTheDocument()
  expect(within(thief).getByRole("button", { name: "轉職" })).toBeDisabled()
  expect(within(await jobCard("戰士（基礎）")).queryByText("目前")).not.toBeInTheDocument()
})

test("SkillsScreen_NamesThePrerequisiteSkill", async () => {
  const advanced = skill({
    id: "advanced-burst",
    name: "狂暴爆氣",
    prerequisite: { skillId: "burst", name: "爆氣", level: 3 },
    next: next({ canLearn: false, reasons: ["prerequisite", "notEnoughMoney"] }),
  })
  renderScreen(
    createApi({ getSkills: vi.fn().mockResolvedValue({ ...overview, skills: [advanced] }) }),
  )

  const card = await skillCard("狂暴爆氣 Lv 0/5")
  expect(within(card).getByText("前置技能：爆氣 Lv 3、金錢不足")).toBeInTheDocument()
  expect(within(card).getByRole("button", { name: "學習" })).toHaveAttribute(
    "title",
    "前置技能：爆氣 Lv 3、金錢不足",
  )
})

test("SkillsScreen_ShowsSkillCardsWithNextLevelCost", async () => {
  renderScreen()

  expect(await screen.findByText("技能熟練度 1,200")).toBeInTheDocument()
  const burst = await skillCard("爆氣 Lv 0/5")
  expect(within(burst).getByText("提高物理攻擊力")).toBeInTheDocument()
  expect(
    within(burst).getByText("機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合・500 金錢・300 熟練度"),
  ).toBeInTheDocument()
  expect(within(burst).getByRole("button", { name: "學習" })).toBeEnabled()

  // 被動常駐：效果文字與費用
  const ironSkin = await skillCard("硬甲 Lv 2/5")
  expect(within(ironSkin).getByText("常駐｜自己物防 +2%")).toBeInTheDocument()
  expect(
    within(ironSkin).getByText("常駐｜自己物防 +3%・4,500 金錢・300 熟練度"),
  ).toBeInTheDocument()
  const upgrade = within(ironSkin).getByRole("button", { name: "升級" })
  expect(upgrade).toBeDisabled()
  expect(upgrade).toHaveAttribute("title", "金錢不足")
  expect(within(ironSkin).getByText("金錢不足")).toBeInTheDocument()

  // 命中後才發動的技能在效果文字前標出時機
  const parry = await skillCard("架式 Lv 1/5")
  expect(within(parry).getByText("命中後")).toBeInTheDocument()
  expect(within(parry).getByText("機率 40%｜自己物防 +1%，2 回合｜冷卻 3 回合")).toBeInTheDocument()

  // 有冷卻與 HP 條件的技能照效果文字顯示
  const heal = await skillCard("治癒 Lv 1/5")
  expect(
    within(heal).getByText("HP 低於 50%｜消耗 SP 5｜回復 HP 智力×100%，1 回合｜冷卻 4 回合"),
  ).toBeInTheDocument()
  expect(within(burst).queryByText("命中後")).not.toBeInTheDocument()

  // 已學滿級的技能顯示目前等級的效果文字
  const combo = await skillCard("連擊 Lv 5/5")
  expect(within(combo).getByText("機率 80%｜消耗 SP 17｜物理連擊 6 次，3 回合")).toBeInTheDocument()
  expect(within(combo).getByText("已達最高等級")).toBeInTheDocument()
  expect(within(combo).queryByRole("button")).not.toBeInTheDocument()
})

test("SkillsScreen_LearnCallsApiAndShowsNewOverview", async () => {
  const refreshed = makeCharacter({ money: 300 })
  const api = createApi({ getMyCharacter: vi.fn().mockResolvedValue(refreshed) })
  const { onCharacterChange } = renderScreen(api)

  await user.click(within(await skillCard("爆氣 Lv 0/5")).getByRole("button", { name: "學習" }))

  expect(api.learnSkill).toHaveBeenCalledWith("burst")
  const burst = await skillCard("爆氣 Lv 1/5")
  expect(within(burst).getByRole("button", { name: "升級" })).toBeEnabled()
  expect(screen.getByText("技能熟練度 900")).toBeInTheDocument()
  // 學技能扣了金錢，重新讀角色讓頂欄同步
  expect(onCharacterChange).toHaveBeenCalledWith(refreshed)
})

test("SkillsScreen_ShowsLearnReasonsFromServer", async () => {
  const api = createApi({
    learnSkill: vi.fn().mockRejectedValue(new ApiError(400, "無法學習技能", ["notEnoughMoney"])),
  })
  renderScreen(api)

  await user.click(within(await skillCard("爆氣 Lv 0/5")).getByRole("button", { name: "學習" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("無法學習技能：金錢不足")
})

test("SkillsScreen_SavesSlotsWithPutBody", async () => {
  const api = createApi({ getSkills: vi.fn().mockResolvedValue(learned) })
  renderScreen(api)

  // 主動欄顯示裝入技能目前等級的效果文字（空欄沒有）
  const slot1 = await screen.findByLabelText("欄位 1 技能")
  const row = slot1.parentElement as HTMLElement
  expect(within(row).queryByText(/機率/)).not.toBeInTheDocument()
  await user.selectOptions(slot1, "burst")
  expect(within(row).getByText("機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合")).toBeInTheDocument()
  const slot2 = screen.getByLabelText("欄位 2 技能")
  await user.selectOptions(slot2, "heal")
  expect(
    within(slot2.parentElement as HTMLElement).getByText(
      "HP 低於 50%｜消耗 SP 5｜回復 HP 智力×100%，1 回合｜冷卻 4 回合",
    ),
  ).toBeInTheDocument()
  await user.click(screen.getByRole("button", { name: "儲存欄位" }))

  expect(api.setSkillSlots).toHaveBeenCalledWith([
    { slot: 1, skillId: "burst" },
    { slot: 2, skillId: "heal" },
    { slot: 4, skillId: "iron-skin" },
  ])
  expect(await screen.findByText("欄位已儲存。")).toBeInTheDocument()
})

test("SkillsScreen_LocksTheLoadoutWhileSaving", async () => {
  const api = createApi({ setSkillSlots: vi.fn(() => new Promise<never>(() => {})) })
  renderScreen(api)

  await user.click(await screen.findByRole("button", { name: "儲存欄位" }))

  for (const slot of [1, 2, 3, 4, 5])
    expect(screen.getByLabelText(`欄位 ${slot} 技能`)).toBeDisabled()
  expect(screen.getByRole("button", { name: "儲存欄位" })).toBeDisabled()
})

// 主動欄列所有學過的主動技能、被動欄列所有學過的被動技能，都不限職業
test("SkillsScreen_OffersOnlyFittingSkillsPerSlot", async () => {
  renderScreen(createApi({ getSkills: vi.fn().mockResolvedValue(learned) }))

  const active = await screen.findByLabelText("欄位 2 技能")
  expect(
    within(active)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toEqual(["（空）", "爆氣 Lv 1", "連擊 Lv 5", "治癒 Lv 1"])
  const passive = screen.getByLabelText("欄位 5 技能")
  expect(
    within(passive)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toEqual(["（空）", "硬甲 Lv 2", "急影 Lv 1", "架式 Lv 1"])
})

test("SkillsScreen_DisablesSaveWhileLoadoutIsInvalid", async () => {
  renderScreen()

  await user.selectOptions(await screen.findByLabelText("欄位 5 技能"), "iron-skin")

  expect(screen.getByText("同一技能不能裝在兩個欄位")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "儲存欄位" })).toBeDisabled()
})

test("SkillsScreen_ShowsServerReasonsWhenSaveFails", async () => {
  const api = createApi({
    setSkillSlots: vi
      .fn()
      .mockRejectedValue(new ApiError(400, "技能欄位不正確", ["activeInPassiveSlot"])),
  })
  renderScreen(api)

  await user.click(await screen.findByRole("button", { name: "儲存欄位" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("技能欄位不正確：被動欄只能放被動技能")
})

test("SkillsScreen_ReloadsWhenProficiencyChangesInBattle", async () => {
  vi.useFakeTimers()
  try {
    const api = createApi()
    const props = {
      api,
      character: makeCharacter({ jobId: "warrior", money: 800 }),
      onCharacterChange: vi.fn(),
    }
    const { rerender } = render(<SkillsScreen {...props} refreshSignal={0} />)
    await act(async () => {
      await vi.advanceTimersByTimeAsync(PanelRefreshMilliseconds)
    })
    expect(api.getSkills).toHaveBeenCalledTimes(1)

    rerender(<SkillsScreen {...props} refreshSignal={1} />)
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(api.getSkills).toHaveBeenCalledTimes(2)
    expect(api.getJobs).toHaveBeenCalledTimes(2)
  } finally {
    vi.useRealTimers()
  }
})
