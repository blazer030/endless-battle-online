import { statLabel } from "./stats"
import type { JobChoiceVo, Skill, SkillOverviewVo, SkillSlotCommand } from "./skillOverview"

// 與後端 SkillLoadout 相同：1～3 主動欄、4～5 被動欄；兩種欄位都不限職業，規則相同、只差欄位
export type SlotNumber = 1 | 2 | 3 | 4 | 5
export type SlotDraft = { slot: SlotNumber; skillId: string | null }

export const ActiveSlots: readonly SlotNumber[] = [1, 2, 3]
export const PassiveSlots: readonly SlotNumber[] = [4, 5]
// 熟練度累積到此值顯示 100%
const FullProficiency = 3500

const allSlots: readonly SlotNumber[] = [...ActiveSlots, ...PassiveSlots]

export const isActiveSlot = (slot: number) => ActiveSlots.some((active) => active === slot)
const isPassiveSlot = (slot: number) => PassiveSlots.some((passive) => passive === slot)

export const draftsFrom = (overview: SkillOverviewVo): SlotDraft[] =>
  allSlots.map((slot) => ({
    slot,
    skillId: overview.slots.find((entry) => entry.slot === slot)?.skillId ?? null,
  }))

const updateSlot = (
  drafts: SlotDraft[],
  slot: SlotNumber,
  update: (draft: SlotDraft) => SlotDraft,
) => drafts.map((draft) => (draft.slot === slot ? update(draft) : draft))

export const setSlot = (drafts: SlotDraft[], slot: SlotNumber, skillId: string) =>
  updateSlot(drafts, slot, (draft) => ({ ...draft, skillId }))

export const clearSlot = (drafts: SlotDraft[], slot: SlotNumber) =>
  updateSlot(drafts, slot, (draft) => ({ ...draft, skillId: null }))

// 命中後、回合結束才發動的技能在效果文字前標出時機；出手前是預設，不標
export const timingText = (timing: Skill["timing"]): string | null =>
  timing === "AfterHit" ? "命中後" : timing === "RoundEnd" ? "回合結束" : null

// 主動欄裝入技能目前等級的效果文字；空欄、找不到技能、未學或被動欄回傳 null
export const equippedSummary = (
  drafts: SlotDraft[],
  overview: SkillOverviewVo,
  slot: SlotNumber,
): string | null => {
  if (!isActiveSlot(slot)) return null
  const skillId = drafts.find((draft) => draft.slot === slot)?.skillId ?? null
  return overview.skills.find((candidate) => candidate.id === skillId)?.summary ?? null
}

// 空欄不送，後端把未列出的欄位視為清空
export const toSlotCommands = (drafts: SlotDraft[]): SkillSlotCommand[] =>
  drafts.flatMap(({ slot, skillId }) => (skillId === null ? [] : [{ slot, skillId }]))

// 鏡射後端 SkillLoadout.Validate：代碼與順序相同，送出前就能顯示原因
export const validateLoadout = (drafts: SlotDraft[], overview: SkillOverviewVo): string[] => {
  const reasons: string[] = []
  const seenSlots = new Set<number>()
  const seenSkills = new Set<string>()

  for (const { slot, skillId } of drafts) {
    if (skillId === null) continue
    const isActive = isActiveSlot(slot)
    if (!isActive && !isPassiveSlot(slot)) reasons.push("SLOT_OUT_OF_RANGE")
    if (seenSlots.has(slot)) reasons.push("DUPLICATE_SLOT")
    seenSlots.add(slot)

    const skill = overview.skills.find((candidate) => candidate.id === skillId)
    if (skill === undefined || skill.level === 0) {
      reasons.push("NOT_LEARNED")
      continue
    }
    if (seenSkills.has(skillId)) reasons.push("DUPLICATE_SKILL")
    seenSkills.add(skillId)

    if (isActive && skill.slotType === "Passive") reasons.push("PASSIVE_IN_ACTIVE_SLOT")
    else if (isPassiveSlot(slot) && skill.slotType === "Active")
      reasons.push("ACTIVE_IN_PASSIVE_SLOT")
  }
  return [...new Set(reasons)]
}

