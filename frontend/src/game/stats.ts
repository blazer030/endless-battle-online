import type { JobGrowth, StatAllocation, StatsView } from "./types"

export type BaseStats = Pick<StatsView, "str" | "smart" | "life" | "vit" | "agi" | "au" | "be">
export type DerivedStats = Pick<StatsView, "at" | "mat" | "df" | "mdf" | "hpMax" | "spMax">

// 與後端 CharacterFactory 相同：可分配 10 點，分配的四項另加 10，其餘三項固定 10
export const AllocationTotalPoints = 10
export const MinimumAllocation = 1
const StartingBonus = 10
const UnallocatedStatValue = 10

// 與 PHP round 相同：.5 遠離零進位
const roundLikePhp = (value: number) => Math.sign(value) * Math.round(Math.abs(value))

export const deriveStats = (base: BaseStats): DerivedStats => ({
  at: base.str,
  mat: base.smart,
  df: base.vit,
  mdf: base.be,
  hpMax: roundLikePhp(base.life * 25) + 50,
  spMax: roundLikePhp((base.smart + base.be) * 3) + 25,
})

export const baseStatsFromAllocation = (allocation: StatAllocation): BaseStats => ({
  str: allocation.str + StartingBonus,
  smart: allocation.smart + StartingBonus,
  life: allocation.life + StartingBonus,
  vit: UnallocatedStatValue,
  agi: allocation.agi + StartingBonus,
  au: UnallocatedStatValue,
  be: UnallocatedStatValue,
})

// 依 WOG 的顯示順序
export const baseStatLabels: { key: keyof BaseStats; label: string }[] = [
  { key: "str", label: "力量" },
  { key: "smart", label: "智力" },
  { key: "life", label: "生命" },
  { key: "vit", label: "體質" },
  { key: "agi", label: "敏捷" },
  { key: "au", label: "魅力" },
  { key: "be", label: "信仰" },
]

export const statLabel = (stat: string) =>
  baseStatLabels.find(({ key }) => key === stat)?.label ?? stat

const averageOf = (range: number[]) => range.reduce((sum, value) => sum + value, 0) / range.length

// 每級期望成長最高的能力（並列時全列），範圍相同的併在一起：「力量／體質 2～3」
export const describeTopGrowth = (growth: JobGrowth) => {
  const best = Math.max(...baseStatLabels.map(({ key }) => averageOf(growth[key])))
  const groups: { range: string; labels: string[] }[] = []
  for (const { key, label } of baseStatLabels) {
    if (averageOf(growth[key]) !== best) continue
    const range = growth[key].join("～")
    const group = groups.find((candidate) => candidate.range === range)
    if (group === undefined) groups.push({ range, labels: [label] })
    else group.labels.push(label)
  }
  return groups.map(({ range, labels }) => `${labels.join("／")} ${range}`).join("／")
}
