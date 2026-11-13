// 密藥與旅館房間的加成種類（來自 /api/characters/me/buffs、/api/inn 與道具使用），由 api/lodgingData.ts 轉換
export const buffKinds = [
  "Experience",
  "JobExperience",
  "Attack",
  "Magic",
  "Defense",
  "MagicDefense",
] as const
export type BuffKind = (typeof buffKinds)[number]

// [startsAt, endsAt) 之間開打的場次才有加成；itemId、itemName 是最後喝的道具，舊資料或內容已移除時為 null
type BuffVoParams = {
  kind: BuffKind
  multiplier: number
  startsAt: Date
  endsAt: Date
  itemId: string | null
  itemName: string | null
}

export class BuffVo {
  readonly kind: BuffKind
  readonly multiplier: number
  readonly startsAt: Date
  readonly endsAt: Date
  readonly itemId: string | null
  readonly itemName: string | null

  private constructor(params: BuffVoParams) {
    this.kind = params.kind
    this.multiplier = params.multiplier
    this.startsAt = params.startsAt
    this.endsAt = params.endsAt
    this.itemId = params.itemId
    this.itemName = params.itemName
  }

  static create(params: BuffVoParams): BuffVo {
    return new BuffVo(params)
  }
}

const buffLabels: Record<BuffKind, string> = {
  Experience: "經驗",
  JobExperience: "熟練度",
  Attack: "物攻",
  Magic: "魔攻",
  Defense: "物防",
  MagicDefense: "魔防",
}

export const buffLabel = (kind: BuffKind) => buffLabels[kind]

const MillisecondsPerMinute = 60_000

// 無條件進位：還剩 1 毫秒也算 1 分
export const remainingMinutes = (endsAt: Date, now: Date) =>
  Math.ceil((endsAt.getTime() - now.getTime()) / MillisecondsPerMinute)

// 剛喝下的持續時間四捨五入：裝置時鐘與伺服器差幾秒時，一小時的密藥仍是 60 分
export const durationMinutes = (endsAt: Date, now: Date) =>
  Math.round((endsAt.getTime() - now.getTime()) / MillisecondsPerMinute)

// 開打時刻早於到期時刻才有加成，到期那一刻就不再列出
export const buffLines = (buffs: readonly BuffVo[], now: Date): string[] =>
  buffs
    .filter((buff) => buff.endsAt.getTime() > now.getTime())
    .map(
      (buff) =>
        `${buffLabel(buff.kind)} ×${buff.multiplier}（剩 ${remainingMinutes(buff.endsAt, now)} 分）`,
    )

export const secondsLeft = (endsAt: Date, now: Date) => (endsAt.getTime() - now.getTime()) / 1000

const twoDigits = (value: number) => String(value).padStart(2, "0")

// 城鎮加成列與旅館共用的剩餘時間：滿 1 小時寫 HH:MM（分鐘捨去），不到 1 小時寫 MM:SS（秒捨去）；都補 0
export const remainingText = (endsAt: Date, now: Date) => {
  const seconds = Math.max(0, Math.floor(secondsLeft(endsAt, now)))
  if (seconds >= 3600) {
    const minutes = Math.floor(seconds / 60)
    return `${twoDigits(Math.floor(minutes / 60))}:${twoDigits(minutes % 60)}`
  }
  return `${twoDigits(Math.floor(seconds / 60))}:${twoDigits(seconds % 60)}`
}
