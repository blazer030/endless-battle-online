import { describe, expect, it } from "vitest"
import {
  clearSlot,
  draftsFrom,
  jobReasonText,
  learnReasonText,
  markCurrentJob,
  proficiencyPercent,
  setSlot,
  slotReasonText,
  stepChance,
  toSlotCommands,
  validateLoadout,
} from "./skills"
import type { SlotDraft } from "./skills"
import type { JobOption, SkillOverview, SkillView } from "./types"
import { makeJob } from "../test/fixtures"

const skill = (overrides: Partial<SkillView>): SkillView => ({
  id: "burst",
  name: "爆氣",
  type: "Active",
  effect: "AtMultiplier",
  description: "",
  jobId: "warrior",
  level: 1,
  maxLevel: 5,
  next: null,
  ...overrides,
})

const overview: SkillOverview = {
  jobId: "warrior",
  skillPoints: 0,
  skills: [
    skill({}),
    skill({ id: "iron-skin", name: "硬甲", type: "Passive", effect: "DfMultiplier" }),
    skill({ id: "combo", name: "連擊", jobId: "thief", effect: "MultiHit" }),
    skill({ id: "swift-shadow", name: "急影", type: "Passive", jobId: "thief" }),
    skill({ id: "war-cry", name: "未學", level: 0 }),
  ],
  slots: [
    { slot: 1, skillId: "burst", chance: 70 },
    { slot: 4, skillId: "iron-skin", chance: 30 },
  ],
}

const drafts = draftsFrom(overview)
const draftOf = (all: SlotDraft[], slot: number) => all.find((draft) => draft.slot === slot)

describe("skills", () => {
  it("draftsFrom_FillsFiveSlotsWithDefaultsForEmptyOnes", () => {
    expect(drafts).toEqual([
      { slot: 1, skillId: "burst", chance: 70 },
      { slot: 2, skillId: null, chance: 50 },
      { slot: 3, skillId: null, chance: 50 },
      { slot: 4, skillId: "iron-skin", chance: 30 },
      { slot: 5, skillId: null, chance: 50 },
    ])
  })

  it("setSlot_AndClearSlot_ChangeOnlyThatSlot", () => {
    const set = setSlot(drafts, 2, "combo")
    expect(draftOf(set, 2)).toEqual({ slot: 2, skillId: "combo", chance: 50 })
    expect(draftOf(set, 1)).toBe(draftOf(drafts, 1))

    const cleared = clearSlot(set, 1)
    expect(draftOf(cleared, 1)).toEqual({ slot: 1, skillId: null, chance: 70 })
  })

  it("stepChance_MovesByTenWithinTenToHundred", () => {
    expect(draftOf(stepChance(drafts, 1, 1), 1)?.chance).toBe(80)
    expect(draftOf(stepChance(drafts, 1, -1), 1)?.chance).toBe(60)

    const atTop = stepChance(stepChance(stepChance(drafts, 1, 1), 1, 1), 1, 1)
    expect(draftOf(atTop, 1)?.chance).toBe(100)
    expect(draftOf(stepChance(atTop, 1, 1), 1)?.chance).toBe(100)

    const atBottom = [10, 20, 30, 40, 50, 60].reduce(
      (current) => stepChance(current, 1, -1),
      drafts,
    )
    expect(draftOf(atBottom, 1)?.chance).toBe(10)
  })

  it("toSlotCommands_SendsOnlyFilledSlots", () => {
    expect(toSlotCommands(drafts)).toEqual([
      { slot: 1, skillId: "burst", chance: 70 },
      { slot: 4, skillId: "iron-skin", chance: 30 },
    ])
  })

  it("validateLoadout_AcceptsTheServerLoadout", () => {
    expect(validateLoadout(drafts, overview)).toEqual([])
  })

  it("validateLoadout_ActiveSkillsOfOtherJobsAreAllowed", () => {
    expect(validateLoadout(setSlot(drafts, 2, "combo"), overview)).toEqual([])
  })

  it("validateLoadout_ReportsTheBackendCodes", () => {
    const cases: [SlotDraft[], string[]][] = [
      [setSlot(drafts, 2, "burst"), ["duplicateSkill"]],
      [setSlot(drafts, 5, "swift-shadow"), ["passiveNotCurrentJob"]],
      [setSlot(drafts, 5, "combo"), ["activeInPassiveSlot"]],
      [setSlot(drafts, 2, "iron-skin"), ["passiveInActiveSlot", "duplicateSkill"]],
      [setSlot(drafts, 3, "war-cry"), ["notLearned"]],
      [setSlot(drafts, 3, "unknown"), ["notLearned"]],
      [
        drafts.map((draft) => (draft.slot === 1 ? { ...draft, chance: 55 } : draft)),
        ["chanceOutOfRange"],
      ],
      [[...drafts, { slot: 1, skillId: "combo", chance: 50 }], ["duplicateSlot"]],
    ]
    for (const [loadout, reasons] of cases)
      expect(validateLoadout(loadout, overview)).toEqual(reasons)
  })

  it("markCurrentJob_MovesCurrentAndSameJobToTheNewJob", () => {
    const option = (id: string, current: boolean, reasons: string[]): JobOption => ({
      ...makeJob({ id }),
      parentName: null,
      current,
      proficiencyPercent: 0,
      eligible: reasons.length === 0,
      reasons,
    })
    const jobs = [
      option("warrior", true, ["sameJob"]),
      option("thief", false, []),
      option("knight", false, ["levelTooLow"]),
    ]

    expect(
      markCurrentJob(jobs, "thief").map(({ id, current, eligible, reasons }) => ({
        id,
        current,
        eligible,
        reasons,
      })),
    ).toEqual([
      { id: "warrior", current: false, eligible: true, reasons: [] },
      { id: "thief", current: true, eligible: false, reasons: ["sameJob"] },
      { id: "knight", current: false, eligible: false, reasons: ["levelTooLow"] },
    ])
  })

  it("proficiencyPercent_FloorsAndCapsAtHundred", () => {
    expect(proficiencyPercent(-5)).toBe(0)
    expect(proficiencyPercent(0)).toBe(0)
    expect(proficiencyPercent(3499)).toBe(99)
    expect(proficiencyPercent(3500)).toBe(100)
    expect(proficiencyPercent(99999)).toBe(100)
  })

  it("reasonTexts_TranslateCodesToChinese", () => {
    expect(jobReasonText("sameJob")).toBe("已是目前職業")
    expect(jobReasonText("levelTooLow")).toBe("等級不足")
    expect(jobReasonText("parentProficiencyTooLow")).toBe("前置職業熟練度不足")
    expect(jobReasonText("statTooLow:vit")).toBe("體質不足")
    expect(learnReasonText("wrongJob")).toBe("不是目前職業的技能")
    expect(learnReasonText("maxLevel")).toBe("已達最高等級")
    expect(learnReasonText("notEnoughProficiency")).toBe("技能熟練度不足")
    expect(learnReasonText("notEnoughMoney")).toBe("金錢不足")
    expect(slotReasonText("duplicateSkill")).toBe("同一技能不能裝在兩個欄位")
    expect(slotReasonText("passiveNotCurrentJob")).toBe("被動欄只能放目前職業的技能")
    expect(slotReasonText("somethingNew")).toBe("somethingNew")
  })
})
