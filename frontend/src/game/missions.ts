import { formatNumber } from "./format"
import type { CharacterVo } from "./character"
import { categoryLabel, type ItemCategory } from "./inventory"

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
  readonly category: ItemCategory
}

// 各條件為 null 表示不限；jobName、sexName、birthName 是條件的顯示名稱
export type MissionRequirements = {
  readonly jobId: string | null
  readonly sex: number | null
  readonly birth: number | null
  readonly prerequisiteId: string | null
  readonly prerequisiteTitle: string | null
  readonly jobName: string | null
  readonly sexName: string | null
  readonly birthName: string | null
}

export type MissionRewardItem = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
  readonly category: ItemCategory
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

// learnedLevel 是目前已學等級，沒學過為 null
export type MissionRequiredSkill = {
  readonly skillId: string
  readonly name: string
  readonly level: number
  readonly met: boolean
  readonly learnedLevel: number | null
}

// expiresAt 是每日任務的期限，一般任務為 null；completionMessage 只有已完成且內容有寫時有值
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
  readonly completionMessage: string | null
}

// firstDailyLevel 是內容裡每日任務的最低等級，沒有每日任務為 null
type MissionBoardVoParams = {
  available: readonly Mission[]
  active: readonly Mission[]
  completed: readonly Mission[]
  activeLimit: number
  nextDailyReset: Date
  firstDailyLevel: number | null
}

export class MissionBoardVo {
  readonly available: readonly Mission[]
  readonly active: readonly Mission[]
  readonly completed: readonly Mission[]
  readonly activeLimit: number
  readonly nextDailyReset: Date
  readonly firstDailyLevel: number | null