export const proficiencyPercent = (value: number) =>
  Math.max(0, Math.min(100, Math.floor((value / FullProficiency) * 100)))

const translate = (texts: Record<string, string>) => (code: string) => texts[code] ?? code

// 條件未達成的文字：錯誤代碼對照與卡片提示共用
const JobConditionText = {
  sameJob: "已是目前職業",
  levelTooLow: "等級不足",
  parentProficiencyTooLow: "前置職業熟練度不足",
} as const

const statTooLowText = (stat: string) => `${statLabel(stat)}不足`

const LearnConditionText = {
  wrongJob: "不是目前職業的技能",
  notEnoughProficiency: "技能熟練度不足",
  notEnoughMoney: "金錢不足",
} as const

const fixedJobReasonText = translate({
  SAME_JOB: JobConditionText.sameJob,
  LEVEL_TOO_LOW: JobConditionText.levelTooLow,
  PARENT_PROFICIENCY_TOO_LOW: JobConditionText.parentProficiencyTooLow,
})

const StatTooLowPrefix = "STAT_TOO_LOW:"

export const jobReasonText = (code: string) =>
  code.startsWith(StatTooLowPrefix)
    ? statTooLowText(code.slice(StatTooLowPrefix.length))
    : fixedJobReasonText(code)

export const learnReasonText = translate({
  WRONG_JOB: LearnConditionText.wrongJob,
  MAX_LEVEL: "已達最高等級",
  NOT_ENOUGH_PROFICIENCY: LearnConditionText.notEnoughProficiency,
  NOT_ENOUGH_MONEY: LearnConditionText.notEnoughMoney,
  PREREQUISITE: "前置技能等級不足",
})

// 技能卡上的未達成條件：前置技能寫出名稱與要求等級；滿級（next 為 null）不列
export const unmetLearnTexts = ({ taughtByCurrentJob, prerequisite, next }: Skill): string[] =>
  next === null
    ? []
    : [
        ...(taughtByCurrentJob ? [] : [LearnConditionText.wrongJob]),
        ...(prerequisite === null || prerequisite.met
          ? []
          : [`前置技能：${prerequisite.name} Lv ${prerequisite.level}`]),
        ...(next.enoughProficiency ? [] : [LearnConditionText.notEnoughProficiency]),
        ...(next.enoughMoney ? [] : [LearnConditionText.notEnoughMoney]),
      ]

export const slotReasonText = translate({
  SLOT_OUT_OF_RANGE: "欄位編號不正確",
  DUPLICATE_SLOT: "欄位重複",
  NOT_LEARNED: "尚未學會這個技能",
  DUPLICATE_SKILL: "同一技能不能裝在兩個欄位",
  ACTIVE_IN_PASSIVE_SLOT: "被動欄只能放被動技能",
  PASSIVE_IN_ACTIVE_SLOT: "主動欄只能放主動技能",
})

const conditionsMet = (job: JobChoiceVo) =>
  job.levelMet && job.parentProficiencyMet && job.statRequirements.every(({ met }) => met)

// 轉職按鈕的提示：還沒達成的條件
export const unmetJobTexts = (job: JobChoiceVo): string[] => [
  ...(job.current ? [JobConditionText.sameJob] : []),
  ...(job.levelMet ? [] : [JobConditionText.levelTooLow]),
  ...(job.parentProficiencyMet ? [] : [JobConditionText.parentProficiencyTooLow]),
  ...job.statRequirements.filter(({ met }) => !met).map(({ stat }) => statTooLowText(stat)),
]

// 轉職成功但列表沒能重新載入時，在本地改標目前職業；其他條件沿用上次的判定
export const markCurrentJob = (jobs: JobChoiceVo[], currentJobId: string): JobChoiceVo[] =>
  jobs.map((job) => {
    const current = job.id === currentJobId
    return { ...job, current, eligible: !current && conditionsMet(job) }
  })
