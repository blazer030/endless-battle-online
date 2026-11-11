import { act, render, screen, within } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import type { BuffView, ContentNames, InnView } from "../game/types"
import type { CharacterVo } from "../game/character"
import { makeCharacter, makeContent } from "../test/fixtures"
import { CharacterScreen } from "./CharacterScreen"
import { user } from "../test/user"

const character: CharacterVo = makeCharacter({
  name: "青衣",
  level: 12,
  experience: 340,
  nextLevelExperience: 1200,
  healthPoints: 210,
  manaPoints: 40,
  sex: 2,
  element: 3,
  birth: 4,
  jobId: "sorcerer",
  jobName: "術士",
  jobProficiency: [
    { jobId: "warrior", proficiency: 9000, skillPoints: 9000 },
    { jobId: "sorcerer", proficiency: 1234, skillPoints: 300 },
  ],
  stats: {
    strength: 14,
    intelligence: 31,
    life: 22,
    vitality: 13,
    agility: 18,
    charisma: 16,
    faith: 27,
    attack: 15,
    magicAttack: 33,
    defense: 12,
    magicDefense: 29,
    maxHealthPoints: 600,
    maxManaPoints: 199,
  },
  baseStats: {
    strength: 12,
    intelligence: 31,
    life: 22,
    vitality: 10,
    agility: 18,
    charisma: 16,
    faith: 25,
  },
})

const content: ContentNames = makeContent({
  sexes: [
    { id: "1", name: "男" },
    { id: "2", name: "女" },
  ],
  elements: ["地", "水", "火", "木", "風", "毒"].map((name, index) => ({
    id: String(index + 1),
    name,
  })),
  births: ["中央大陸", "魔法王國", "熱帶雨林", "末日王城"].map((name, index) => ({
    id: String(index + 1),
    name,
  })),
})

const noBuffs = () => ({
  getBuffs: vi.fn<() => Promise<BuffView[]>>().mockResolvedValue([]),
  getInn: vi.fn<() => Promise<InnView>>().mockResolvedValue({ rooms: [], lodging: null }),
})

const valueOf = (term: string, container: HTMLElement = document.body) => {
  const definition = within(container).getByText(term, { selector: "dt" }).nextElementSibling
  if (!(definition instanceof HTMLElement)) throw new Error(`${term} 沒有對應的數值`)
  return definition
}

test("CharacterScreen_ShowsProfile", () => {
  render(
    <CharacterScreen api={noBuffs()} character={character} content={content} onSignOut={vi.fn()} />,
  )

  expect(screen.getByRole("heading", { name: "青衣" })).toBeInTheDocument()
  expect(screen.getByText("術士")).toBeInTheDocument()
  expect(screen.getByText("Lv 12")).toBeInTheDocument()
  expect(valueOf("性別")).toHaveTextContent("女")
  expect(valueOf("屬性")).toHaveTextContent("火")
  expect(valueOf("出生地")).toHaveTextContent("末日王城")
  expect(screen.queryByText("必殺技")).not.toBeInTheDocument()
  expect(valueOf("職業熟練度")).toHaveTextContent(/^35%（1,234）$/)
})

test("CharacterScreen_ShowsZeroProficiency_WhenCurrentJobHasNone", () => {
  render(
    <CharacterScreen
      api={noBuffs()}
      character={{ ...character, jobProficiency: [] }}
      content={content}
      onSignOut={vi.fn()}
    />,
  )

  expect(valueOf("職業熟練度")).toHaveTextContent(/^0%（0）$/)
})

test("CharacterScreen_ShowsProportionalBars", () => {
  render(
    <CharacterScreen api={noBuffs()} character={character} content={content} onSignOut={vi.fn()} />,
  )

  const bars = [
    ["HP", 210, 600, "210 / 600"],
    ["SP", 40, 199, "40 / 199"],
    ["EXP", 340, 1200, "340 / 1,200"],
  ] as const
  for (const [label, now, max, text] of bars) {
    const bar = screen.getByRole("progressbar", { name: label })
    expect(bar).toHaveAttribute("aria-valuenow", String(now))
    expect(bar).toHaveAttribute("aria-valuemax", String(max))
    expect(bar).toHaveAttribute("aria-valuetext", text)
  }
})

