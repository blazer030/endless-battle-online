import type { BaseStats, DerivedStats, StatAllocation } from "./character"
import type { JobGrowth } from "./types"

export type { BaseStats, DerivedStats } from "./character"

// 與後端 CharacterFactory 相同：可分配 10 點，分配的四項另加 10，其餘三項固定 10
export const AllocationTotalPoints = 10
export const MinimumAllocation = 1
const StartingBonus = 10
const UnallocatedStatValue = 10

// 與 PHP round 相同：.5 遠離零進位
const roundLikePhp = (value: number) => Math.sign(value) * Math.round(Math.abs(value))

export const deriveStats = (base: BaseStats): DerivedStats => ({
  attack: base.strength,
  magicAttack: base.intelligence,
  defense: base.vitality,
  magicDefense: base.faith,
  maxHealthPoints: roundLikePhp(base.life * 25) + 50,
  maxManaPoints: roundLikePhp((base.intelligence + base.faith) * 3) + 25,
})

export const baseStatsFromAllocation = (allocation: StatAllocation): BaseStats => ({
  strength: allocation.strength + StartingBonus,
  intelligence: allocation.intelligence + StartingBonus,
  life: allocation.life + StartingBonus,
  vitality: UnallocatedStatValue,
  agility: allocation.agility + StartingBonus,
  charisma: UnallocatedStatValue,
  faith: UnallocatedStatValue,
})

// 依 WOG 的顯示順序
export const baseStatLabels: { key: keyof BaseStats; label: string }[] = [
  { key: "strength", label: "力量" },
  { key: "intelligence", label: "智力" },
  { key: "life", label: "生命" },
  { key: "vitality", label: "體質" },
  { key: "agility", label: "敏捷" },
  { key: "charisma", label: "魅力" },
  { key: "faith", label: "信仰" },
]

// /api/content 的職業成長還是舊代碼
export const legacyStatCodes = {
  str: "strength",
  smart: "intelligence",
  life: "life",
  vit: "vitality",
  agi: "agility",
  au: "charisma",
  be: "faith",
} as const satisfies Record<string, keyof BaseStats>

export type LegacyStatCode = keyof typeof legacyStatCodes

export const statLabel = (stat: string) =>
  baseStatLabels.find((row) => row.key === stat)?.label ?? stat

const averageOf = (range: number[]) => range.reduce((sum, value) => sum + value, 0) / range.length

// 每級期望成長最高的能力（並列時全列），範圍相同的併在一起：「力量／體質 2～3」
// 成長範圍來自 /api/content 的職業，還是舊代碼
const growthRows = (Object.keys(legacyStatCodes) as LegacyStatCode[]).map((code) => ({
  code,
  label: statLabel(legacyStatCodes[code]),
}))

export const describeTopGrowth = (growth: JobGrowth) => {
  const best = Math.max(...growthRows.map(({ code }) => averageOf(growth[code])))
  const groups: { range: string; labels: string[] }[] = []
  for (const { code, label } of growthRows) {
    if (averageOf(growth[code]) !== best) continue
    const range = growth[code].join("～")
    const group = groups.find((candidate) => candidate.range === range)
    if (group === undefined) groups.push({ range, labels: [label] })
    else group.labels.push(label)
  }
  return groups.map(({ range, labels }) => `${labels.join("／")} ${range}`).join("／")
}
