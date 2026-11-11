import { formatNumber } from "./format"
import { nameOf, type PublicNames } from "./publicNames"
import type { CharacterVo } from "./character"

// 任務板與任務完成的資料（來自 /api/missions），由 api/missionData.ts 轉換
export type MissionKill = {
  readonly monsterId: string
  readonly monsterName: string
  readonly placeId: number
  readonly placeName: string
  readonly count: number
  readonly progress: number
}

// owned 是背包裡目前持有的數量
export type MissionDeliver = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
  readonly owned: number
}

// 各條件為 null 表示不限
export type MissionRequirements = {
  readonly jobId: string | null
  readonly sex: number | null
  readonly birth: number | null
  readonly prerequisiteId: string | null
  readonly prerequisiteTitle: string | null
}

export type MissionRewardItem = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
}
export type MissionRewardSkill = {
  readonly skillId: string
  readonly name: string
  readonly level: number
}
export type MissionRewardPet = {
  readonly monsterId: string
  readonly name: string
  readonly level: number
}

export type MissionReward = {
  readonly experience: number
  readonly money: number
  readonly items: readonly MissionRewardItem[]
  readonly skill: MissionRewardSkill | null
  readonly pet: MissionRewardPet | null
}

// carried 表示指定的寵物正跟在身邊
export type MissionPet = {
  readonly monsterId: string
  readonly name: string
  readonly carried: boolean
}

export type MissionRequiredSkill = {
  readonly skillId: string
  readonly name: string
  readonly level: number
  readonly met: boolean
}

// expiresAt 是每日任務的期限，一般任務為 null
export type Mission = {
  readonly id: string
  readonly giver: string
  readonly title: string
  readonly description: string
  readonly level: number
  readonly placeId: number | null
  readonly placeName: string | null
  readonly daily: boolean
  readonly kills: readonly MissionKill[]
  readonly deliver: readonly MissionDeliver[]
  readonly requirements: MissionRequirements
  readonly rewards: MissionReward
  readonly ready: boolean
  readonly expiresAt: Date | null
  readonly deliverPet: MissionPet | null
  readonly requiredSkill: MissionRequiredSkill | null
}

type MissionBoardVoParams = {
  available: readonly Mission[]
  active: readonly Mission[]
  completed: readonly Mission[]
  activeLimit: number
  nextDailyReset: Date
}

export class MissionBoardVo {
  readonly available: readonly Mission[]
  readonly active: readonly Mission[]
  readonly completed: readonly Mission[]
  readonly activeLimit: number
  readonly nextDailyReset: Date

  private constructor(params: MissionBoardVoParams) {
    this.available = params.available
    this.active = params.active
    this.completed = params.completed
    this.activeLimit = params.activeLimit
    this.nextDailyReset = params.nextDailyReset
  }

  static create(params: MissionBoardVoParams): MissionBoardVo {
    return new MissionBoardVo(params)
  }
}

// experience、money、items、skill、pet 是這次的報酬；skillGranted 為 false 表示沒有技能獎勵或已會該技能；character 已含獎勵
type MissionCompletionVoParams = {
  board: MissionBoardVo
  character: CharacterVo
  completionMessage: string
  experience: number
  money: number
  items: readonly MissionRewardItem[]
  skill: MissionRewardSkill | null
  skillGranted: boolean
  levelsGained: number
  pet: MissionRewardPet | null
}

export class MissionCompletionVo {
  readonly board: MissionBoardVo
  readonly character: CharacterVo
  readonly completionMessage: string
  readonly experience: number
  readonly money: number
  readonly items: readonly MissionRewardItem[]
  readonly skill: MissionRewardSkill | null
  readonly skillGranted: boolean
  readonly levelsGained: number
  readonly pet: MissionRewardPet | null

  private constructor(params: MissionCompletionVoParams) {
    this.board = params.board
    this.character = params.character
    this.completionMessage = params.completionMessage
    this.experience = params.experience
    this.money = params.money
    this.items = params.items
    this.skill = params.skill
    this.skillGranted = params.skillGranted
    this.levelsGained = params.levelsGained
    this.pet = params.pet
  }

  static create(params: MissionCompletionVoParams): MissionCompletionVo {
    return new MissionCompletionVo(params)
  }
}

