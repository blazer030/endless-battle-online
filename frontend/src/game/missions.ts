import { formatNumber } from "./format"
import type { ContentNames, MissionCompletion, MissionRewardView, MissionView } from "./types"

export const MaxActiveMissions = 3

const MillisecondsPerHour = 60 * 60 * 1000

// 內容的 id 是字串；名稱表沒載到時直接顯示 id
const nameOf = (options: { id: string; name: string }[], id: string) =>
  options.find((option) => option.id === id)?.name ?? id

export const requirementLines = (mission: MissionView, content: ContentNames): string[] => {
  const { jobId, sex, birth, prerequisiteId, prerequisiteTitle } = mission.requirements
  const lines = [`Lv ${mission.level}`]
  if (jobId !== null) lines.push(`限 ${nameOf(content.jobs, jobId)}`)
  if (sex !== null) lines.push(`限 ${nameOf(content.sexes, String(sex))}性`)
  if (birth !== null) lines.push(`限 ${nameOf(content.births, String(birth))}出身`)
  if (prerequisiteId !== null) lines.push(`需先完成「${prerequisiteTitle ?? prerequisiteId}」`)
  return lines
}

export const objectiveLines = (mission: MissionView): string[] => [
  ...mission.kills
    .slice(0, 1)
    .map((kill) => `擊敗 ${kill.monsterName}（${kill.placeName}） ${kill.progress}/${kill.count}`),
  ...mission.deliver.map((item) => `交付 ${item.name} ${item.owned}/${item.quantity}`),
  ...(mission.deliverPet === null ? [] : [`交付寵物 ${mission.deliverPet.name}（需攜帶）`]),
]

export const rewardLines = (rewards: MissionRewardView): string[] => [
  ...(rewards.exp > 0 ? [`經驗 ${formatNumber(rewards.exp)}`] : []),
  ...(rewards.money > 0 ? [`金錢 ${formatNumber(rewards.money)}`] : []),
  ...rewards.items.map((item) => `${item.name} ×${formatNumber(item.quantity)}`),
  ...(rewards.skill === null ? [] : [`技能 ${rewards.skill.name} Lv${rewards.skill.level}`]),
  ...(rewards.pet === null ? [] : [`寵物 ${rewards.pet.name}`]),
]

const clampRatio = (value: number) => Math.min(Math.max(value, 0), 1)

export const progressRatio = (mission: MissionView): number => {
  const required = mission.deliver.reduce((sum, item) => sum + item.quantity, 0)
  const held = mission.deliver.reduce((sum, item) => sum + Math.min(item.owned, item.quantity), 0)
  const kill = mission.kills[0]
  if (kill !== undefined) {
    const { count, progress } = kill
    if (required === 0) return count > 0 ? clampRatio(progress / count) : 1
    // 擊殺與交付合計，兩邊都滿才到 1（同後端的 Ready）
    return clampRatio((Math.min(progress, count) + held) / (count + required))
  }
  if (required === 0) return mission.ready ? 1 : 0
  return clampRatio(held / required)
}

export const actionLabel = (mission: MissionView): "交付" | "完成" =>
  mission.deliver.length > 0 || mission.deliverPet !== null ? "交付" : "完成"

// 同 WOG 的「剩餘時間 N 小時」：不足一小時也算一小時
export const resetHoursText = (nextReset: string, now: Date): string => {
  const hours = Math.max(
    0,
    Math.ceil((new Date(nextReset).getTime() - now.getTime()) / MillisecondsPerHour),
  )
  return `※ 每日任務將在 ${hours} 小時後重置`
}

const reasonTexts: Record<string, string> = {
  missionUnknown: "沒有這個任務",
  alreadyActive: "已經接過此任務",
  alreadyCompleted: "這個任務已經完成",
  tooMany: `同時最多進行 ${MaxActiveMissions} 個任務`,
  levelTooLow: "等級不足",
  jobMismatch: "職業不符",
  sexMismatch: "性別不符",
  birthMismatch: "出生地不符",
  prerequisiteMissing: "需先完成前置任務",
  missionConflict: "已經選擇了另一條路線",
  missionNotActive: "沒有進行中的這個任務",
  objectiveIncomplete: "尚未完成任務",
  itemsMissing: "交付的物品不足",
  bagFull: "背包已滿",
  petMissing: "需要攜帶指定的寵物",
  petsFull: "牧場已滿，最多只能擁有3隻寵物",
}

// 有任何認不得的代碼時改用後端的 detail
export const missionErrorMessage = (reasons: string[], fallback: string): string =>
  reasons.length === 0 || !reasons.every((code) => Object.hasOwn(reasonTexts, code))
    ? fallback
    : reasons.map((code) => reasonTexts[code]).join("、")

export const completionLines = (result: MissionCompletion): string[] => {
  const lines = [
    result.completionMessage,
    `獲得經驗 ${formatNumber(result.exp)}、金錢 ${formatNumber(result.money)}`,
    ...result.items.map((item) => `獲得 ${item.name} ×${formatNumber(item.quantity)}`),
  ]
  if (result.skill !== null)
    lines.push(
      result.skillGranted
        ? `學會了${result.skill.name} Lv${result.skill.level}`
        : `已經會${result.skill.name}，略過技能獎勵`,
    )
  if (result.pet !== null) lines.push(`獲得寵物 ${result.pet.name}`)
  if (result.levelsGained > 0) lines.push(`升了 ${result.levelsGained} 級`)
  return lines.filter((line) => line !== "")
}
