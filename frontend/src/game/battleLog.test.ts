import { describe, expect, test } from "vitest"
import { describeMercenaryReport, describePlaybackItem } from "./battleLog"
import type { LogLineKind } from "./battleLog"
import type { PlaybackItem } from "./battlePlayback"
import type {
  AttackEvent,
  BattleEvent,
  EncounterEvent,
  MercenaryReport,
  PetAttackEvent,
  SkillEvent,
} from "./battleEvents"
import type { BaseStats } from "./character"

type DistributiveOmit<T, K extends PropertyKey> = T extends unknown ? Omit<T, K> : never

const event = (e: DistributiveOmit<BattleEvent, "atSeconds">): PlaybackItem => ({
  kind: "event",
  event: { ...e, atSeconds: 0 } as BattleEvent,
})

const encounter = (overrides: Partial<EncounterEvent> = {}) =>
  event({
    type: "encounter",
    monsterId: "slime",
    monsterName: "史萊姆",
    monsterHealthPoints: 10,
    monsterMaxHealthPoints: 10,
    playerHealthPoints: 20,
    playerMaxHealthPoints: 20,
    playerManaPoints: 5,
    playerMaxManaPoints: 5,
    petName: null,
    petMonsterId: null,
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
    targetHealthPointsAfter: 5,
    stunned: false,
    ...overrides,
  })

const petAttack = (overrides: Partial<PetAttackEvent & { atSeconds: number }>) => {
  const { atSeconds = 0, ...rest } = overrides
  const base = event({
    type: "petAttack",
    petName: "小狼",
    kind: "Physical",
    hit: true,
    critical: false,
    damage: 77,
    targetHealthPointsAfter: 5,
    ...rest,
  })
  return base.kind === "event" ? { ...base, event: { ...base.event, atSeconds } } : base
}

const skill = (overrides: Partial<SkillEvent>) =>
  event({
    type: "skill",
    caster: "Player",
    skillId: "burst",
    skillName: "爆氣",
    level: 1,
    manaPointsAfter: 3,
    heal: 0,
    damage: 0,
    opponentHealthPointsAfter: null,
    healthPointsCost: 0,
    manaPointsCost: 0,
    moneyCost: 0,
    opponentManaPointsLoss: 0,
    manaPointsGain: 0,
    lasting: false,
    ...overrides,
  })

const growth = (overrides: Partial<BaseStats> = {}): BaseStats => ({
  strength: 0,
  intelligence: 0,
  life: 0,
  vitality: 0,
  agility: 0,
  charisma: 0,
  faith: 0,
  ...overrides,
})

type Line = [string, LogLineKind]

