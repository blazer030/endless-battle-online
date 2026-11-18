import { describe, expect, it } from "vitest"
import { makeCharacter, makeMission, makeMissionBoard } from "../test/fixtures"
import {
  actionLabel,
  completedRegular,
  completionRows,
  dailyNotes,
  displayTitle,
  handSlip,
  kindLabel,
  liseLine,
  missionsLayout,
  missionsRevealOrigin,
  objectiveRows,
  openingPage,
  pageCounts,
  regularNotes,
  requirementText,
  resetHours,
  rewardRows,
  MaxActiveMissions,
  progressRatio,
  MissionCompletionVo,
} from "./missions"

describe("MaxActiveMissions", () => {
  it("is three", () => {
    expect(MaxActiveMissions).toBe(3)
  })
})

const delivery = makeMission({
  id: "test-delivery",
  kills: [],
  deliver: [
    { itemId: "herb", name: "藥草", quantity: 5, owned: 7, category: "Material" },
    { itemId: "fang", name: "狼牙", quantity: 3, owned: 1, category: "Material" },
  ],
})

const fakes = makeMission({
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
      count: 5,
      progress: 1,
    },
  ],
})

describe("progressRatio", () => {
  it("uses kill progress over the count", () => {
    expect(progressRatio(makeMission())).toBe(3 / 8)
  })

  it("caps kill progress at one", () => {
    const mission = makeMission()
    expect(
      progressRatio({
        ...mission,
        kills: mission.kills.map((kill) => ({ ...kill, progress: 12 })),
      }),
    ).toBe(1)
  })

  it("adds up every kill target", () => {
    // (3 + 1) / (3 + 5)
    expect(progressRatio(fakes)).toBe(4 / 8)
  })

  it("caps each kill target before adding them up", () => {
    const overshoot = {
      ...fakes,
      kills: fakes.kills.map((kill, index) => (index === 0 ? { ...kill, progress: 9 } : kill)),
    }
    // (min(9,3) + 1) / (3 + 5)
    expect(progressRatio(overshoot)).toBe(4 / 8)
  })

  it("counts delivered items without overshooting each line", () => {
    // min(7,5) + min(1,3) = 6，共需 8
    expect(progressRatio(delivery)).toBe(6 / 8)
  })

  it("combines kills and deliveries for a mixed mission", () => {
    // (3 + min(7,5) + min(1,3)) / (8 + 5 + 3)
    expect(progressRatio({ ...makeMission(), deliver: delivery.deliver })).toBe(9 / 16)
  })

  it("caps kill progress before combining it with deliveries", () => {
    const mission = makeMission()
    const mixed = {
      ...mission,
      kills: mission.kills.map((kill) => ({ ...kill, progress: 12 })),
      deliver: delivery.deliver.map((item) => ({ ...item, owned: 0 })),
    }
    // min(12,8) / (8 + 8)，不是 12 / 16
    expect(progressRatio(mixed)).toBe(8 / 16)
  })

  describe("counts a pet and a skill condition as one step each", () => {
    const fullKills = makeMission().kills.map((kill) => ({ ...kill, count: 3, progress: 3 }))
    const pet = { monsterId: "grass-slime", name: "草原史萊姆" }
    const skill = { skillId: "skill-414", name: "無我", level: 2, learnedLevel: null }

    it.each([
      ["skill not met", null, { ...skill, met: false }, 3 / 4],
      ["pet not carried", { ...pet, carried: false }, null, 3 / 4],
      ["pet carried and skill met", { ...pet, carried: true }, { ...skill, met: true }, 1],
    ])("%s", (_name, deliverPet, requiredSkill, expected) => {
      expect(progressRatio(makeMission({ kills: fullKills, deliverPet, requiredSkill }))).toBe(
        expected,
      )
    })

    it("counts a carried pet without kills", () => {
      const carried = { ...pet, carried: true }
      expect(progressRatio(makeMission({ kills: [], deliverPet: carried }))).toBe(1)
      expect(
        progressRatio(makeMission({ kills: [], deliverPet: { ...carried, carried: false } })),
      ).toBe(0)
    })
  })

  it("is full for a ready mission without objectives", () => {
    expect(progressRatio(makeMission({ kills: [], ready: true }))).toBe(1)
    expect(progressRatio(makeMission({ kills: [] }))).toBe(0)
  })
})

describe("actionLabel", () => {
  it("delivers when items are required, otherwise completes", () => {
    expect(actionLabel(delivery)).toBe("交付")
    expect(actionLabel(makeMission())).toBe("完成")
  })

  it("delivers when a pet is required", () => {
    expect(
      actionLabel(
        makeMission({
          deliverPet: { monsterId: "grass-slime", name: "草原史萊姆", carried: true },
        }),
      ),
    ).toBe("交付")
  })
})