test("CharacterScreen_ShowsBareStatsWithEquipmentBonus", () => {
  render(
    <CharacterScreen api={noBuffs()} character={character} content={content} onSignOut={vi.fn()} />,
  )

  // 裸點（+裝備）；加成為 0 時不顯示括號
  const stats = [
    ["力量", "12（+2）"],
    ["智力", "31"],
    ["生命", "22"],
    ["體質", "10（+3）"],
    ["敏捷", "18"],
    ["魅力", "16"],
    ["信仰", "25（+2）"],
  ] as const
  const card = screen.getByRole("region", { name: "能力" })
  for (const [label, value] of stats) {
    expect(valueOf(label, card).textContent).toBe(value)
  }
})

test("CharacterScreen_ShowsDerivedStats", () => {
  render(
    <CharacterScreen api={noBuffs()} character={character} content={content} onSignOut={vi.fn()} />,
  )

  const stats = [
    ["物攻", "15"],
    ["魔攻", "33"],
    ["物防", "12"],
    ["魔防", "29"],
    ["HP 上限", "600"],
    ["SP 上限", "199"],
  ] as const
  const card = screen.getByRole("region", { name: "戰鬥數值" })
  for (const [label, value] of stats) {
    expect(valueOf(label, card)).toHaveTextContent(new RegExp(`^${value}$`))
  }
})

test("CharacterScreen_FallsBackToIds_WhenContentMissing", () => {
  render(
    <CharacterScreen
      api={noBuffs()}
      character={character}
      content={makeContent()}
      onSignOut={vi.fn()}
    />,
  )

  const profile = screen.getByRole("region", { name: "青衣" })
  expect(valueOf("性別", profile)).toHaveTextContent(/^2$/)
  expect(valueOf("屬性", profile)).toHaveTextContent(/^3$/)
  expect(valueOf("出生地", profile)).toHaveTextContent(/^4$/)
})

test("CharacterScreen_CapsProficiencyPercentAtHundred", () => {
  const veteran = {
    ...character,
    jobProficiency: [{ jobId: "sorcerer", proficiency: 9000, skillPoints: 0 }],
  }
  render(
    <CharacterScreen api={noBuffs()} character={veteran} content={content} onSignOut={vi.fn()} />,
  )

  expect(valueOf("職業熟練度")).toHaveTextContent(/^100%（9,000）$/)
})

// 掛載時讀一次密藥
test("CharacterScreen_LoadsBuffsWhenOpened", async () => {
  const api = noBuffs()
  api.getBuffs.mockResolvedValue([
    { kind: "Exp", multiplier: 1.5, endsAt: new Date(Date.now() + 3_600_000).toISOString() },
    { kind: "Defense", multiplier: 1.2, endsAt: new Date(Date.now() + 600_000).toISOString() },
  ])
  render(<CharacterScreen api={api} character={character} content={content} onSignOut={vi.fn()} />)

  const card = screen.getByRole("region", { name: "密藥效果" })
  expect(await within(card).findByText("經驗 ×1.5（剩 60 分）")).toBeInTheDocument()
  expect(within(card).getByText("物防 ×1.2（剩 10 分）")).toBeInTheDocument()
  expect(api.getBuffs).toHaveBeenCalledTimes(1)
})

test("CharacterScreen_ShowsNoBuffs", async () => {
  render(
    <CharacterScreen api={noBuffs()} character={character} content={content} onSignOut={vi.fn()} />,
  )

  const card = screen.getByRole("region", { name: "密藥效果" })
  expect(await within(card).findByText("沒有生效中的密藥")).toBeInTheDocument()
})

