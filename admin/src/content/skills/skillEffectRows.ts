import type { ContentElement } from "../shared/elements"
import type {
  SkillAttackKind,
  SkillBasis,
  SkillEffectVo,
  SkillResource,
  SkillScalingVo,
  SkillStat,
  SkillTarget,
} from "./SkillVo"

export type EffectKind = SkillEffectVo["kind"]

export type EffectMode = "percent" | "amount" | "scaling" | "percentOfDamage" | "percentOfMaximum"

// 表單的一列效果：所有欄位都可能有值，mode 記住三擇一選了哪個；存檔時由 effectFromRow 只留該種類的欄位
export type EffectRow = {
  kind: EffectKind
  rounds: number
  mode?: EffectMode
  target?: SkillTarget
  stat?: SkillStat
  percent?: number
  amount?: number
  scaling?: SkillScalingVo[]
  percentOfDamage?: number
  percentOfMaximum?: number
  chance?: number | null
  attackKind?: SkillAttackKind
  hits?: number
  resource?: SkillResource
  hasChanceByStat?: boolean
  chanceByStat?: { stat: SkillBasis; percent: number }
  element?: ContentElement
}

const defaultScaling = (): SkillScalingVo[] => [{ basis: "Strength", ratio: 1 }]

export function defaultEffectRow(kind: EffectKind, rounds = 0): EffectRow {
  switch (kind) {
    case "StatChange":
      return { kind, rounds, target: "Caster", stat: "Attack", mode: "percent", percent: 0 }
    case "BonusDamage":
      return { kind, rounds, attackKind: "Physical", scaling: defaultScaling() }
    case "MultiHit":
      return { kind, rounds, attackKind: "Physical", hits: 2 }
    case "DamageReduction":
      return { kind, rounds, attackKind: "Physical", percent: 0 }
    case "Restore":
      return { kind, rounds, resource: "HitPoints", mode: "scaling", scaling: defaultScaling() }
    case "Stun":
    case "Silence":
      return { kind, rounds, chance: 100 }
    case "PercentDamage":
    case "ElementAdvantage":
      return { kind, rounds, percent: 0 }
    case "DamageOverTime":
      return { kind, rounds, scaling: defaultScaling() }
    case "ReduceSkillPoints":
      return { kind, rounds, mode: "amount", amount: 0 }
    case "AttackElement":
    case "DefenseElement":
      return { kind, rounds, element: "Earth" }
  }
}

function modeOf(effect: SkillEffectVo): EffectMode | undefined {
  switch (effect.kind) {
    case "StatChange":
      return effect.scaling ? "scaling" : effect.amount !== undefined ? "amount" : "percent"
    case "Restore":
      return effect.scaling
        ? "scaling"
        : effect.percentOfDamage !== undefined
          ? "percentOfDamage"
          : "percentOfMaximum"
    case "ReduceSkillPoints":
      return effect.percentOfMaximum !== undefined ? "percentOfMaximum" : "amount"
    default:
      return undefined
  }
}

export const effectToRow = (effect: SkillEffectVo): EffectRow => ({
  ...(structuredClone(effect) as EffectRow),
  mode: modeOf(effect),
  hasChanceByStat: effect.kind === "Stun" && effect.chanceByStat !== undefined,
})

const scalingOf = (row: EffectRow) => (row.scaling ?? []).map((term) => ({ ...term }))

// 清空的選填機率不送出
const optionalChance = (row: EffectRow) =>
  row.chance === undefined || row.chance === null ? {} : { chance: row.chance }

export function effectFromRow(row: EffectRow): SkillEffectVo {
  const { kind, rounds } = row
  switch (kind) {
    case "StatChange":
      return {
        kind,
        target: row.target ?? "Caster",
        stat: row.stat ?? "Attack",
        ...(row.mode === "scaling"
          ? { scaling: scalingOf(row) }
          : row.mode === "amount"
            ? { amount: row.amount ?? 0 }
            : { percent: row.percent ?? 0 }),
        ...optionalChance(row),
        rounds,
      }
    case "BonusDamage":
      return {
        kind,
        attackKind: row.attackKind ?? "Physical",
        scaling: scalingOf(row),
        ...optionalChance(row),
        rounds,
      }
    case "MultiHit":
      return { kind, attackKind: row.attackKind ?? "Physical", hits: row.hits ?? 0, rounds }
    case "DamageReduction":
      return { kind, attackKind: row.attackKind ?? "Physical", percent: row.percent ?? 0, rounds }
    case "Restore":
      return {
        kind,
        resource: row.resource ?? "HitPoints",
        ...(row.mode === "percentOfDamage"
          ? { percentOfDamage: row.percentOfDamage ?? 0 }
          : row.mode === "percentOfMaximum"
            ? { percentOfMaximum: row.percentOfMaximum ?? 0 }
            : { scaling: scalingOf(row) }),
        rounds,
      }
    case "Stun":
      return {
        kind,
        chance: row.chance ?? 0,
        ...(row.hasChanceByStat
          ? { chanceByStat: row.chanceByStat ?? { stat: "Strength", percent: 0 } }
          : {}),
        rounds,
      }
    case "Silence":
      return { kind, chance: row.chance ?? 0, rounds }
    case "PercentDamage":
    case "ElementAdvantage":
      return { kind, percent: row.percent ?? 0, rounds }
    case "DamageOverTime":
      return { kind, scaling: scalingOf(row), rounds }
    case "ReduceSkillPoints":
      return {
        kind,
        ...(row.mode === "percentOfMaximum"
          ? { percentOfMaximum: row.percentOfMaximum ?? 0 }
          : { amount: row.amount ?? 0 }),
        rounds,
      }
    case "AttackElement":
    case "DefenseElement":
      return { kind, element: row.element ?? "Earth", rounds }
  }
}