// ── 任務板新畫面（規格 missions-screen）──

const kill = (count: number, progress: number, monsterName = "史萊姆") => ({
  monsterId: "m",
  monsterName,
  placeId: 1,
  placeName: "中央平原",
  count,
  progress,
})
const deliver = (name: string, quantity: number, owned: number) => ({
  itemId: name,
  name,
  quantity,
  owned,
  category: "Material" as const,
})
const dailyMission = (id: string, level: number) =>
  makeMission({ id, level, daily: true, title: `[每日]${id}` })

describe("Kind label and title", () => {
  it.each([
    [makeMission({ title: "[每日]討伐任務", daily: true }), "討伐任務", "討伐"],
    [
      makeMission({ title: "製作漁網", kills: [], deliver: [deliver("蜘蛛絲", 1, 0)] }),
      "製作漁網",
      "交付",
    ],
    [
      makeMission({
        title: "協尋寵物",
        kills: [],
        deliverPet: { monsterId: "monster-1", name: "飛兔", carried: false },
      }),
      "協尋寵物",
      "寵物",
    ],
  ])("%#", (mission, title, kind) => {
    expect(displayTitle(mission)).toBe(title)
    expect(kindLabel(mission)).toBe(kind)
  })
})

describe("board pages", () => {
  const regular = makeMission({ id: "mission-1", level: 20 })
  const lv150 = makeMissionBoard({
    available: [
      dailyMission("mission-222", 50),
      dailyMission("mission-226", 150),
      dailyMission("mission-227", 150),
      ...Array.from({ length: 21 }, (_, index) => makeMission({ id: `mission-${index + 1}` })),
    ],
    completed: [dailyMission("mission-225", 50), makeMission({ id: "mission-90" })],
  })

  it("daily page merges completed dailies back in level then content order", () => {
    expect(dailyNotes(lv150).map((note) => [note.mission.id, note.done])).toEqual([
      ["mission-222", false],
      ["mission-225", true],
      ["mission-226", false],
      ["mission-227", false],
    ])
  })

  it("regular page and the completed pile", () => {
    expect(regularNotes(lv150)).toHaveLength(21)
    expect(completedRegular(lv150).map((mission) => mission.id)).toEqual(["mission-90"])
  })

  // 規格 Opening page and counts
  it.each([
    [
      "Lv 20, no daily open",
      makeMissionBoard({ available: [regular] }),
      "regular",
      { daily: 0, regular: 1 },
    ],
    ["Lv 150", lv150, "daily", { daily: 3, regular: 21 }],
    [
      "every unlocked daily in hand",
      makeMissionBoard({ available: [regular], active: [dailyMission("mission-222", 50)] }),
      "regular",
      { daily: 0, regular: 1 },
    ],
    [
      "one daily completed today, the rest in hand",
      makeMissionBoard({
        available: [regular],
        active: [dailyMission("mission-222", 50)],
        completed: [dailyMission("mission-225", 50)],
      }),
      "daily",
      { daily: 0, regular: 1 },
    ],
  ])("%s", (_name, board, page, counts) => {
    expect(openingPage(board)).toBe(page)
    expect(pageCounts(board)).toEqual(counts)
  })
})

// 規格 Reset hours：台北 09:00 重置（UTC 01:00）
describe("Reset hours", () => {
  const nextReset = new Date("2026-11-16T01:00:00Z")
  it.each([
    ["09:00", "2026-11-15T01:00:00Z", 24],
    ["08:10", "2026-11-16T00:10:00Z", 1],
    ["21:30", "2026-11-15T13:30:00Z", 12],
  ])("%s", (_taipei, now, hours) => {
    expect(resetHours(nextReset, new Date(now))).toBe(hours)
  })

  it("never shows less than 1", () => {
    expect(resetHours(nextReset, new Date("2026-11-16T01:00:30Z"))).toBe(1)
  })
})