const cases: [string, PlaybackItem, Line[]][] = [
  ["encounter", encounter(), [["遭遇 史萊姆！", "normal"]]],
  ["round", event({ type: "round", round: 3 }), []],
  ["player physical hit", attack({}), [["你攻擊 史萊姆，造成 1,234 點傷害。", "normal"]]],
  [
    "player magic hit",
    attack({ kind: "Magic", damage: 88 }),
    [["你施放魔法，史萊姆受到 88 點傷害。", "normal"]],
  ],
  [
    "player critical",
    attack({ critical: true, damage: 120 }),
    [["暴擊！你攻擊史萊姆，造成 120 點傷害！", "trigger"]],
  ],
  [
    "player magic critical",
    attack({ kind: "Magic", critical: true, damage: 88 }),
    [["暴擊！你施放魔法，史萊姆受到 88 點傷害！", "trigger"]],
  ],
  ["player miss", attack({ hit: false, damage: 0 }), [["你的攻擊落空了。", "muted"]]],
  [
    "monster hit",
    attack({ attacker: "Monster", damage: 7 }),
    [["史萊姆攻擊，你受到 7 點傷害。", "normal"]],
  ],
  [
    "monster critical",
    attack({ attacker: "Monster", critical: true, damage: 45 }),
    [["史萊姆暴擊！你受到 45 點傷害！", "normal"]],
  ],
  [
    "monster magic critical",
    attack({ attacker: "Monster", kind: "Magic", critical: true, damage: 1900 }),
    [["史萊姆暴擊！你受到 1,900 點傷害！", "normal"]],
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
  [
    "player damaging skill",
    skill({ skillId: "poison", skillName: "施毒", damage: 50, opponentHealthPointsAfter: 10 }),
    [
      ["你施展了施毒！", "trigger"],
      ["史萊姆受到 50 點傷害。", "normal"],
    ],
  ],
  [
    "player skill with costs",
    skill({ skillName: "滅世", healthPointsCost: 450, manaPointsCost: 5 }),
    [
      ["你施展了滅世！", "trigger"],
      ["消耗了 HP 450、SP 5。", "muted"],
    ],
  ],
  [
    "lasting skill damage",
    skill({
      skillId: "poison",
      skillName: "施毒",
      damage: 50,
      opponentHealthPointsAfter: 10,
      lasting: true,
    }),
    [["施毒持續作用，史萊姆受到 50 點傷害。", "normal"]],
  ],
  [
    "player skill draining monster sp",
    skill({ skillId: "burn-mana", skillName: "燃燒魔力", opponentManaPointsLoss: 20 }),
    [
      ["你施展了燃燒魔力！", "trigger"],
      ["史萊姆失去 20 點 SP。", "normal"],
    ],
  ],
  [
    "player skill restoring sp",
    skill({ skillId: "mana-absorb", skillName: "魔力吸收", manaPointsGain: 10 }),
    [
      ["你施展了魔力吸收！", "trigger"],
      ["回復了 10 點 SP。", "normal"],
    ],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines cast with no amounts",
    skill({ caster: "Monster" }),
    [["史萊姆施展了爆氣！", "trigger"]],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines heal",
    skill({ caster: "Monster", skillName: "治癒術", heal: 50 }),
    [
      ["史萊姆施展了治癒術！", "trigger"],
      ["史萊姆回復了 50 點 HP。", "normal"],
    ],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines sp gain",
    skill({ caster: "Monster", skillName: "魔力吸收", manaPointsGain: 30 }),
    [
      ["史萊姆施展了魔力吸收！", "trigger"],
      ["史萊姆回復了 30 點 SP。", "normal"],
    ],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines damage",
    skill({ caster: "Monster", skillName: "星芒", damage: 120, opponentHealthPointsAfter: 700 }),
    [
      ["史萊姆施展了星芒！", "trigger"],
      ["你受到 120 點傷害。", "normal"],
    ],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines opponent sp loss",
    skill({ caster: "Monster", skillName: "燃燒魔力", opponentManaPointsLoss: 40 }),
    [
      ["史萊姆施展了燃燒魔力！", "trigger"],
      ["你失去 40 點 SP。", "normal"],
    ],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines lasting",
    skill({
      caster: "Monster",
      skillName: "施毒",
      lasting: true,
      damage: 25,
      opponentHealthPointsAfter: 700,
    }),
    [["施毒持續作用，你受到 25 點傷害。", "normal"]],
  ],
  [
    "describeSkill_MonsterCaster_WritesExpectedLines costs are not written",
    skill({ caster: "Monster", manaPointsCost: 4, healthPointsCost: 10 }),
    [["史萊姆施展了爆氣！", "trigger"]],
  ],
  [
    "describeAttack_StunnedPlayer_WritesMutedLine",
    attack({ hit: false, damage: 0, stunned: true }),
    [["你暈眩中，無法行動。", "muted"]],
  ],
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
    event({ type: "reward", experience: 12000, money: 3, jobExperience: 1500, drops: [] }),
    [["獲得經驗 12,000、金錢 3、熟練度 1,500。", "drop"]],
  ],
  [
    "reward with drops",
    event({
      type: "reward",
      experience: 10,
      money: 2,
      jobExperience: 1,
      drops: [
        { itemId: "iron-ore", name: "鐵礦石", quantity: 2, discarded: false },
        { itemId: "beast-hide", name: "野獸毛皮", quantity: 1, discarded: true },
      ],
    }),
    [
      ["獲得經驗 10、金錢 2、熟練度 1。", "drop"],
      ["獲得 鐵礦石 ×2", "drop"],
      ["背包已滿，野獸毛皮被丟棄了。", "muted"],
    ],
  ],
  [
    "reward with an empty drop list",
    event({ type: "reward", experience: 10, money: 2, jobExperience: 1, drops: [] }),
    [["獲得經驗 10、金錢 2、熟練度 1。", "drop"]],
  ],
  [
    "potion",
    event({
      type: "potion",
      itemId: "small-potion",
      name: "小藥水",
      healthPoints: 50,
      manaPoints: 25,
      remaining: 8,
    }),
    [["使用了小藥水，恢復 50 HP／25 SP。", "normal"]],
  ],
  [
    "level up",
    event({
      type: "levelUp",
      newLevel: 12,
      growth: {
        strength: 1,
        intelligence: 2,
        life: 3,
        vitality: 4,
        agility: 5,
        charisma: 6,
        faith: 7,
      },
    }),
    [["升級！Lv 12（力量+1 智力+2 生命+3 體質+4 敏捷+5 魅力+6 信仰+7）", "trigger"]],
  ],
  [
    "level up omits zero growth",
    event({ type: "levelUp", newLevel: 3, growth: growth({ strength: 2, agility: 1 }) }),
    [["升級！Lv 3（力量+2 敏捷+1）", "trigger"]],
  ],
  [
    "level up without growth",
    event({ type: "levelUp", newLevel: 1001, growth: growth() }),
    [["升級！Lv 1001", "trigger"]],
  ],
  [
    "free rest",
    event({ type: "rest", cost: 0, experiencePenalty: false, waitSeconds: 15 }),
    [["你在旅館休息，恢復了體力。", "muted"]],
  ],
  [
    "paid rest",
    event({ type: "rest", cost: 2100, experiencePenalty: false, waitSeconds: 15 }),
    [
      ["你在旅館休息，恢復了體力。", "muted"],
      ["付了 2,100 金錢住宿。", "muted"],
    ],
  ],
  [
    "rest with exp penalty",
    event({ type: "rest", cost: 0, experiencePenalty: true, waitSeconds: 15 }),
    [
      ["你在旅館休息，恢復了體力。", "muted"],
      ["付不起住宿費，經驗減少了。", "muted"],
    ],
  ],
  [
    "summary",
    {
      kind: "summary",
      fights: 1500,
      wins: 1498,
      experience: 20000,
      money: 4,
      levels: 2,
      missionKills: 0,
      petsCaptured: 0,
    },
    [["離開期間戰鬥 1,500 場（勝 1,498）、獲得經驗 20,000、金錢 4，升了 2 級。", "drop"]],
  ],
  [
    "summary mentions mission progress",
    {
      kind: "summary",
      fights: 12,
      wins: 10,
      experience: 300,
      money: 0,
      levels: 1,
      missionKills: 3,
      petsCaptured: 0,
    },
    [["離開期間戰鬥 12 場（勝 10）、獲得經驗 300、金錢 0，任務進度 +3，升了 1 級。", "drop"]],
  ],
  [
    "describes mission progress with the monster name",
    event({
      type: "mission",
      missionId: "m1",
      title: "清除雪精",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 2,
      required: 3,
      requiresDelivery: false,
      skillTooLow: false,
      completed: false,
    }),
    [["任務進度：史萊姆 2/3", "drop"]],
  ],
  [
    "adds a completion line when the target is met",
    event({
      type: "mission",
      missionId: "m1",
      title: "清除雪精",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 3,
      required: 3,
      requiresDelivery: false,
      skillTooLow: false,
      completed: true,
    }),
    [
      ["任務進度：史萊姆 3/3", "drop"],
      ["任務「清除雪精」的目標已達成，回任務頁回報。", "trigger"],
    ],
  ],
  [
    "shows an NPC with its message",
    event({ type: "npc", monsterId: "monster-265", name: "倫魯", message: "我是倫魯" }),
    [["遇到 倫魯：「我是倫魯」", "normal"]],
  ],
  [
    "shows an NPC without a message",
    event({ type: "npc", monsterId: "monster-275", name: "探險者", message: null }),
    [["遇到 探險者", "normal"]],
  ],
  [
    "adds no completion line while another target is not full",
    event({
      type: "mission",
      missionId: "m3",
      title: "冒牌傳說",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 3,
      required: 3,
      requiresDelivery: false,
      skillTooLow: false,
      completed: false,
    }),
    [["任務進度：史萊姆 3/3", "drop"]],
  ],
  [
    "tells a mixed mission to deliver before reporting",
    event({
      type: "mission",
      missionId: "m2",
      title: "幽靈船長的寶藏",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 3,
      required: 3,
      requiresDelivery: true,
      skillTooLow: false,
      completed: true,
    }),
    [
      ["任務進度：史萊姆 3/3", "drop"],
      ["任務「幽靈船長的寶藏」的擊殺目標已達成，交付後回任務頁回報。", "trigger"],
    ],
  ],
  [
    "tells a mission whose skill is too low to raise it before reporting",
    event({
      type: "mission",
      missionId: "m3",
      title: "血之試煉",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 3,
      required: 3,
      requiresDelivery: false,
      skillTooLow: true,
      completed: true,
    }),
    [
      ["任務進度：史萊姆 3/3", "drop"],
      ["任務「血之試煉」的擊殺目標已達成，技能等級達到要求後回任務頁回報。", "trigger"],
    ],
  ],
  [
    "names both conditions when delivery and skill are still needed",
    event({
      type: "mission",
      missionId: "m4",
      title: "血之試煉",
      monsterId: "slime",
      monsterName: "史萊姆",
      progress: 3,
      required: 3,
      requiresDelivery: true,
      skillTooLow: true,
      completed: true,
    }),
    [
      ["任務進度：史萊姆 3/3", "drop"],
      ["任務「血之試煉」的擊殺目標已達成，交付且技能等級達到要求後回任務頁回報。", "trigger"],
    ],
  ],
  [
    "summary mentions captured pets",
    {
      kind: "summary",
      fights: 12,
      wins: 10,
      experience: 300,
      money: 0,
      levels: 1,
      missionKills: 3,
      petsCaptured: 2,
    },
    [
      [
        "離開期間戰鬥 12 場（勝 10）、獲得經驗 300、金錢 0，任務進度 +3，捕捉寵物 2 隻，升了 1 級。",
        "drop",
      ],
    ],
  ],
  [
    "describes a pet guard",
    event({ type: "petGuard", petName: "小狼", damage: 1234, guardAfter: 50, retired: false }),
    [["小狼替你擋下了 1,234 點傷害。", "normal"]],
  ],
  [
    "adds the retire line when the guard breaks",
    event({ type: "petGuard", petName: "小狼", damage: 8, guardAfter: 0, retired: true }),
    [
      ["小狼替你擋下了 8 點傷害。", "normal"],
      ["小狼 戰敗，退出了戰鬥。", "muted"],
    ],
  ],
  ["describes a pet attack", petAttack({}), [["小狼發動攻擊，對史萊姆造成 77 點傷害。", "normal"]]],
  [
    "describes a pet critical",
    petAttack({ critical: true }),
    [["小狼會心一擊！對史萊姆造成 77 點傷害。", "trigger"]],
  ],
  [
    "describes a pet miss by the second",
    petAttack({ hit: false, atSeconds: 5.4 }),
    [["小狼 發呆", "muted"]],
  ],
  [
    "describes capture break and success in order",
    event({
      type: "capture",
      itemName: "捕捉網",
      itemBroken: true,
      capturedMonsterId: "slime",
      capturedMonsterName: "史萊姆",
      remaining: 3,
    }),
    [
      ["捕捉網受到損壞，數量減少1個！！", "muted"],
      ["捕捉到 史萊姆，捕捉網數量減少1個！！", "trigger"],
    ],
  ],
  [
    "describes a capture that only breaks the item",
    event({
      type: "capture",
      itemName: "捕捉網",
      itemBroken: true,
      capturedMonsterId: null,
      capturedMonsterName: null,
      remaining: 3,
    }),
    [["捕捉網受到損壞，數量減少1個！！", "muted"]],
  ],
  [
    "summary without level ups",
    {
      kind: "summary",
      fights: 12,
      wins: 10,
      experience: 300,
      money: 40,
      levels: 0,
      missionKills: 0,
      petsCaptured: 0,
    },
    [["離開期間戰鬥 12 場（勝 10）、獲得經驗 300、金錢 40。", "drop"]],
  ],
]

