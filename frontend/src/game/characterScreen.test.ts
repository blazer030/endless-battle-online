import { describe, expect, it } from "vitest"
import { makeCharacter } from "../test/fixtures"
import {
  abilityRows,
  characterCard,
  characterLayout,
  characterRevealOrigin,
  combatRows,
  jobSwitcher,
  learnFailureText,
  learnedPopup,
  portraitOffset,
  skillCard,
} from "./characterScreen"
import { CreationOptionsVo } from "./creationOptions"
import type { LearnableSkill, SkillJob } from "./skillOverview"

const options = CreationOptionsVo.create({
  sexes: [
    { id: 1, name: "男" },
    { id: 2, name: "女" },
  ],
  elements: [{ id: 3, name: "火", strongAgainst: 4, weakAgainst: 2 }],
  births: [{ id: 1, name: "中央大陸" }],
})

const rows = (...texts: [string, string, string]) =>
  [
    { kind: "Trigger", text: texts[0] },
    { kind: "Cost", text: texts[1] },
    { kind: "Effect", text: texts[2] },
  ] as const

const skill = (overrides: Partial<LearnableSkill> = {}): LearnableSkill => ({
  id: "skill-1",
  name: "爆氣",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "爆發體內的鬥氣",
  icon: "skill-1",
  level: 6,
  maxLevel: 10,
  rows: rows("機率 90%", "SP 12", "自己物攻 +6%，3 回合"),
  prerequisite: null,
  next: {
    level: 7,
    money: 27000,
    skillPoints: 2587,
    rows: rows("機率 100%", "SP 12", "自己物攻 +7%，3 回合"),
    canLearn: true,
    enoughMoney: true,
    enoughSkillPoints: true,
  },
  ...overrides,
})

const job = (overrides: Partial<SkillJob> = {}): SkillJob => ({
  jobId: "job-6",
  jobName: "戰士",
  tier: 1,
  current: true,
  skillPoints: 2587,
  skills: [
    skill(),
    skill({ id: "skill-61", level: 3 }),
    skill({ id: "skill-71", level: 1 }),
    skill({ id: "skill-77", level: 0, rows: null }),
  ],
  ...overrides,
})

