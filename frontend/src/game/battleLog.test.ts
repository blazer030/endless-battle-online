import { describe, expect, test } from "vitest"
import { createMonsterNameResolver, describePlaybackItem } from "./battleLog"
import type { LogLineKind } from "./battleLog"
import type { PlaybackItem } from "./battlePlayback"
import { makeContent } from "../test/fixtures"
import type { AttackEvent, BattleEvent, EncounterEvent, GrowthStats, SkillEvent } from "./types"

type DistributiveOmit<T, K extends PropertyKey> = T extends unknown ? Omit<T, K> : never

const resolver = createMonsterNameResolver(
  makeContent({ monsters: [{ id: "slime", name: "史萊姆" }] }),
)

const event = (e: DistributiveOmit<BattleEvent, "atSeconds">): PlaybackItem => ({
  kind: "event",
  event: { ...e, atSeconds: 0 } as BattleEvent,
})

const encounter = (overrides: Partial<EncounterEvent> = {}) =>
  event({
    type: "encounter",
    monsterId: "slime",
    monsterHp: 10,
    monsterHpMax: 10,
    playerHp: 20,
    playerHpMax: 20,
    playerSp: 5,
    playerSpMax: 5,
    staminaDepleted: false,
    ...overrides,
  })

const attack = (overrides: Partial<AttackEvent>) =>
  event({
    type: "attack",
    attacker: "Player",
    kind: "Physical",
    hit: true,
    critical: false,
    damage: 1234,
    elementMultiplier: 1,
    targetHpAfter: 5,
    specialAttackName: null,
    stunned: false,
    ...overrides,
  })

const skill = (overrides: Partial<SkillEvent>) =>
  event({
    type: "skill",
    caster: "Player",
    skillId: "burst",
    skillName: "爆氣",
    level: 1,
    spAfter: 3,
    heal: 0,
    ...overrides,
  })

const growth = (overrides: Partial<GrowthStats> = {}): GrowthStats => ({
  str: 0,
  smart: 0,
  life: 0,
  vit: 0,
  agi: 0,
  au: 0,
  be: 0,
  ...overrides,
})

type Line = [string, LogLineKind]