// WOG end_date「{角色名} 逃跑成功」
test("describePlaybackItem_EscapeNamesThePlayer", () => {
  expect(describePlaybackItem(event({ type: "escape" }), "史萊姆", "青衣")).toEqual([
    { text: "青衣 逃跑成功", kind: "muted" },
  ])
})

describe("describePlaybackItem", () => {
  test.each(cases)("%s", (_name, item, lines) => {
    expect(describePlaybackItem(item, "史萊姆")).toEqual(
      lines.map(([text, kind]) => ({ text, kind })),
    )
  })

  test("describePlaybackItem_UsesTheNameOnTheEncounter", () => {
    const item = encounter({ monsterId: "monster-1", monsterName: "神秘怪" })
    expect(describePlaybackItem(item, null)).toEqual([{ text: "遭遇 神秘怪！", kind: "normal" }])
  })
})

const mercenaryReport = (overrides: Partial<MercenaryReport> = {}): MercenaryReport => ({
  name: "阿福",
  fights: 2,
  wins: 1,
  experience: 1300,
  money: 45,
  levelsGained: 0,
  remaining: 95,
  dismissed: false,
  recent: [
    {
      at: new Date("2026-10-21T00:00:00Z"),
      monsterId: "slime",
      monsterName: "史萊姆",
      won: true,
      experience: 1300,
      money: 45,
    },
    {
      at: new Date("2026-10-21T00:01:40Z"),
      monsterId: "wolf",
      monsterName: "寒狼",
      won: false,
      experience: 0,
      money: 0,
    },
  ],
  ...overrides,
})