  private constructor(params: MissionBoardVoParams) {
    this.available = params.available
    this.active = params.active
    this.completed = params.completed
    this.activeLimit = params.activeLimit
    this.nextDailyReset = params.nextDailyReset
    this.firstDailyLevel = params.firstDailyLevel
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

// 手邊上限以伺服器回傳的 activeLimit 為準；這個只在任務板讀到之前畫空位用
export const MaxActiveMissions = 3

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

// ── 任務板新畫面（設計板 MissionsFit）──

// 名稱開頭的「[每日]」在板上拿掉（紅圖釘已經表示是每日）
export const displayTitle = (mission: Mission) => mission.title.replace(/^\[每日\]/, "")

export const kindLabel = (mission: Mission): "討伐" | "寵物" | "交付" =>
  mission.kills.length > 0 ? "討伐" : mission.deliverPet !== null ? "寵物" : "交付"

// mission-N 的 N 就是內容順序；不是這種 id 的照原本順序
const contentOrder = (id: string) => {
  const match = /^mission-(\d+)$/.exec(id)
  return match === null ? Number.MAX_SAFE_INTEGER : Number(match[1])
}

export type BoardNote = { readonly mission: Mission; readonly done: boolean }

// 每日頁：可接的與今天完成的每日委託，依等級、再依內容順序（完成的留在原位）
export const dailyNotes = (board: MissionBoardVo): BoardNote[] =>
  [
    ...board.available
      .filter((mission) => mission.daily)
      .map((mission) => ({ mission, done: false })),
    ...board.completed
      .filter((mission) => mission.daily)
      .map((mission) => ({ mission, done: true })),
  ]
    .map((note, index) => ({ note, index }))
    .sort(
      (left, right) =>
        left.note.mission.level - right.note.mission.level ||
        contentOrder(left.note.mission.id) - contentOrder(right.note.mission.id) ||
        left.index - right.index,
    )
    .map(({ note }) => note)

export const regularNotes = (board: MissionBoardVo) =>
  board.available.filter((mission) => !mission.daily)

// 完成過的一般委託（API 已是新到舊）
export const completedRegular = (board: MissionBoardVo) =>
  board.completed.filter((mission) => !mission.daily)

export type BoardPage = "daily" | "regular"

// 每日頁有單子（可接或今天完成）就停每日頁，否則停一般頁
export const openingPage = (board: MissionBoardVo): BoardPage =>
  dailyNotes(board).length > 0 ? "daily" : "regular"

// 標在另一頁標題紙上的張數：可接的單子
export const pageCounts = (board: MissionBoardVo): Record<BoardPage, number> => ({
  daily: board.available.filter((mission) => mission.daily).length,
  regular: regularNotes(board).length,
})

const MillisecondsPerHour = 60 * 60 * 1000

// 「N 小時後重置」：無條件進位到小時，最少 1
export const resetHours = (nextReset: Date, now: Date) =>
  Math.max(1, Math.ceil((nextReset.getTime() - now.getTime()) / MillisecondsPerHour))

// 委託單與手邊的一項目標；met＝已達成（綠）、short＝接下後還不夠（紅；擊殺不標紅）
export type ObjectiveRow = {
  readonly text: string
  readonly sub: string | null
  readonly amount: string
  readonly met: boolean
  readonly short: boolean
  readonly category: ItemCategory | null
  readonly itemId: string | null
}

export const objectiveRows = (mission: Mission, accepted: boolean): ObjectiveRow[] => [
  ...mission.kills.map((kill) => ({
    text: `擊敗 ${kill.monsterName}`,
    sub: kill.placeName,
    amount: accepted
      ? `${formatNumber(Math.min(kill.progress, kill.count))} / ${formatNumber(kill.count)}`
      : `×${formatNumber(kill.count)}`,
    met: accepted && kill.progress >= kill.count,
    short: false,
    category: null,
    itemId: null,
  })),
  ...mission.deliver.map((item) => ({
    text: `交付 ${item.name}`,
    sub: null,
    amount: accepted
      ? `${formatNumber(Math.min(item.owned, item.quantity))} / ${formatNumber(item.quantity)}`
      : `×${formatNumber(item.quantity)}`,
    met: accepted && item.owned >= item.quantity,
    short: accepted && item.owned < item.quantity,
    category: item.category,
    itemId: item.itemId,
  })),
  ...(mission.deliverPet === null
    ? []
    : [
        {
          text: `交付寵物 ${mission.deliverPet.name}`,
          sub: accepted ? null : "需攜帶",
          amount: accepted ? (mission.deliverPet.carried ? "攜帶中" : "未攜帶") : "",
          met: accepted && mission.deliverPet.carried,
          short: accepted && !mission.deliverPet.carried,
          category: null,
          itemId: null,
        },
      ]),
  ...(mission.requiredSkill === null
    ? []
    : [
        {
          text: `技能 ${mission.requiredSkill.name} Lv${mission.requiredSkill.level}`,
          sub: null,
          amount:
            mission.requiredSkill.learnedLevel === null
              ? "未學會"
              : `目前 Lv${mission.requiredSkill.learnedLevel}`,
          met: mission.requiredSkill.met,
          short: !mission.requiredSkill.met,
          category: null,
          itemId: null,
        },
      ]),
]

export type HandSlip = {
  readonly title: string
  readonly daily: boolean
  readonly first: ObjectiveRow | null
  readonly more: number
  readonly ratio: number
  readonly stamp: "可交" | "達成" | null
}

// 手邊的單子：第一項目標＋其餘幾項、進度比例；全部達成（含技能）才蓋章
export const handSlip = (mission: Mission): HandSlip => {
  const rows = objectiveRows(mission, true)
  return {
    title: displayTitle(mission),
    daily: mission.daily,
    first: rows[0] ?? null,
    more: Math.max(0, rows.length - 1),
    ratio: progressRatio(mission),
    stamp: mission.ready ? (actionLabel(mission) === "交付" ? "可交" : "達成") : null,
  }
}

// 未接時的條件，以「・」相接
export const requirementText = (mission: Mission) => {
  const { jobName, sexName, birthName, prerequisiteId, prerequisiteTitle } = mission.requirements
  return [
    `Lv ${formatNumber(mission.level)}`,
    ...(jobName === null ? [] : [`限 ${jobName}`]),
    ...(sexName === null ? [] : [`限 ${sexName}性`]),
    ...(birthName === null ? [] : [`限 ${birthName}出身`]),
    ...(prerequisiteId === null ? [] : [`需先完成「${prerequisiteTitle ?? prerequisiteId}」`]),
  ].join("・")
}

// icon 為 coin 時前面放金幣圖；category 有值時放該分類的佔位圖
export type RewardRow = {
  readonly text: string
  readonly icon: "coin" | null
  readonly category: ItemCategory | null
  readonly itemId: string | null
}

export const rewardRows = (rewards: MissionReward): RewardRow[] => [
  { text: `經驗 ${formatNumber(rewards.experience)}`, icon: null, category: null, itemId: null },
  ...(rewards.money > 0
    ? [{ text: formatNumber(rewards.money), icon: "coin" as const, category: null, itemId: null }]
    : []),
  ...rewards.items.map((item) => ({
    text: `${item.name} ×${formatNumber(item.quantity)}`,
    icon: null,
    category: item.category,
    itemId: item.itemId,
  })),
  ...(rewards.skill === null
    ? []
    : [
        {
          text: `技能 ${rewards.skill.name} Lv${rewards.skill.level}`,
          icon: null,
          category: null,
          itemId: null,
        },
      ]),
  ...(rewards.pet === null
    ? []
    : [
        {
          text: `寵物 ${rewards.pet.name} Lv${rewards.pet.level}`,
          icon: null,
          category: null,
          itemId: null,
        },
      ]),
]

// kind：missed＝已經會的技能沒學到、level＝升級（紅字）
export type CompletionRow = RewardRow & { readonly kind: "normal" | "missed" | "level" }

export const completionRows = (result: MissionCompletionVo): CompletionRow[] => [
  {
    text: `獲得經驗 ${formatNumber(result.experience)}${result.money > 0 ? `、金錢 ${formatNumber(result.money)}` : ""}`,
    icon: result.money > 0 ? "coin" : null,
    category: null,
    itemId: null,
    kind: "normal",
  },
  ...result.items.map((item) => ({
    text: `獲得 ${item.name} ×${formatNumber(item.quantity)}`,
    icon: null,
    category: item.category,
    itemId: item.itemId,
    kind: "normal" as const,
  })),
  ...(result.skill === null
    ? []
    : [
        result.skillGranted
          ? {
              text: `學會了 ${result.skill.name} Lv${result.skill.level}`,
              icon: null,
              category: null,
              itemId: null,
              kind: "normal" as const,
            }
          : {
              text: `已經學會${result.skill.name}，這次沒有學到`,
              icon: null,
              category: null,
              itemId: null,
              kind: "missed" as const,
            },
      ]),
  ...(result.pet === null
    ? []
    : [
        {
          text: `獲得寵物 ${result.pet.name} Lv${result.pet.level}`,
          icon: null,
          category: null,
          itemId: null,
          kind: "normal" as const,
        },
      ]),
  ...(result.levelsGained > 0
    ? [
        {
          text: `升到 Lv${result.character.level}`,
          icon: null,
          category: null,
          itemId: null,
          kind: "level" as const,
        },
      ]
    : []),
]

export type LiseEvent =
  | { kind: "accepted"; mission: Mission }
  | { kind: "abandoned" }
  | { kind: "refused"; reasons: readonly string[]; mission: Mission; activeLimit: number }
  | { kind: "failed" }

export type LiseLine = { readonly text: string; readonly failed: boolean }

// 同後端 PetRules.MaxPets
const MaxPetsInStable = 3

const sentFailed: LiseLine = { text: "咦，剛剛沒送出去……再試一次吧？", failed: true }

export const boardLoadFailedLine = "哎呀，委託單一時找不到了……"

// 報酬道具都同一分類時說那一類，否則說背包（後端只回 BAG_FULL，不知道是哪一件放不下）
const fullCategory = (mission: Mission) => {
  const categories = new Set(mission.rewards.items.map((item) => item.category))
  const [only] = categories
  return categories.size === 1 && only !== undefined ? categoryLabel(only) : "背包"
}

const refusalLines: [string, (mission: Mission, activeLimit: number) => string][] = [
  ["ITEMS_MISSING", () => "交付的東西好像還差一點呢……"],
  ["OBJECTIVE_INCOMPLETE", () => "討伐的數量好像還差一點呢……"],
  ["PET_MISSING", (mission) => `咦，您身邊沒有帶著${mission.deliverPet?.name ?? "寵物"}呢……`],
  [
    "SKILL_TOO_LOW",
    (mission) =>
      mission.requiredSkill === null
        ? "技能等級好像還不夠呢……"
        : `${mission.requiredSkill.name}要練到 Lv${mission.requiredSkill.level} 才行喔。`,
  ],
  ["BAG_FULL", (mission) => `哎呀，${fullCategory(mission)}的格子已經滿了……`],
  ["PETS_FULL", () => `獸欄已經住滿 ${MaxPetsInStable} 隻了，新夥伴沒地方住呢……`],
  ["TOO_MANY", (_, activeLimit) => `您手邊已經有 ${activeLimit} 張委託了喔。`],
  ["MISSION_NOT_ACTIVE", () => "這張委託已經不在您手上了呢……"],
]

// 莉瑟在委託單裡說的話；多個原因時取表中最前面的
export const liseLine = (event: LiseEvent): LiseLine => {
  switch (event.kind) {
    case "accepted":
      return { text: `「${displayTitle(event.mission)}」就拜託您了，路上小心！`, failed: false }
    case "abandoned":
      return { text: "好的，這張先放回佈告欄囉。", failed: false }
    case "failed":
      return sentFailed
    case "refused": {
      const line = refusalLines.find(([reason]) => event.reasons.includes(reason))
      return line === undefined
        ? sentFailed
        : { text: line[1](event.mission, event.activeLimit), failed: true }
    }
  }
}

// 展開圓心＝公會前「任務板」紙籤中心（紙籤跟著全景縮放，矮舞台時被全景右緣擋住）
export const missionsRevealOrigin = (stageHeight: number) => {
  const scale = stageHeight / 844
  return {
    x: Math.round(Math.max(195 + 104 * scale, 390 - 113 * scale)),
    y: Math.round(540 * scale + 19),
  }
}

// 佈告欄高＝手邊上緣 − 94；單子排高 min(112, (板高 − 140) / 3)；手邊高與離底隨舞台高伸縮
export const missionsLayout = (stageHeight: number) => {
  const extra = stageHeight - 624
  const handHeight = Math.round(128 + extra * 0.256)
  const handMargin = Math.round(8 + extra * 0.051)
  const handTop = stageHeight - handMargin - handHeight
  const boardHeight = handTop - 94
  return {
    boardHeight,
    rowHeight: Math.round(Math.min(112, (boardHeight - 140) / 3)),
    handTop,
    handHeight,
    handMargin,
  }
}