const cases: [string, PlaybackItem, Line[]][] = [
  ["encounter", encounter(), [["遭遇 史萊姆！", "normal"]]],
  [
    "encounter with stamina depleted",
    encounter({ staminaDepleted: true }),
    [
      ["遭遇 史萊姆！", "normal"],
      ["行動力耗盡，報酬減半。", "muted"],
    ],
  ],
  ["round", event({ type: "round", round: 3 }), []],
  ["player physical hit", attack({}), [["你攻擊 史萊姆，造成 1,234 點傷害。", "normal"]]],
  [
    "player magic hit",
    attack({ kind: "Magic", damage: 88 }),
    [["你施放魔法，史萊姆受到 88 點傷害。", "normal"]],
  ],
  [
    "player critical",
    attack({ critical: true, damage: 31207, specialAttackName: "星落" }),
    [["星落！史萊姆受到 31,207 點傷害！", "trigger"]],
  ],
  [
    "player critical without special attack name",
    attack({ critical: true, damage: 500, specialAttackName: null }),
    [["暴擊！史萊姆受到 500 點傷害！", "trigger"]],
  ],
  ["player miss", attack({ hit: false, damage: 0 }), [["你的攻擊落空了。", "muted"]]],
  [
    "monster hit",
    attack({ attacker: "Monster", damage: 7 }),
    [["史萊姆攻擊，你受到 7 點傷害。", "normal"]],
  ],
  [
    "monster critical",
    attack({ attacker: "Monster", critical: true, damage: 1900 }),
    [["史萊姆重擊！你受到 1,900 點傷害！", "normal"]],
  ],
  [
    "monster miss",
    attack({ attacker: "Monster", hit: false, damage: 0 }),
    [["史萊姆的攻擊落空了。", "normal"]],
  ],
  ["player skill cast", skill({}), [["你施展了爆氣！", "trigger"]]],
  [
    "player heal skill",
    skill({ skillId: "heal", skillName: "治癒", heal: 1234 }),
    [
      ["你施展了治癒！", "trigger"],
      ["回復了 1,234 點 HP。", "normal"],
    ],
  ],
  ["monster skill is not narrated", skill({ caster: "Monster" }), []],
  [
    "monster stunned",
    attack({ attacker: "Monster", hit: false, damage: 0, stunned: true }),
    [["史萊姆暈眩中，無法行動。", "muted"]],
  ],
  ["multi-hit strike", attack({ damage: 40 }), [["你攻擊 史萊姆，造成 40 點傷害。", "normal"]]],
  ["monster defeated", event({ type: "defeat", loser: "Monster" }), [["史萊姆倒下了。", "normal"]]],
  ["player defeated", event({ type: "defeat", loser: "Player" }), [["你倒下了……", "muted"]]],
  [
    "timeout",
    event({ type: "timeout", rounds: 100, damageDealt: 40 }),
    [["戰鬥陷入膠著，雙方收手。", "muted"]],
  ],
  [
    "reward",
    event({ type: "reward", exp: 12000, money: 3, jobExp: 1500 }),
    [["獲得經驗 12,000、金錢 3、熟練度 1,500。", "drop"]],
  ],
  [
    "level up",
    event({
      type: "levelUp",
      newLevel: 12,
      growth: { str: 1, smart: 2, life: 3, vit: 4, agi: 5, au: 6, be: 7 },
    }),
    [["升級！Lv 12（力量+1 智力+2 生命+3 體質+4 敏捷+5 魅力+6 信仰+7）", "trigger"]],
  ],
  [
    "level up omits zero growth",
    event({ type: "levelUp", newLevel: 3, growth: growth({ str: 2, agi: 1 }) }),
    [["升級！Lv 3（力量+2 敏捷+1）", "trigger"]],
  ],
  [
    "level up without growth",
    event({ type: "levelUp", newLevel: 1001, growth: growth() }),
    [["升級！Lv 1001", "trigger"]],
  ],
  [
    "free rest",
    event({ type: "rest", cost: 0, expPenalty: false, waitSeconds: 15 }),
    [["你在旅館休息，恢復了體力。", "muted"]],
  ],
  [
    "paid rest",
    event({ type: "rest", cost: 2100, expPenalty: false, waitSeconds: 15 }),
    [
      ["你在旅館休息，恢復了體力。", "muted"],
      ["付了 2,100 金錢住宿。", "muted"],
    ],
  ],
  [
    "rest with exp penalty",
    event({ type: "rest", cost: 0, expPenalty: true, waitSeconds: 15 }),
    [
      ["你在旅館休息，恢復了體力。", "muted"],
      ["付不起住宿費，經驗減少了。", "muted"],
    ],
  ],
  [
    "summary",
    { kind: "summary", fights: 1500, wins: 1498, exp: 20000, money: 4, levels: 2 },
    [["離開期間戰鬥 1,500 場（勝 1,498）、獲得經驗 20,000、金錢 4，升了 2 級。", "drop"]],
  ],
  [
    "summary without level ups",
    { kind: "summary", fights: 12, wins: 10, exp: 300, money: 40, levels: 0 },
    [["離開期間戰鬥 12 場（勝 10）、獲得經驗 300、金錢 40。", "drop"]],
  ],
]

describe("describePlaybackItem", () => {
  test.each(cases)("%s", (_name, item, lines) => {
    expect(describePlaybackItem(item, resolver, "slime")).toEqual(
      lines.map(([text, kind]) => ({ text, kind })),
    )
  })

  test("describePlaybackItem_FallsBackToMonsterId_WhenNameUnknown", () => {
    const item = encounter({ monsterId: "mystery" })
    expect(describePlaybackItem(item, resolver, null)).toEqual([
      { text: "遭遇 mystery！", kind: "normal" },
    ])
  })
})