describe("objectives and hand slips", () => {
  const skillMission = (learnedLevel: number | null) =>
    makeMission({
      kills: [kill(3, 3)],
      deliver: [deliver("蜘蛛絲", 2, 2)],
      requiredSkill: { skillId: "skill-414", name: "施毒", level: 3, met: false, learnedLevel },
      ready: false,
    })

  it("lists every objective with its progress", () => {
    const mission = makeMission({
      kills: [kill(3, 1)],
      deliver: [deliver("蜘蛛絲", 3, 5)],
      deliverPet: { monsterId: "monster-1", name: "飛兔", carried: false },
      requiredSkill: { skillId: "skill-414", name: "施毒", level: 2, met: false, learnedLevel: 1 },
    })

    expect(objectiveRows(mission, true)).toMatchObject([
      { text: "擊敗 史萊姆", sub: "中央平原", amount: "1 / 3", met: false, short: false },
      { text: "交付 蜘蛛絲", amount: "3 / 3", met: true, short: false, category: "Material" },
      { text: "交付寵物 飛兔", sub: null, amount: "未攜帶", met: false, short: true },
      { text: "技能 施毒 Lv2", amount: "目前 Lv1", met: false, short: true },
    ])
    expect(objectiveRows(mission, false).map((row) => [row.amount, row.sub])).toEqual([
      ["×3", "中央平原"],
      ["×3", null],
      ["", "需攜帶"],
      ["目前 Lv1", null],
    ])
    expect(objectiveRows(skillMission(null), true)[2].amount).toBe("未學會")
  })

  it("hand slip shows the first objective and how many more", () => {
    const slip = handSlip(
      makeMission({
        title: "[每日]紅蓮與奔雷",
        daily: true,
        kills: [],
        deliver: [
          deliver("火", 3, 2),
          deliver("雷", 3, 0),
          deliver("風", 1, 0),
          deliver("土", 1, 0),
        ],
      }),
    )

    expect(slip).toMatchObject({ title: "紅蓮與奔雷", daily: true, more: 3, stamp: null })
    expect(slip.first).toMatchObject({ text: "交付 火", amount: "2 / 3" })
  })

  it.each([
    ["kills only", makeMission({ kills: [kill(3, 3)], ready: true }), "達成"],
    [
      "with a delivery",
      makeMission({ kills: [], deliver: [deliver("火", 1, 1)], ready: true }),
      "可交",
    ],
    ["only the skill is missing", skillMission(2), null],
  ])("stamp: %s", (_name, mission, stamp) => {
    expect(handSlip(mission).stamp).toBe(stamp)
  })

  // 規格 Progress bar
  it.each([
    ["kill 3/3, kill 1/3", makeMission({ kills: [kill(3, 3), kill(3, 1)] }), 4 / 6],
    [
      "deliver 2/3, pet not carried",
      makeMission({
        kills: [],
        deliver: [deliver("火", 3, 2)],
        deliverPet: { monsterId: "m", name: "飛兔", carried: false },
      }),
      2 / 4,
    ],
    [
      "kill 5/5, skill not met",
      makeMission({
        kills: [kill(5, 5)],
        requiredSkill: { skillId: "s", name: "施毒", level: 3, met: false, learnedLevel: 2 },
      }),
      5 / 6,
    ],
  ])("ratio: %s", (_name, mission, ratio) => {
    expect(handSlip(mission).ratio).toBe(ratio)
  })
})

describe("sheet text", () => {
  it("joins the conditions of an available mission", () => {
    const mission = makeMission({
      level: 5500,
      requirements: {
        jobId: "job-6",
        sex: 2,
        birth: 1,
        prerequisiteId: "mission-1",
        prerequisiteTitle: "製作漁網",
        jobName: "戰士",
        sexName: "女",
        birthName: "中央大陸",
      },
    })

    expect(requirementText(mission)).toBe(
      "Lv 5,500・限 戰士・限 女性・限 中央大陸出身・需先完成「製作漁網」",
    )
    expect(requirementText(makeMission({ level: 20 }))).toBe("Lv 20")
  })

  it("lists the rewards", () => {
    expect(
      rewardRows({
        experience: 3800,
        money: 15000,
        items: [{ itemId: "item-342", name: "白色布料", quantity: 1, category: "Material" }],
        skill: { skillId: "s", name: "施毒", level: 1 },
        pet: { monsterId: "m", name: "飛兔", level: 2 },
      }),
    ).toEqual([
      { text: "經驗 3,800", icon: null, category: null, itemId: null },
      { text: "15,000", icon: "coin", category: null, itemId: null },
      { text: "白色布料 ×1", icon: null, category: "Material", itemId: "item-342" },
      { text: "技能 施毒 Lv1", icon: null, category: null, itemId: null },
      { text: "寵物 飛兔 Lv2", icon: null, category: null, itemId: null },
    ])
    expect(
      rewardRows({ experience: 190, money: 0, items: [], skill: null, pet: null }),
    ).toHaveLength(1)
  })

  const completion = (overrides: Partial<Parameters<typeof MissionCompletionVo.create>[0]> = {}) =>
    MissionCompletionVo.create({
      board: makeMissionBoard(),
      character: makeCharacter({ level: 50 }),
      completionMessage: "",
      experience: 9500,
      money: 0,
      items: [],
      skill: null,
      skillGranted: false,
      levelsGained: 0,
      pet: null,
      ...overrides,
    })

  it("lists what the completion gave", () => {
    expect(
      completionRows(
        completion({
          money: 300,
          items: [{ itemId: "item-342", name: "白色布料", quantity: 2, category: "Material" }],
          skill: { skillId: "s", name: "施毒", level: 1 },
          skillGranted: true,
          pet: { monsterId: "m", name: "飛兔", level: 2 },
          levelsGained: 1,
        }),
      ),
    ).toEqual([
      {
        text: "獲得經驗 9,500、金錢 300",
        icon: "coin",
        category: null,
        itemId: null,
        kind: "normal",
      },
      {
        text: "獲得 白色布料 ×2",
        icon: null,
        category: "Material",
        itemId: "item-342",
        kind: "normal",
      },
      { text: "學會了 施毒 Lv1", icon: null, category: null, itemId: null, kind: "normal" },
      { text: "獲得寵物 飛兔 Lv2", icon: null, category: null, itemId: null, kind: "normal" },
      { text: "升到 Lv50", icon: null, category: null, itemId: null, kind: "level" },
    ])
  })

  it("says when a known skill was not learned and leaves out money it did not give", () => {
    expect(
      completionRows(
        completion({ skill: { skillId: "s", name: "施毒", level: 1 }, skillGranted: false }),
      ),
    ).toEqual([
      { text: "獲得經驗 9,500", icon: null, category: null, itemId: null, kind: "normal" },
      {
        text: "已經學會施毒，這次沒有學到",
        icon: null,
        category: null,
        itemId: null,
        kind: "missed",
      },
    ])
  })
})

