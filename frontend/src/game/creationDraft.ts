import { CharacterNameMaxLength, nameLength } from "./names"
import {
  AllocationTotalPoints,
  MinimumAllocation,
  baseStatsFromAllocation,
  deriveStats,
} from "./stats"
import type { CreateCharacterCommand, StatAllocation } from "./types"

export const CharacterNameMinLength = 2

export type AllocationStat = keyof StatAllocation

// 建角登記證的六欄；null 表示還沒填。能力要按「就這樣」確定才算填好
export type CreationDraft = {
  readonly name: string | null
  readonly sex: number | null
  readonly birth: number | null
  readonly element: number | null
  readonly jobId: string | null
  readonly allocation: StatAllocation
  readonly statsConfirmed: boolean
}

export const emptyDraft: CreationDraft = {
  name: null,
  sex: null,
  birth: null,
  element: null,
  jobId: null,
  allocation: { str: 1, smart: 1, life: 1, agi: 1 },
  statsConfirmed: false,
}

// 名字去掉前後空白後要 2～12 個字；不合時回 null，草稿不變
export const acceptName = (draft: CreationDraft, input: string): CreationDraft | null => {
  const name = input.trim()
  const length = nameLength(name)
  if (length < CharacterNameMinLength || length > CharacterNameMaxLength) return null
  return { ...draft, name }
}

export const pointsLeft = ({ allocation }: CreationDraft) =>
  AllocationTotalPoints - allocation.str - allocation.smart - allocation.life - allocation.agi

// 每項至少 1 點、總和不超過 10；改了配點就要重新確定
export const adjustStat = (draft: CreationDraft, stat: AllocationStat, delta: 1 | -1) => {
  const value = draft.allocation[stat] + delta
  if (value < MinimumAllocation || (delta > 0 && pointsLeft(draft) <= 0)) return draft
  return { ...draft, allocation: { ...draft.allocation, [stat]: value }, statsConfirmed: false }
}

// 點數要剛好分完才能確定
export const canConfirmStats = (draft: CreationDraft) => pointsLeft(draft) === 0

export const confirmStats = (draft: CreationDraft) =>
  canConfirmStats(draft) ? { ...draft, statsConfirmed: true } : draft

export const isReady = (draft: CreationDraft) =>
  draft.name !== null &&
  draft.sex !== null &&
  draft.birth !== null &&
  draft.element !== null &&
  draft.jobId !== null &&
  draft.statsConfirmed &&
  canConfirmStats(draft)

export const previewStats = ({ allocation }: CreationDraft) => {
  const { hpMax, spMax } = deriveStats(baseStatsFromAllocation(allocation))
  return { hpMax, spMax }
}

export const toCommand = (draft: CreationDraft): CreateCharacterCommand => {
  const { name, sex, birth, element, jobId, allocation } = draft
  if (
    name === null ||
    sex === null ||
    birth === null ||
    element === null ||
    jobId === null ||
    !isReady(draft)
  )
    throw new Error("登記證還沒填完")
  return { name, sex, birth, element, jobId, allocation }
}
