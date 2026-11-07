import { effectFromRow, effectToRow, type EffectRow } from "./skillEffectRows"
import {
  SkillVo,
  type SkillBasis,
  type SkillConditionVo,
  type SkillCostVo,
  type SkillLevelVo,
  type SkillSlotType,
  type SkillTiming,
} from "./SkillVo"

export type ConditionRow = {
  kind: SkillConditionVo["kind"]
  percent?: number
  stat?: SkillBasis
}

export type CostMode = "amount" | "percent" | "all"

// 表單每列多一個 mode 記住三擇一選了哪個；存檔時只留該種類與方式的欄位
export type CostRow = {
  kind: SkillCostVo["kind"]
  mode: CostMode
  amount?: number
  percent?: number
}

export type LevelRow = {
  learningCost: number
  proficiencyRequired: number
  cooldown: number
  conditions: ConditionRow[]
  costs: CostRow[]
  effects: EffectRow[]
}

export type SkillFormValues = {
  id: string
  name: string
  slotType: SkillSlotType
  timing: SkillTiming
  description: string
  maxLevel: number
  // 空字串代表沒有前置技能
  prerequisiteSkillId: string
  prerequisiteLevel: number | null
  jobIds: string[]
  levels: LevelRow[]
}

const blankLevel: LevelRow = {
  learningCost: 0,
  proficiencyRequired: 0,
  cooldown: 0,
  conditions: [],
  costs: [],
  effects: [],
}

export const blankSkillValues: SkillFormValues = {
  id: "",
  name: "",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "",
  maxLevel: 1,
  prerequisiteSkillId: "",
  prerequisiteLevel: null,
  jobIds: [],
  levels: [blankLevel],
}

export const defaultConditionRow = (kind: ConditionRow["kind"]): ConditionRow => {
  switch (kind) {
    case "Chance":
      return { kind, percent: 100 }
    case "HitPointsBelow":
      return { kind, percent: 50 }
    case "HitPointsBelowOpponent":
      return { kind }
    case "StatAbove":
      return { kind, stat: "Strength" }
  }
}

export const defaultCostRow = (kind: CostRow["kind"]): CostRow => ({
  kind,
  mode: "amount",
  amount: 0,
})

const conditionToRow = (condition: SkillConditionVo): ConditionRow => ({ ...condition })

function conditionFromRow(row: ConditionRow): SkillConditionVo {
  switch (row.kind) {
    case "Chance":
    case "HitPointsBelow":
      return { kind: row.kind, percent: row.percent ?? 0 }
    case "HitPointsBelowOpponent":
      return { kind: row.kind }
    case "StatAbove":
      return { kind: row.kind, stat: row.stat ?? "Strength" }
  }
}

function costToRow(cost: SkillCostVo): CostRow {
  const mode: CostMode =
    "all" in cost && cost.all === true
      ? "all"
      : "percent" in cost && cost.percent !== undefined
        ? "percent"
        : "amount"
  return { ...cost, mode } as CostRow
}

function costFromRow(row: CostRow): SkillCostVo {
  if (row.kind === "Money") {
    return { kind: row.kind, amount: row.amount ?? 0 }
  }
  if (row.mode === "percent") {
    return { kind: row.kind, percent: row.percent ?? 0 }
  }
  if (row.mode === "all" && row.kind === "SkillPoints") {
    return { kind: row.kind, all: true }
  }
  return { kind: row.kind, amount: row.amount ?? 0 }
}

const levelToRow = (level: SkillLevelVo): LevelRow => ({
  learningCost: level.learningCost,
  proficiencyRequired: level.proficiencyRequired,
  cooldown: level.cooldown,
  conditions: level.conditions.map(conditionToRow),
  costs: level.costs.map(costToRow),
  effects: level.effects.map(effectToRow),
})

const levelFromRow = (row: LevelRow, index: number): SkillLevelVo => ({
  level: index + 1,
  learningCost: row.learningCost,
  proficiencyRequired: row.proficiencyRequired,
  cooldown: row.cooldown,
  conditions: row.conditions.map(conditionFromRow),
  costs: row.costs.map(costFromRow),
  effects: row.effects.map(effectFromRow),
})

export const skillToValues = (skill: SkillVo): SkillFormValues => ({
  id: skill.id,
  name: skill.name,
  slotType: skill.slotType,
  timing: skill.timing,
  description: skill.description,
  maxLevel: skill.maxLevel,
  prerequisiteSkillId: skill.prerequisite?.skillId ?? "",
  prerequisiteLevel: skill.prerequisite?.level ?? null,
  jobIds: [...skill.jobIds],
  levels: skill.levels.map(levelToRow),
})

// 等級上限改變時在最後增減等級；新增的一級從前一級複製
export function resizeLevels(levels: readonly LevelRow[], maxLevel: number): LevelRow[] {
  const resized = levels.slice(0, maxLevel)
  while (resized.length < maxLevel) {
    resized.push(structuredClone(resized.at(-1) ?? blankLevel))
  }
  return resized
}

export const skillFromValues = (values: SkillFormValues): SkillVo =>
  SkillVo.create({
    id: values.id.trim(),
    name: values.name,
    slotType: values.slotType,
    timing: values.timing,
    description: values.description,
    maxLevel: values.maxLevel,
    prerequisite:
      values.prerequisiteSkillId === ""
        ? null
        : { skillId: values.prerequisiteSkillId, level: values.prerequisiteLevel ?? 1 },
    jobIds: values.jobIds,
    levels: resizeLevels(values.levels, values.maxLevel).map(levelFromRow),
  })
