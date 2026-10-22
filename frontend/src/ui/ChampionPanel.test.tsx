import { render, screen, within } from "@testing-library/react"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { ChampionChallenge } from "../game/types"
import { makeChampion, makeCharacter } from "../test/fixtures"
import { ChampionPanel, type ChampionApi } from "./ChampionPanel"

const local = (year: number, month: number, day: number, hour = 0, minute = 0) =>
  new Date(year, month - 1, day, hour, minute).toISOString()

const crowned = makeCharacter({ name: "青衣", level: 12, money: 500 })

const victory: ChampionChallenge = {
  champion: makeChampion({
    name: "青衣",
    level: 12,
    isPlayer: true,
    crownedAt: local(2026, 10, 22, 9),
    hp: 200,
    hpMax: 325,
  }),
  events: [
    {
      type: "attack",
      atSeconds: 2,
      attacker: "Player",
      kind: "Physical",
      hit: true,
      critical: false,
      stunned: false,
      damage: 1400,
      elementMultiplier: 1,
      targetHpAfter: 0,
      specialAttackName: null,
    },
    { type: "defeat", atSeconds: 2, loser: "Monster" },
  ],
  won: true,
  character: crowned,
}

const createFakeApi = (overrides: Partial<ChampionApi> = {}): ChampionApi => ({
  getChampion: vi.fn().mockResolvedValue(makeChampion()),
  challengeChampion: vi.fn().mockResolvedValue(victory),
  ...overrides,
})

const renderPanel = (api: ChampionApi = createFakeApi()) => {
  const onCharacterChanged = vi.fn()
  const onClose = vi.fn()
  render(<ChampionPanel api={api} onCharacterChanged={onCharacterChanged} onClose={onClose} />)
  return { api, onCharacterChanged, onClose }
}

test("ChampionPanel_ShowsTheSeedChampion", async () => {
  renderPanel()

  const dialog = screen.getByRole("dialog", { name: "冠軍" })
  const seat = await within(dialog).findByRole("article", { name: "王都冠軍 格雷德 Lv 30" })
  const stats = within(within(seat).getByRole("list"))
    .getAllByRole("listitem")
    .map((item) => item.textContent)
  expect(stats).toEqual([
    "HP 1,400/1,400",
    "物攻 180",
    "魔攻 25",
    "物防 150",
    "魔防 40",
    "敏捷 30",
    "魅力 25",
    "衛冕 0 次",
    "挑戰費 500",
  ])
  expect(within(dialog).getByRole("button", { name: "挑戰" })).toBeEnabled()
})

test("ChampionPanel_ChallengeShowsTheDuelAndTheNewChampion", async () => {
  const { api, onCharacterChanged } = renderPanel()

  await user.click(await screen.findByRole("button", { name: "挑戰" }))

  expect(api.challengeChampion).toHaveBeenCalledTimes(1)
  expect(onCharacterChanged).toHaveBeenCalledWith(crowned)
  const result = within(screen.getByRole("log", { name: "挑戰結果" }))
    .getAllByRole("listitem")
    .map((line) => line.textContent)
  expect(result).toEqual([
    "你攻擊 王都冠軍 格雷德，造成 1,400 點傷害。",
    "王都冠軍 格雷德倒下了。",
    "青衣 順利當上冠軍",
  ])
  const seat = screen.getByRole("article", { name: "青衣 Lv 12" })
  expect(within(seat).getByText("你自己（2026/10/22）")).toBeInTheDocument()
})

test("ChampionPanel_LossKeepsTheChampion", async () => {
  const api = createFakeApi({
    challengeChampion: vi.fn().mockResolvedValue({
      ...victory,
      champion: makeChampion({ hp: 900, defenses: 1 }),
      events: [victory.events[0], { type: "defeat", atSeconds: 4, loser: "Player" }],
      won: false,
    }),
  })
  renderPanel(api)

  await user.click(await screen.findByRole("button", { name: "挑戰" }))

  const result = within(screen.getByRole("log", { name: "挑戰結果" }))
    .getAllByRole("listitem")
    .map((line) => line.textContent)
  expect(result).toEqual([
    "你攻擊 王都冠軍 格雷德，造成 1,400 點傷害。",
    "你倒下了……",
    "挑戰失敗，冠軍保住了寶座。",
  ])
  expect(screen.getByText("衛冕 1 次")).toBeInTheDocument()
})

test("ChampionPanel_LockedSeatDisablesChallengeAndShowsReasons", async () => {
  renderPanel(
    createFakeApi({
      getChampion: vi.fn().mockResolvedValue(
        makeChampion({
          lockedUntil: local(2099, 1, 2, 3, 4),
          reasons: ["championLocked", "notEnoughMoney"],
        }),
      ),
    }),
  )

  expect(await screen.findByText("2099/01/02 03:04 之前無法挑戰冠軍")).toBeInTheDocument()
  expect(screen.getByText("冷卻中無法挑戰冠軍、挑戰冠軍最少需500")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "挑戰" })).toBeDisabled()
})

test("ChampionPanel_ShowsMappedChallengeError", async () => {
  renderPanel(
    createFakeApi({
      challengeChampion: vi.fn().mockRejectedValue(new ApiError(400, "無法挑戰", ["noHp"])),
    }),
  )

  await user.click(await screen.findByRole("button", { name: "挑戰" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("HP 為 0，無法挑戰")
})

test("ChampionPanel_CloseButtonCloses", async () => {
  const { onClose } = renderPanel()

  await user.click(screen.getByRole("button", { name: "關閉" }))

  expect(onClose).toHaveBeenCalledTimes(1)
})
