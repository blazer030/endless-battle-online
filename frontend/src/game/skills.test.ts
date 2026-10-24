import { describe, expect, it } from "vitest"
import {
  clearSlot,
  activationText,
  cooldownText,
  draftsFrom,
  equippedActivation,
  jobReasonText,
  learnReasonText,
  markCurrentJob,
  proficiencyPercent,
  setSlot,
  slotReasonText,
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
  chance: 40,
  cooldown: 0,
  trigger: { kind: "Chance", percent: 0 },
  next: null,
  ...overrides,
})

const overview: SkillOverview = {
  jobId: "warrior",
  skillPoints: 0,
  skills: [
    skill({}),
    skill({
      id: "iron-skin",
      name: "硬甲",
      type: "Passive",
      effect: "DfMultiplier",
      chance: 0,
      trigger: null,
    }),
    skill({
      id: "heal",
      name: "治癒",
      effect: "Heal",
      jobId: "bard",
      chance: 0,
      cooldown: 4,
      trigger: { kind: "HpBelow", percent: 50 },
    }),
    skill({ id: "combo", name: "連擊", jobId: "thief", effect: "MultiHit", level: 3, chance: 60 }),
    skill({
      id: "swift-shadow",
      name: "急影",
      type: "Passive",
      jobId: "thief",
      chance: 0,
      trigger: null,
    }),
    skill({ id: "war-cry", name: "未學", level: 0, chance: 0 }),
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

  // 主動欄顯示裝入技能的發動條件：擲骰的顯示發動率，hpBelow 顯示門檻；空欄與被動欄沒有
  it("equippedActivation_DescribesTheSkillsOwnTrigger", () => {
    expect(equippedActivation(drafts, overview, 1)).toBe("發動率 40%")
    expect(equippedActivation(setSlot(drafts, 2, "combo"), overview, 2)).toBe("發動率 60%")
    expect(equippedActivation(setSlot(drafts, 2, "heal"), overview, 2)).toBe("HP 低於 50% 自動發動")
    expect(equippedActivation(drafts, overview, 3)).toBeNull()
    expect(equippedActivation(drafts, overview, 4)).toBeNull()
  })

  it("activationText_AndCooldownText_DescribeTriggerAndCooldown", () => {
    expect(activationText({ kind: "Chance", percent: 0 }, 70)).toBe("發動率 70%")
    expect(activationText({ kind: "HpBelow", percent: 30 }, 0)).toBe("HP 低於 30% 自動發動")
    expect(activationText(null, 0)).toBeNull()
    expect(cooldownText(4)).toBe("冷卻 4 回合")
    expect(cooldownText(0)).toBeNull()
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
