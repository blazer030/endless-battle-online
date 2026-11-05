import { describe, expect, it } from "vitest"
import {
  clearSlot,
  draftsFrom,
  equippedSummary,
  jobReasonText,
  learnReasonText,
  markCurrentJob,
  proficiencyPercent,
  setSlot,
  slotReasonText,
  timingText,
  toSlotCommands,
  validateLoadout,
} from "./skills"
import type { SlotDraft } from "./skills"
import type { JobOption, SkillOverview, SkillView } from "./types"
import { makeJob } from "../test/fixtures"

const skill = (overrides: Partial<SkillView>): SkillView => ({
  id: "burst",
  name: "爆氣",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "",
  icon: null,
  level: 1,
  maxLevel: 5,
  summary: "機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合",
  next: null,
  ...overrides,
})

const overview: SkillOverview = {
  jobId: "warrior",
  skillPoints: 0,
  skills: [
    skill({}),
    skill({ id: "iron-skin", name: "硬甲", slotType: "Passive", summary: "常駐｜自己物防 +1%" }),
    skill({
      id: "heal",
      name: "治癒",
      summary: "HP 低於 50%｜消耗 SP 5｜回復 HP 智力×100%，1 回合｜冷卻 4 回合",
    }),
    skill({
      id: "combo",
      name: "連擊",
      level: 3,
      summary: "機率 60%｜消耗 SP 11｜物理連擊 4 次，3 回合",
    }),
    skill({ id: "swift-shadow", name: "急影", slotType: "Passive", summary: "常駐｜自己速度 +1%" }),
    skill({ id: "war-cry", name: "未學", level: 0, summary: null }),
  ],
  slots: [
    { slot: 1, skillId: "burst" },
    { slot: 4, skillId: "iron-skin" },
  ],
}

const drafts = draftsFrom(overview)
const draftOf = (all: SlotDraft[], slot: number) => all.find((draft) => draft.slot === slot)

describe("skills", () => {
  it("draftsFrom_FillsFiveSlotsWithEmptyOnes", () => {
    expect(drafts).toEqual([
      { slot: 1, skillId: "burst" },
      { slot: 2, skillId: null },
      { slot: 3, skillId: null },
      { slot: 4, skillId: "iron-skin" },
      { slot: 5, skillId: null },
    ])
  })

  it("setSlot_AndClearSlot_ChangeOnlyThatSlot", () => {
    const set = setSlot(drafts, 2, "combo")
    expect(draftOf(set, 2)).toEqual({ slot: 2, skillId: "combo" })
    expect(draftOf(set, 1)).toBe(draftOf(drafts, 1))

    const cleared = clearSlot(set, 1)
    expect(draftOf(cleared, 1)).toEqual({ slot: 1, skillId: null })
  })

  // 主動欄顯示裝入技能目前等級的效果文字；空欄、未學與被動欄沒有
  it("equippedSummary_ShowsTheSkillsCurrentSummary", () => {
    expect(equippedSummary(drafts, overview, 1)).toBe("機率 40%｜消耗 SP 5｜自己物攻 +1%，3 回合")
    expect(equippedSummary(setSlot(drafts, 2, "heal"), overview, 2)).toBe(
      "HP 低於 50%｜消耗 SP 5｜回復 HP 智力×100%，1 回合｜冷卻 4 回合",
    )
    expect(equippedSummary(setSlot(drafts, 2, "war-cry"), overview, 2)).toBeNull()
    expect(equippedSummary(drafts, overview, 3)).toBeNull()
    expect(equippedSummary(drafts, overview, 4)).toBeNull()
  })

  it("timingText_MarksSkillsThatActivateAfterAHit", () => {
    expect(timingText("AfterHit")).toBe("命中後")
    expect(timingText("BeforeAttack")).toBeNull()
  })

  it("toSlotCommands_SendsOnlyFilledSlots", () => {
    expect(toSlotCommands(drafts)).toEqual([
      { slot: 1, skillId: "burst" },
      { slot: 4, skillId: "iron-skin" },
    ])
  })

  it("validateLoadout_AcceptsTheServerLoadout", () => {
    expect(validateLoadout(drafts, overview)).toEqual([])
  })

  it("validateLoadout_SkillsOfOtherJobsAreAllowedInBothSlotKinds", () => {
    expect(validateLoadout(setSlot(drafts, 2, "combo"), overview)).toEqual([])
    expect(validateLoadout(setSlot(drafts, 5, "swift-shadow"), overview)).toEqual([])
  })

  it("validateLoadout_ReportsTheBackendCodes", () => {
    const cases: [SlotDraft[], string[]][] = [
      [setSlot(drafts, 2, "burst"), ["duplicateSkill"]],
      [setSlot(drafts, 5, "combo"), ["activeInPassiveSlot"]],
      [setSlot(drafts, 2, "iron-skin"), ["passiveInActiveSlot", "duplicateSkill"]],
      [setSlot(drafts, 3, "war-cry"), ["notLearned"]],
      [setSlot(drafts, 3, "unknown"), ["notLearned"]],
      [[...drafts, { slot: 1, skillId: "combo" }], ["duplicateSlot"]],
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
    expect(slotReasonText("activeInPassiveSlot")).toBe("被動欄只能放被動技能")
    expect(slotReasonText("somethingNew")).toBe("somethingNew")
  })
})