// 規格 Lise speaks feedback and errors
describe("liseLine", () => {
  const mission = makeMission({
    title: "[每日]討伐任務",
    deliverPet: { monsterId: "m", name: "飛兔", carried: false },
    requiredSkill: { skillId: "s", name: "施毒", level: 3, met: false, learnedLevel: 2 },
    rewards: {
      experience: 1,
      money: 0,
      items: [{ itemId: "a", name: "白色布料", quantity: 1, category: "Material" }],
      skill: null,
      pet: null,
    },
  })
  const refused = (...reasons: string[]) =>
    liseLine({ kind: "refused", reasons, mission, activeLimit: 3 })

  it.each([
    [liseLine({ kind: "accepted", mission }), "「討伐任務」就拜託您了，路上小心！", false],
    [liseLine({ kind: "abandoned" }), "好的，這張先放回佈告欄囉。", false],
    [refused("ITEMS_MISSING"), "交付的東西好像還差一點呢……", true],
    [refused("OBJECTIVE_INCOMPLETE"), "討伐的數量好像還差一點呢……", true],
    [refused("PET_MISSING"), "咦，您身邊沒有帶著飛兔呢……", true],
    [refused("SKILL_TOO_LOW"), "施毒要練到 Lv3 才行喔。", true],
    [refused("BAG_FULL"), "哎呀，材料的格子已經滿了……", true],
    [refused("PETS_FULL"), "獸欄已經住滿 3 隻了，新夥伴沒地方住呢……", true],
    [refused("TOO_MANY"), "您手邊已經有 3 張委託了喔。", true],
    [
      liseLine({ kind: "refused", reasons: ["TOO_MANY"], mission, activeLimit: 4 }),
      "您手邊已經有 4 張委託了喔。",
      true,
    ],
    [refused("MISSION_NOT_ACTIVE"), "這張委託已經不在您手上了呢……", true],
    [refused("LEVEL_TOO_LOW"), "咦，剛剛沒送出去……再試一次吧？", true],
    [liseLine({ kind: "failed" }), "咦，剛剛沒送出去……再試一次吧？", true],
    [refused("PETS_FULL", "BAG_FULL"), "哎呀，材料的格子已經滿了……", true],
  ])("%#", (line, text, failed) => {
    expect(line).toEqual({ text, failed })
  })

  it("says the bag when reward items are of several categories", () => {
    const mixed = makeMission({
      rewards: {
        experience: 1,
        money: 0,
        items: [
          { itemId: "a", name: "白色布料", quantity: 1, category: "Material" },
          { itemId: "b", name: "藥水", quantity: 1, category: "Consumable" },
        ],
        skill: null,
        pet: null,
      },
    })
    expect(
      liseLine({ kind: "refused", reasons: ["BAG_FULL"], mission: mixed, activeLimit: 3 }).text,
    ).toBe("哎呀，背包的格子已經滿了……")
  })
})

// 規格 Layout at the extremes
describe("Mission board layout follows the stage height", () => {
  it.each([
    [624, 394, 85, 488, 128, 8, 306, 418],
    [683, 435, 98, 529, 143, 11, 299, 456],
    [720, 460, 107, 554, 153, 13, 294, 480],
    [780, 502, 112, 596, 168, 16, 291, 518],
  ])("H %i", (height, boardHeight, rowHeight, handTop, handHeight, handMargin, x, y) => {
    expect(missionsLayout(height)).toEqual({
      boardHeight,
      rowHeight,
      handTop,
      handHeight,
      handMargin,
    })
    expect(missionsRevealOrigin(height)).toEqual({ x, y })
  })
})