describe("describeMercenaryReport", () => {
  test("describeMercenaryReport_ListsEachFight_WhenFiveOrFewer", () => {
    expect(describeMercenaryReport(mercenaryReport())).toEqual([
      { text: "【傭兵】阿福 戰勝 史萊姆，獲得金錢 45，經驗值 1,300", kind: "muted" },
      { text: "【傭兵】阿福 戰敗 寒狼", kind: "muted" },
    ])
  })

  test("describeMercenaryReport_SummarisesMoreThanFiveFights", () => {
    expect(
      describeMercenaryReport(
        mercenaryReport({ fights: 6, wins: 4, experience: 5000, money: 120 }),
      ),
    ).toEqual([
      {
        text: "【傭兵】阿福 離開期間戰鬥 6 場（勝 4），獲得金錢 120，經驗值 5,000",
        kind: "muted",
      },
    ])
  })

  test("describeMercenaryReport_EndsWithDismissal_WhenFightsRunOut", () => {
    const lines = describeMercenaryReport(
      mercenaryReport({
        fights: 1,
        wins: 0,
        remaining: 0,
        dismissed: true,
        recent: [mercenaryReport().recent[1]],
      }),
    )
    expect(lines).toEqual([
      { text: "【傭兵】阿福 戰敗 寒狼", kind: "muted" },
      { text: "【傭兵】阿福 戰鬥次數已滿，自動解除僱用", kind: "muted" },
    ])
  })

  test("describePlaybackItem_DescribesMercenaryItems", () => {
    expect(describePlaybackItem({ kind: "mercenary", report: mercenaryReport() }, null)).toEqual(
      describeMercenaryReport(mercenaryReport()),
    )
  })
})
