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

// endsAt 之前開打的場次才有加成
type BuffVoParams = { kind: BuffKind; multiplier: number; endsAt: Date }

export class BuffVo {
  readonly kind: BuffKind
  readonly multiplier: number
  readonly endsAt: Date

  private constructor(params: BuffVoParams) {
    this.kind = params.kind
    this.multiplier = params.multiplier
    this.endsAt = params.endsAt
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
