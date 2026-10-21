import { render, screen, within } from "@testing-library/react"
import { expect, test } from "vitest"
import type { CharacterView, ContentNames } from "../game/types"
import { makeCharacter, makeContent } from "../test/fixtures"
import { CharacterScreen } from "./CharacterScreen"

const character: CharacterView = makeCharacter({
  name: "青衣",
  level: 12,
  exp: 340,
  nextExp: 1200,
  hp: 210,
  sp: 40,
  sex: 2,
  element: 3,
  birth: 4,
  jobId: "sorcerer",
  jobName: "術士",
  specialAttackName: "星落",
  jobProficiency: [
    { jobId: "warrior", proficiency: 9000, skillPoints: 9000 },
    { jobId: "sorcerer", proficiency: 1234, skillPoints: 300 },
  ],
  stats: {
    str: 14,
    smart: 31,
    life: 22,
    vit: 13,
    agi: 18,
    au: 16,
    be: 27,
    at: 15,
    mat: 33,
    df: 12,
    mdf: 29,
    hpMax: 600,
    spMax: 199,
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

const valueOf = (term: string, container: HTMLElement = document.body) => {
  const definition = within(container).getByText(term, { selector: "dt" }).nextElementSibling
  if (!(definition instanceof HTMLElement)) throw new Error(`${term} 沒有對應的數值`)
  return definition
}

test("CharacterScreen_ShowsProfile", () => {
  render(<CharacterScreen character={character} content={content} />)

  expect(screen.getByRole("heading", { name: "青衣" })).toBeInTheDocument()
  expect(screen.getByText("術士")).toBeInTheDocument()
  expect(screen.getByText("Lv 12")).toBeInTheDocument()
  expect(valueOf("性別")).toHaveTextContent("女")
  expect(valueOf("屬性")).toHaveTextContent("火")
  expect(valueOf("出生地")).toHaveTextContent("末日王城")
  expect(valueOf("必殺技")).toHaveTextContent("星落")
  expect(valueOf("職業熟練度")).toHaveTextContent(/^1,234$/)
})

test("CharacterScreen_ShowsZeroProficiency_WhenCurrentJobHasNone", () => {
  render(<CharacterScreen character={{ ...character, jobProficiency: [] }} content={content} />)

  expect(valueOf("職業熟練度")).toHaveTextContent(/^0$/)
})

test("CharacterScreen_ShowsProportionalBars", () => {
  render(<CharacterScreen character={character} content={content} />)

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

test("CharacterScreen_ShowsBaseStatsWithBonusPlaceholder", () => {
  render(<CharacterScreen character={character} content={content} />)

  const stats = [
    ["力量", 14],
    ["智力", 31],
    ["生命", 22],
    ["體質", 13],
    ["敏捷", 18],
    ["魅力", 16],
    ["信仰", 27],
  ] as const
  const card = screen.getByRole("region", { name: "能力" })
  for (const [label, value] of stats) {
    expect(valueOf(label, card)).toHaveTextContent(new RegExp(`^${value}\\(\\+0\\)$`))
  }
})

test("CharacterScreen_ShowsDerivedStats", () => {
  render(<CharacterScreen character={character} content={content} />)

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
  render(<CharacterScreen character={character} content={makeContent()} />)

  const profile = screen.getByRole("region", { name: "青衣" })
  expect(valueOf("性別", profile)).toHaveTextContent(/^2$/)
  expect(valueOf("屬性", profile)).toHaveTextContent(/^3$/)
  expect(valueOf("出生地", profile)).toHaveTextContent(/^4$/)
})
