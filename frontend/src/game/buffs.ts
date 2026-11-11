import type { BuffKind, BuffView } from "./types"

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
export const remainingMinutes = (endsAt: string, now: Date) =>
  Math.ceil((Date.parse(endsAt) - now.getTime()) / MillisecondsPerMinute)

// 剛喝下的持續時間四捨五入：裝置時鐘與伺服器差幾秒時，一小時的密藥仍是 60 分
export const durationMinutes = (endsAt: string, now: Date) =>
  Math.round((Date.parse(endsAt) - now.getTime()) / MillisecondsPerMinute)

// 開打時刻早於到期時刻才有加成，到期那一刻就不再列出
export const buffLines = (buffs: BuffView[], now: Date): string[] =>
  buffs
    .filter((buff) => Date.parse(buff.endsAt) > now.getTime())
    .map(
      (buff) =>
        `${buffLabel(buff.kind)} ×${buff.multiplier}（剩 ${remainingMinutes(buff.endsAt, now)} 分）`,
    )
