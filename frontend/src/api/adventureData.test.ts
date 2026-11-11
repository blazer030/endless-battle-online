import { expect, test } from "vitest"
import { makeCharacter } from "../test/fixtures"
import { AdvanceConverter } from "./adventureData"
import { ParseError } from "./parse"

const growth = {
  strength: 2,
  intelligence: 0,
  life: 1,
  vitality: 1,
  agility: 1,
  charisma: 0,
  faith: 0,
}

// 每種事件各一個，欄位照 API 回應
const eventDtos = [
  {
    type: "encounter",
    atSeconds: 0,
    monsterId: "monster-1",
    monsterName: "史萊姆",
    monsterHealthPoints: 32,
    monsterMaxHealthPoints: 32,
    playerHealthPoints: 300,
    playerMaxHealthPoints: 350,
    playerManaPoints: 80,
    playerMaxManaPoints: 91,
    petName: null,
    petMonsterId: null,
  },
  { type: "round", atSeconds: 0, round: 1 },
  {
    type: "attack",
    atSeconds: 0,
    attacker: "Player",
    kind: "Physical",
    hit: true,
    critical: false,
    damage: 20,
    elementMultiplier: 1,
    targetHealthPointsAfter: 12,
    stunned: false,
  },
  {
    type: "skill",
    atSeconds: 1,
    caster: "Player",
    skillId: "skill-1",
    skillName: "爆氣",
    level: 1,
    manaPointsAfter: 75,
    heal: 0,
    damage: 12,
    opponentHealthPointsAfter: 0,
    healthPointsCost: 0,
    manaPointsCost: 5,
    moneyCost: 0,
    opponentManaPointsLoss: 0,
    manaPointsGain: 0,
    lasting: false,
  },
  {
    type: "potion",
    atSeconds: 1,
    itemId: "item-1",
    name: "藥草",
    healthPoints: 30,
    manaPoints: 0,
    remaining: 4,
  },
  { type: "defeat", atSeconds: 2, loser: "Monster" },
  { type: "timeout", atSeconds: 2, rounds: 50, damageDealt: 30 },
  {
    type: "reward",
    atSeconds: 2,
    experience: 8,
    money: 3,
    jobExperience: 1,
    drops: [{ itemId: "item-204", name: "黏液", quantity: 1, discarded: false }],
  },
  { type: "levelUp", atSeconds: 2, newLevel: 2, growth },
  { type: "rest", atSeconds: 3, cost: 0, experiencePenalty: false, waitSeconds: 5 },
  {
    type: "mission",
    atSeconds: 3,
    missionId: "mission-1",
    title: "討伐史萊姆",
    monsterId: "monster-1",
    monsterName: "史萊姆",
    progress: 1,
    required: 3,
    requiresDelivery: false,
    completed: false,
    skillTooLow: false,
  },
  { type: "npc", atSeconds: 4, monsterId: "monster-265", name: "倫魯", message: null },
  { type: "petGuard", atSeconds: 4, petName: "小黏", damage: 5, guardAfter: 10, retired: false },
  {
    type: "petAttack",
    atSeconds: 4,
    petName: "小黏",
    kind: "Magic",
    hit: true,
    critical: true,
    damage: 9,
    targetHealthPointsAfter: 3,
  },
  {
    type: "capture",
    atSeconds: 5,
    itemName: "捕獸網",
    itemBroken: false,
    capturedMonsterId: "monster-1",
    capturedMonsterName: "史萊姆",
    remaining: 2,
  },
  { type: "escape", atSeconds: 6 },
]

const summary = {
  fights: 1,
  wins: 1,
  losses: 0,
  timeouts: 0,
  experience: 8,
  money: 3,
  jobExperience: 1,
  levelsGained: 1,
  skillsCast: 1,
  drops: 1,
  potionsUsed: 1,
  missionKills: 1,
  petsCaptured: 1,
  escapes: 0,
}

const advanceDto = {
  events: eventDtos,
  summary,
  eventsCoverAll: true,
  character: makeCharacter(),
  mercenary: {
    name: "阿福",
    fights: 1,
    wins: 1,
    experience: 40,
    money: 7,
    levelsGained: 0,
    remaining: 9,
    dismissed: false,
    recent: [
      {
        at: "2026-11-11T08:00:00Z",
        monsterId: "monster-1",
        monsterName: "史萊姆",
        won: true,
        experience: 40,
        money: 7,
      },
    ],
  },
  areaPassExpiresAt: "2026-11-11T09:00:00Z",
}

const parseFailure = (value: unknown): ParseError => {
  try {
    AdvanceConverter.toVo(value)
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("AdvanceConverter_ConvertsEveryEventType", () => {
  const advance = AdvanceConverter.toVo(advanceDto)

  expect(advance.events).toEqual(eventDtos)
  expect(advance.summary).toEqual(summary)
  expect(advance.character.name).toBe(makeCharacter().name)
  expect(advance.mercenary?.recent[0].at.toISOString()).toBe("2026-11-11T08:00:00.000Z")
  expect(advance.areaPassExpiresAt?.toISOString()).toBe("2026-11-11T09:00:00.000Z")
})

test("AdvanceConverter_AllowsNoMercenaryAndNoPass", () => {
  const advance = AdvanceConverter.toVo({ ...advanceDto, mercenary: null, areaPassExpiresAt: null })

  expect(advance.mercenary).toBeNull()
  expect(advance.areaPassExpiresAt).toBeNull()
})

test("AdvanceConverter_RejectsAnUnknownEventType", () => {
  const events = [{ type: "dance", atSeconds: 0 }]

  expect(parseFailure({ ...advanceDto, events }).path).toBe("advance.events[0].type")
})

test("AdvanceConverter_MissingFieldNamesThePath", () => {
  const { playerManaPoints: _removed, ...encounter } = eventDtos[0] as Record<string, unknown>
  void _removed

  expect(parseFailure({ ...advanceDto, events: [encounter] }).path).toBe(
    "advance.events[0].playerManaPoints",
  )
})

test("AdvanceConverter_IgnoresUnknownFields", () => {
  const events = [{ ...eventDtos[1], weather: "rain" }]

  expect(AdvanceConverter.toVo({ ...advanceDto, events }).events).toEqual([eventDtos[1]])
})