describe("characterScreen", () => {
  // 規格 character-screen「Card values」
  it("characterCard_ShowsTheRegistration", () => {
    const character = makeCharacter({
      name: "收尾者",
      level: 162,
      jobId: "job-6",
      jobName: "戰士",
      sex: 1,
      element: 3,
      birth: 1,
      healthPoints: 6212,
      manaPoints: 980,
      experience: 42000,
      nextLevelExperience: 100000,
      jobProficiency: [{ jobId: "job-6", proficiency: 2450, skillPoints: 0 }],
      stats: { ...makeCharacter().stats, maxHealthPoints: 9640, maxManaPoints: 1205 },
    })

    expect(characterCard(character, options)).toEqual({
      name: "收尾者",
      jobName: "戰士",
      level: "Lv 162",
      proficiencyPercent: "70%",
      proficiencyText: "2,450 / 3,500",
      proficiencyRatio: 0.7,
      sexName: "男",
      elementName: "火",
      birthName: "中央大陸",
      healthPoints: { text: "6,212 / 9,640", ratio: 6212 / 9640 },
      manaPoints: { text: "980 / 1,205", ratio: 980 / 1205 },
      experience: { text: "42%", ratio: 0.42 },
    })
  })

  // 規格「Proficiency at the cap」；沒有熟練度紀錄時是 0
  it("characterCard_ProficiencyCapsAtFull", () => {
    const capped = makeCharacter({
      jobId: "job-6",
      jobProficiency: [{ jobId: "job-6", proficiency: 3500, skillPoints: 0 }],
    })
    const none = makeCharacter({ jobId: "job-6", jobProficiency: [] })

    expect(characterCard(capped, options).proficiencyPercent).toBe("100%")
    expect(characterCard(capped, options).proficiencyRatio).toBe(1)
    expect(characterCard(none, options).proficiencyText).toBe("0 / 3,500")
  })

  // 規格「Stat with an equipment bonus」「Stat without a bonus」；負的加成寫 -N
  it("abilityRows_ShowBaseAndSignedBonus", () => {
    const base = makeCharacter().baseStats
    const character = makeCharacter({
      baseStats: { ...base, strength: 412, intelligence: 96, agility: 120 },
      stats: { ...makeCharacter().stats, strength: 430, intelligence: 96, agility: 115 },
    })

    const rows = abilityRows(character)

    expect(rows.map((row) => row.label)).toEqual([
      "力量",
      "智力",
      "生命",
      "體質",
      "敏捷",
      "魅力",
      "信仰",
    ])
    expect(rows[0]).toEqual({ label: "力量", value: "412", bonus: "+18" })
    expect(rows[1]).toEqual({ label: "智力", value: "96", bonus: null })
    expect(rows[4]).toEqual({ label: "敏捷", value: "120", bonus: "-5" })
  })

  it("combatRows_ListSixValues", () => {
    const character = makeCharacter({
      stats: {
        ...makeCharacter().stats,
        attack: 1128,
        magicAttack: 210,
        defense: 864,
        magicDefense: 302,
        maxHealthPoints: 9640,
        maxManaPoints: 1205,
      },
    })

    expect(combatRows(character)).toEqual([
      { label: "物攻", value: "1,128" },
      { label: "魔攻", value: "210" },
      { label: "物防", value: "864" },
      { label: "魔防", value: "302" },
      { label: "HP 上限", value: "9,640" },
      { label: "SP 上限", value: "1,205" },
    ])
  })

  // 規格「Switcher of the current job」；以前的職業寫已學幾招、技能點；依 Class 分組
  it("jobSwitcher_HeaderAndGroups", () => {
    const jobs = [
      job(),
      job({
        jobId: "job-7",
        jobName: "術士",
        current: false,
        skillPoints: 1882,
        skills: [skill({ id: "skill-11" })],
      }),
      job({
        jobId: "job-10",
        jobName: "狂戰士",
        tier: 2,
        current: false,
        skillPoints: 0,
        skills: [],
      }),
    ]

    const current = jobSwitcher(jobs, "job-6")
    const previous = jobSwitcher(jobs, "job-10")

    expect(current.header).toEqual({
      jobId: "job-6",
      name: "戰士",
      current: true,
      subtitle: "Class I・見習・已學 3 / 4 招",
      skillPoints: "2,587",
    })
    expect(previous.header.subtitle).toBe("Class II・正式・已學 0 招")
    expect(current.groups).toEqual([
      {
        title: "CLASS I・見習",
        jobs: [
          { jobId: "job-6", name: "戰士", note: "現職", selected: true },
          { jobId: "job-7", name: "術士", note: "技能點 1,882", selected: false },
        ],
      },
      {
        title: "CLASS II・正式",
        jobs: [{ jobId: "job-10", name: "狂戰士", note: "技能點 0", selected: false }],
      },
    ])
  })

  // 規格「Cost line」：不夠的金額標出來
  it("skillCard_CostLineMarksShortAmounts", () => {
    const card = skillCard(
      skill({ next: { ...skill().next!, canLearn: false, enoughMoney: false } }),
    )

    expect(card.levelText).toBe("Lv 6/10")
    expect(card.pips).toEqual([true, true, true, true, true, true, false, false, false, false])
    expect(card.typeLabel).toBe("主動")
    expect(card.footer).toEqual({
      kind: "learn",
      costLabel: "升級需要",
      money: "27,000",
      moneyShort: true,
      skillPoints: "2,587",
      skillPointsShort: false,
      button: "升級",
      disabled: true,
      reasons: "金錢不足",
    })
  })

  // 規格「Disabled reasons」
  it("skillCard_DisabledReasonsInOrder", () => {
    const card = skillCard(
      skill({
        level: 0,
        rows: null,
        prerequisite: { skillId: "skill-61", name: "防禦心得", level: 3, met: false },
        next: { ...skill().next!, level: 1, canLearn: false, enoughSkillPoints: false },
      }),
    )

    expect(card.levelText).toBe("未習得")
    expect(card.footer).toMatchObject({
      costLabel: "學習需要",
      button: "學習",
      reasons: "前置技能：防禦心得 Lv 3・技能點不足",
    })
  })

  // 規格「Maximum level」；沒有說明時寫「（說明待補）」；被動
  it("skillCard_MaximumLevelAndPendingDescription", () => {
    const card = skillCard(skill({ level: 10, next: null, description: "", slotType: "Passive" }))

    expect(card.footer).toEqual({ kind: "max" })
    expect(card.description).toBe("（說明待補）")
    expect(card.typeLabel).toBe("被動")
  })

  // 兩欄數值：列名、下一級跟目前不同的標記；未學時目前那欄是 null、下一級不標
  it("skillCard_DetailColumns", () => {
    const learned = skillCard(skill())
    const unlearned = skillCard(skill({ level: 0, rows: null }))

    expect(learned.currentRows).toEqual([
      { label: "發動", text: "機率 90%", changed: false },
      { label: "消耗", text: "SP 12", changed: false },
      { label: "效果", text: "自己物攻 +6%，3 回合", changed: false },
    ])
    expect(learned.nextRows).toEqual([
      { label: "發動", text: "機率 100%", changed: true },
      { label: "消耗", text: "SP 12", changed: false },
      { label: "效果", text: "自己物攻 +7%，3 回合", changed: true },
    ])
    expect(unlearned.currentRows).toBeNull()
    expect(unlearned.nextRows?.every((row) => !row.changed)).toBe(true)
  })

  // 規格「Upgrade with the current job」「Learning a new skill」、升到滿級
  it("learnedPopup_UpgradedLearnedAndMastered", () => {
    const before = skill()
    const after = skill({ level: 7, rows: rows("機率 100%", "SP 12", "自己物攻 +7%，3 回合") })

    const upgraded = learnedPopup(before, after)
    const learned = learnedPopup(
      skill({ level: 0, rows: null }),
      skill({ level: 1, rows: rows("機率 40%", "SP 2", "自己物攻 +1%，3 回合") }),
    )
    const mastered = learnedPopup(skill({ level: 9 }), skill({ level: 10, next: null }))

    expect(upgraded).toMatchObject({
      title: "升級成功",
      levelText: "Lv 6 → Lv 7 / 10",
      mastered: false,
    })
    expect(upgraded.rows.map((row) => row.changed)).toEqual([true, false, true])
    expect(upgraded.pips.filter(Boolean)).toHaveLength(7)
    expect(learned).toMatchObject({
      title: "習得成功",
      levelText: "Lv 1 / 10",
      fromLevel: null,
      mastered: false,
    })
    expect(learned.rows.every((row) => !row.changed)).toBe(true)
    expect(mastered.mastered).toBe(true)
  })

  // 失敗彈窗的字：取第一個認得的原因，沒有就是連線失敗
  it("learnFailureText_FirstKnownReason", () => {
    const withPrerequisite = skill({
      prerequisite: { skillId: "skill-61", name: "防禦心得", level: 3, met: false },
    })

    expect(learnFailureText(null, withPrerequisite)).toBe("連線失敗")
    expect(learnFailureText([], withPrerequisite)).toBe("連線失敗")
    expect(learnFailureText(["SOMETHING_NEW"], withPrerequisite)).toBe("連線失敗")
    expect(learnFailureText(["NOT_ENOUGH_MONEY"], withPrerequisite)).toBe("金錢不足")
    expect(learnFailureText(["NOT_ENOUGH_PROFICIENCY", "NOT_ENOUGH_MONEY"], withPrerequisite)).toBe(
      "技能點不足",
    )
    expect(learnFailureText(["SOMETHING_NEW", "PREREQUISITE"], withPrerequisite)).toBe(
      "前置技能：防禦心得 Lv 3",
    )
    expect(learnFailureText(["MAX_LEVEL"], withPrerequisite)).toBe("已達最高等級")
    expect(learnFailureText(["WRONG_JOB"], withPrerequisite)).toBe("不是這個職業的技能")
    expect(learnFailureText(["NOT_LEARNED"], withPrerequisite)).toBe("尚未學會這個技能")
  })

  // 規格「Layout at the extremes」
  it("characterLayout_AtTheExtremes", () => {
    const shortest = characterLayout(624)
    const tallest = characterLayout(780)

    expect(shortest.scale).toBeCloseTo(0.88)
    expect(shortest.deskTop).toBeCloseTo(312.52)
    expect(shortest.portraitTop).toBeCloseTo(22.84)
    expect(shortest.feetCenter).toBeCloseTo(87.24)
    expect(shortest.journalTop).toBeCloseTo(352.52)
    expect(tallest).toEqual({
      scale: 1,
      deskTop: 352,
      portraitTop: 22,
      portraitHeight: 336,
      feetCenter: 100.5,
      journalTop: 392,
    })
  })

  // 規格「Feet offset」：術士男 +0.035、336 高 → 往左 11.76；沒量過的職業 0
  it("portraitOffset_ShiftsByFeet", () => {
    expect(portraitOffset("job-7", 1) * 336).toBeCloseTo(11.76)
    expect(portraitOffset("job-7", 2)).toBe(0.046)
    expect(portraitOffset("job-8", 1)).toBe(-0.02)
    expect(portraitOffset("job-10", 1)).toBe(0)
  })

  it("characterRevealOrigin_IsFixed", () => {
    expect(characterRevealOrigin()).toEqual({ x: 311, y: 193 })
  })
})
