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
      { stat: "vit", minimum: 60 },
      { stat: "str", minimum: 20 },
    ],
    eligible: false,
    reasons: ["levelTooLow", "parentProficiencyTooLow", "statTooLow:vit"],
  }),
]

const next = (overrides: Partial<NonNullable<SkillView["next"]>> = {}) => ({
  level: 1,
  sp: 5,
  rounds: 3,
  chance: 0,
  money: 500,
  proficiency: 300,
  canLearn: true,
  reasons: [],
  ...overrides,
})

const skill = (overrides: Partial<SkillView>): SkillView => ({
  id: "burst",
  name: "爆氣",
  type: "Active",
  effect: "AtMultiplier",
  description: "提高物理攻擊力",
  jobId: "warrior",
  level: 0,
  maxLevel: 5,
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
      type: "Passive",
      effect: "DfMultiplier",
      description: "提高物理防禦",
      level: 2,
      next: next({ level: 3, money: 4500, canLearn: false, reasons: ["notEnoughMoney"] }),
    }),
    skill({ id: "combo", name: "連擊", jobId: "thief", effect: "MultiHit", level: 5, next: null }),
  ],
  slots: [{ slot: 4, skillId: "iron-skin", chance: 40 }],
}

const learned: SkillOverview = {
  ...overview,
  skillPoints: 900,
  skills: overview.skills.map((entry) =>
    entry.id === "burst" ? { ...entry, level: 1, next: next({ level: 2 }) } : entry,
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

test("SkillsScreen_ShowsSkillCardsWithNextLevelCost", async () => {
  renderScreen()

  expect(await screen.findByText("技能熟練度 1,200")).toBeInTheDocument()
  const burst = await skillCard("爆氣 Lv 0/5")
  expect(within(burst).getByText("提高物理攻擊力")).toBeInTheDocument()
  expect(within(burst).getByText("SP 5・3 回合・500 金錢・300 熟練度")).toBeInTheDocument()
  expect(within(burst).getByRole("button", { name: "學習" })).toBeEnabled()

  const ironSkin = await skillCard("硬甲 Lv 2/5")
  const upgrade = within(ironSkin).getByRole("button", { name: "升級" })
  expect(upgrade).toBeDisabled()
  expect(upgrade).toHaveAttribute("title", "金錢不足")
  expect(within(ironSkin).getByText("金錢不足")).toBeInTheDocument()

  const combo = await skillCard("連擊 Lv 5/5")
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

  await user.selectOptions(await screen.findByLabelText("欄位 1 技能"), "burst")
  const raise = screen.getByRole("button", { name: "提高欄位 1 發動率" })
  for (let step = 0; step < 6; step++) await user.click(raise)
  expect(screen.getByText("100%")).toBeInTheDocument()
  expect(raise).toBeDisabled()
  await user.click(screen.getByRole("button", { name: "儲存欄位" }))

  expect(api.setSkillSlots).toHaveBeenCalledWith([
    { slot: 1, skillId: "burst", chance: 100 },
    { slot: 4, skillId: "iron-skin", chance: 40 },
  ])
  expect(await screen.findByText("欄位已儲存。")).toBeInTheDocument()
})

test("SkillsScreen_LocksTheLoadoutWhileSaving", async () => {
  const api = createApi({ setSkillSlots: vi.fn(() => new Promise<never>(() => {})) })
  renderScreen(api)

  await user.click(await screen.findByRole("button", { name: "儲存欄位" }))

  for (const slot of [1, 2, 3, 4, 5])
    expect(screen.getByLabelText(`欄位 ${slot} 技能`)).toBeDisabled()
  expect(screen.getByRole("button", { name: "提高欄位 1 發動率" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "降低欄位 1 發動率" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "儲存欄位" })).toBeDisabled()
})

test("SkillsScreen_OffersOnlyFittingSkillsPerSlot", async () => {
  renderScreen(createApi({ getSkills: vi.fn().mockResolvedValue(learned) }))

  const active = await screen.findByLabelText("欄位 2 技能")
  expect(
    within(active)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toEqual(["（空）", "爆氣 Lv 1", "連擊 Lv 5"])
  const passive = screen.getByLabelText("欄位 5 技能")
  expect(
    within(passive)
      .getAllByRole("option")
      .map((option) => option.textContent),
  ).toEqual(["（空）", "硬甲 Lv 2"])
  expect(screen.queryByRole("button", { name: "提高欄位 5 發動率" })).not.toBeInTheDocument()
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
      .mockRejectedValue(new ApiError(400, "技能欄位不正確", ["passiveNotCurrentJob"])),
  })
  renderScreen(api)

  await user.click(await screen.findByRole("button", { name: "儲存欄位" }))

  expect(await screen.findByRole("alert")).toHaveTextContent(
    "技能欄位不正確：被動欄只能放目前職業的技能",
  )
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
