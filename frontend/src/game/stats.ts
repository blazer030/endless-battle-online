import type { BaseStats, DerivedStats, StatAllocation } from "./character"

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

export const statLabel = (stat: string) =>
  baseStatLabels.find((row) => row.key === stat)?.label ?? stat
