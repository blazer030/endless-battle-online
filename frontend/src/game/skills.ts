import { statLabel } from "./stats"
import type { JobOption, SkillOverview, SkillSlotCommand } from "./types"

// 與後端 SkillLoadout 相同：1～3 主動欄（發動率由技能等級固定），4～5 被動欄（常駐）；兩種欄位都不限職業
export type SlotNumber = 1 | 2 | 3 | 4 | 5
export type SlotDraft = { slot: SlotNumber; skillId: string | null }

export const ActiveSlots: readonly SlotNumber[] = [1, 2, 3]
export const PassiveSlots: readonly SlotNumber[] = [4, 5]
// 熟練度累積到此值顯示 100%
const FullProficiency = 3500

const allSlots: readonly SlotNumber[] = [...ActiveSlots, ...PassiveSlots]

export const isActiveSlot = (slot: number) => ActiveSlots.some((active) => active === slot)
const isPassiveSlot = (slot: number) => PassiveSlots.some((passive) => passive === slot)

export const draftsFrom = (overview: SkillOverview): SlotDraft[] =>
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

// 主動欄裝入技能目前等級的發動率；空欄、找不到技能或被動欄回傳 null
export const equippedChance = (
  drafts: SlotDraft[],
  overview: SkillOverview,
  slot: SlotNumber,
): number | null => {
  if (!isActiveSlot(slot)) return null
  const skillId = drafts.find((draft) => draft.slot === slot)?.skillId ?? null
  const skill = overview.skills.find((candidate) => candidate.id === skillId)
  return skill === undefined ? null : skill.chance
}

// 空欄不送，後端把未列出的欄位視為清空
export const toSlotCommands = (drafts: SlotDraft[]): SkillSlotCommand[] =>
  drafts.flatMap(({ slot, skillId }) => (skillId === null ? [] : [{ slot, skillId }]))

// 鏡射後端 SkillLoadout.Validate：代碼與順序相同，送出前就能顯示原因
export const validateLoadout = (drafts: SlotDraft[], overview: SkillOverview): string[] => {
  const reasons: string[] = []
  const seenSlots = new Set<number>()
  const seenSkills = new Set<string>()

  for (const { slot, skillId } of drafts) {
    if (skillId === null) continue
    const isActive = isActiveSlot(slot)
    if (!isActive && !isPassiveSlot(slot)) reasons.push("slotOutOfRange")
    if (seenSlots.has(slot)) reasons.push("duplicateSlot")
    seenSlots.add(slot)

    const skill = overview.skills.find((candidate) => candidate.id === skillId)
    if (skill === undefined || skill.level === 0) {
      reasons.push("notLearned")
      continue
    }
    if (seenSkills.has(skillId)) reasons.push("duplicateSkill")
    seenSkills.add(skillId)

    if (isActive && skill.type === "Passive") reasons.push("passiveInActiveSlot")
    else if (isPassiveSlot(slot) && skill.type === "Active") reasons.push("activeInPassiveSlot")
  }
  return [...new Set(reasons)]
}

export const proficiencyPercent = (value: number) =>
  Math.max(0, Math.min(100, Math.floor((value / FullProficiency) * 100)))

const translate = (texts: Record<string, string>) => (code: string) => texts[code] ?? code

const fixedJobReasonText = translate({
  sameJob: "已是目前職業",
  levelTooLow: "等級不足",
  parentProficiencyTooLow: "前置職業熟練度不足",
})

const StatTooLowPrefix = "statTooLow:"

export const jobReasonText = (code: string) =>
  code.startsWith(StatTooLowPrefix)
    ? `${statLabel(code.slice(StatTooLowPrefix.length))}不足`
    : fixedJobReasonText(code)

export const learnReasonText = translate({
  wrongJob: "不是目前職業的技能",
  maxLevel: "已達最高等級",
  notEnoughProficiency: "技能熟練度不足",
  notEnoughMoney: "金錢不足",
})

export const slotReasonText = translate({
  slotOutOfRange: "欄位編號不正確",
  duplicateSlot: "欄位重複",
  notLearned: "尚未學會這個技能",
  duplicateSkill: "同一技能不能裝在兩個欄位",
  activeInPassiveSlot: "被動欄只能放被動技能",
  passiveInActiveSlot: "主動欄只能放主動技能",
})

const SameJob = "sameJob"

// 轉職成功但列表沒能重新載入時，在本地改標目前職業；其他條件沿用上次的判定
export const markCurrentJob = (jobs: JobOption[], currentJobId: string): JobOption[] =>
  jobs.map((job) => {
    const isCurrent = job.id === currentJobId
    const others = job.reasons.filter((code) => code !== SameJob)
    const reasons = isCurrent ? [SameJob, ...others] : others
    return { ...job, current: isCurrent, reasons, eligible: reasons.length === 0 }
  })
