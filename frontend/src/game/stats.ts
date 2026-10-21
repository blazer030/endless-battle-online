import type { StatAllocation, StatsView } from "./types"

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