test("CharacterScreen_BuffMinutesTickAndExpiredRowsDrop", async () => {
  vi.useFakeTimers()
  try {
    vi.setSystemTime(new Date("2026-10-23T03:00:00Z"))
    const api = noBuffs()
    api.getBuffs.mockResolvedValue([
      { kind: "Exp", multiplier: 1.5, endsAt: "2026-10-23T03:30:00Z" },
      { kind: "Defense", multiplier: 1.2, endsAt: "2026-10-23T03:01:30Z" },
    ])
    render(
      <CharacterScreen api={api} character={character} content={content} onSignOut={vi.fn()} />,
    )
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0)
    })
    const card = screen.getByRole("region", { name: "密藥效果" })
    expect(within(card).getByText("經驗 ×1.5（剩 30 分）")).toBeInTheDocument()
    expect(within(card).getByText("物防 ×1.2（剩 2 分）")).toBeInTheDocument()

    await act(async () => {
      await vi.advanceTimersByTimeAsync(120_000)
    })

    expect(within(card).getByText("經驗 ×1.5（剩 28 分）")).toBeInTheDocument()
    expect(within(card).queryByText(/物防/)).not.toBeInTheDocument()
    expect(api.getBuffs).toHaveBeenCalledTimes(1)
  } finally {
    vi.useRealTimers()
  }
})

const loft = {
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Exp" as const,
  multiplier: 1.1,
  seconds: 28_800,
  price: 600,
}

test("CharacterScreen_ShowsTheLodgingLineInTheElixirCard", async () => {
  const api = noBuffs()
  api.getInn.mockResolvedValue({
    rooms: [loft],
    lodging: { roomId: "apprentice-loft", endsAt: new Date(Date.now() + 3_600_000).toISOString() },
  })
  render(<CharacterScreen api={api} character={character} content={content} onSignOut={vi.fn()} />)

  const card = screen.getByRole("region", { name: "密藥效果" })
  expect(await within(card).findByText("學徒閣樓・經驗 ×1.10（剩 1:00）")).toBeInTheDocument()
  expect(within(card).getByText("沒有生效中的密藥")).toBeInTheDocument()
})

test("CharacterScreen_HidesTheLodgingLineWithoutLodging", async () => {
  const api = noBuffs()
  api.getBuffs.mockResolvedValue([
    { kind: "Exp", multiplier: 1.5, endsAt: new Date(Date.now() + 3_600_000).toISOString() },
  ])
  render(<CharacterScreen api={api} character={character} content={content} onSignOut={vi.fn()} />)

  const card = screen.getByRole("region", { name: "密藥效果" })
  expect(await within(card).findByText("經驗 ×1.5（剩 60 分）")).toBeInTheDocument()
  expect(api.getInn).toHaveBeenCalledTimes(1)
  expect(within(card).queryByText(/・/)).not.toBeInTheDocument()
})

test("CharacterScreen_KeepsElixirsWhenTheInnFails", async () => {
  const api = noBuffs()
  api.getInn.mockRejectedValue(new Error("boom"))
  api.getBuffs.mockResolvedValue([
    { kind: "Exp", multiplier: 1.5, endsAt: new Date(Date.now() + 3_600_000).toISOString() },
  ])
  render(<CharacterScreen api={api} character={character} content={content} onSignOut={vi.fn()} />)

  const card = screen.getByRole("region", { name: "密藥效果" })
  expect(await within(card).findByText("經驗 ×1.5（剩 60 分）")).toBeInTheDocument()
  expect(within(card).queryByText(/・/)).not.toBeInTheDocument()
})

test("CharacterScreen_SignOutButtonNotifies", async () => {
  const onSignOut = vi.fn()
  render(
    <CharacterScreen
      api={noBuffs()}
      character={character}
      content={content}
      onSignOut={onSignOut}
    />,
  )

  await user.click(screen.getByRole("button", { name: "登出" }))

  expect(onSignOut).toHaveBeenCalledTimes(1)
})