export const MaxActiveMissions = 3

const MillisecondsPerHour = 60 * 60 * 1000

// 名稱表沒載到時直接顯示 id
export const requirementLines = (
  mission: Mission,
  names: Pick<PublicNames, "jobs" | "sexes" | "births">,
): string[] => {
  const { jobId, sex, birth, prerequisiteId, prerequisiteTitle } = mission.requirements
  const lines = [`Lv ${mission.level}`]
  if (jobId !== null) lines.push(`限 ${nameOf(names.jobs, jobId)}`)
  if (sex !== null) lines.push(`限 ${nameOf(names.sexes, sex)}性`)
  if (birth !== null) lines.push(`限 ${nameOf(names.births, birth)}出身`)
  if (prerequisiteId !== null) lines.push(`需先完成「${prerequisiteTitle ?? prerequisiteId}」`)
  return lines
}

export const objectiveLines = (mission: Mission): string[] => [
  ...mission.kills.map(
    (kill) => `擊敗 ${kill.monsterName}（${kill.placeName}） ${kill.progress}/${kill.count}`,
  ),
  ...mission.deliver.map((item) => `交付 ${item.name} ${item.owned}/${item.quantity}`),
  ...(mission.deliverPet === null ? [] : [`交付寵物 ${mission.deliverPet.name}（需攜帶）`]),
  ...(mission.requiredSkill === null
    ? []
    : [
        `技能 ${mission.requiredSkill.name} Lv${mission.requiredSkill.level}${mission.requiredSkill.met ? "" : "（等級不足）"}`,
      ]),
]

export const rewardLines = (rewards: MissionReward): string[] => [
  ...(rewards.experience > 0 ? [`經驗 ${formatNumber(rewards.experience)}`] : []),
  ...(rewards.money > 0 ? [`金錢 ${formatNumber(rewards.money)}`] : []),
  ...rewards.items.map((item) => `${item.name} ×${formatNumber(item.quantity)}`),
  ...(rewards.skill === null ? [] : [`技能 ${rewards.skill.name} Lv${rewards.skill.level}`]),
  ...(rewards.pet === null ? [] : [`寵物 ${rewards.pet.name}`]),
]

const clampRatio = (value: number) => Math.min(Math.max(value, 0), 1)

export const progressRatio = (mission: Mission): number => {
  const conditions = [
    ...(mission.deliverPet === null ? [] : [mission.deliverPet.carried]),
    ...(mission.requiredSkill === null ? [] : [mission.requiredSkill.met]),
  ]
  // 擊殺、交付、寵物與技能合計，全部都滿才到 1（同後端的 Ready）
  const total =
    mission.kills.reduce((sum, kill) => sum + kill.count, 0) +
    mission.deliver.reduce((sum, item) => sum + item.quantity, 0) +
    conditions.length
  if (total === 0) return mission.ready ? 1 : 0
  const done =
    mission.kills.reduce((sum, kill) => sum + Math.min(kill.progress, kill.count), 0) +
    mission.deliver.reduce((sum, item) => sum + Math.min(item.owned, item.quantity), 0) +
    conditions.filter(Boolean).length
  return clampRatio(done / total)
}

export const actionLabel = (mission: Mission): "交付" | "完成" =>
  mission.deliver.length > 0 || mission.deliverPet !== null ? "交付" : "完成"

// 同 WOG 的「剩餘時間 N 小時」：不足一小時也算一小時
export const resetHoursText = (nextReset: Date, now: Date): string => {
  const hours = Math.max(0, Math.ceil((nextReset.getTime() - now.getTime()) / MillisecondsPerHour))
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
  skillTooLow: "技能等級不足",
  petsFull: "牧場已滿，最多只能擁有3隻寵物",
}

// 有任何認不得的代碼時改用後端的 detail
export const missionErrorMessage = (reasons: string[], fallback: string): string =>
  reasons.length === 0 || !reasons.every((code) => Object.hasOwn(reasonTexts, code))
    ? fallback
    : reasons.map((code) => reasonTexts[code]).join("、")

export const completionLines = (result: MissionCompletionVo): string[] => {
  const lines = [
    result.completionMessage,
    `獲得經驗 ${formatNumber(result.experience)}、金錢 ${formatNumber(result.money)}`,
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
