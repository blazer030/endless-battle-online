import type { BalanceReport } from "../api/report"

type AdventureRow = BalanceReport["adventures"][number]
type AdventureResult = AdventureRow["withSkills"]

// before 只在和對照的報告不同時才有值（對照報告沒有這一格時是空字串）
export type ComparedCell = { key: string; value: string; before?: string }

export type ComparedRow = { key: string; label: string[]; cells: ComparedCell[] }

const compare = (key: string, value: string, before: string | undefined): ComparedCell =>
  before === undefined || before === value ? { key, value } : { key, value, before }

function wearCells(row: BalanceReport["wearLevels"][number]) {
  return new Map(
    row.cells.map((cell) => [
      String(cell.shopLevel),
      cell.required === null ? "" : cell.level === null ? "—" : String(cell.level),
    ]),
  )
}

export function wearLevelTable(report: BalanceReport, against?: BalanceReport): ComparedRow[] {
  const rowKey = (row: BalanceReport["wearLevels"][number]) => `${row.jobId}/${row.stat}`
  const others = new Map(against?.wearLevels.map((row) => [rowKey(row), wearCells(row)]))
  return report.wearLevels.map((row) => {
    const other = others.get(rowKey(row))
    return {
      key: rowKey(row),
      label: [row.jobName, row.stat],
      cells: [...wearCells(row)].map(([shopLevel, value]) =>
        compare(shopLevel, value, against ? (other?.get(shopLevel) ?? "") : undefined),
      ),
    }
  })
}

export const adventureMetrics = [
  "fights",
  "winRate",
  "losses",
  "timeouts",
  "expPerHour",
  "netMoneyPerHour",
  "levelsGained",
  "nextTierHours",
] as const

function formatMetric(result: AdventureResult, metric: (typeof adventureMetrics)[number]): string {
  switch (metric) {
    case "winRate":
      return `${Math.round(result.winRate * 100)}%`
    case "expPerHour":
    case "netMoneyPerHour":
      return String(Math.round(result[metric]))
    case "nextTierHours":
      return result.nextTierUnaffordable
        ? "∞"
        : result.nextTierHours === null
          ? ""
          : result.nextTierHours.toFixed(1)
    default:
      return String(result[metric])
  }
}

function adventureCells(row: AdventureRow) {
  const cells = new Map<string, string>([
    ["level", String(row.level)],
    ["equipment", row.equipmentShopLevels.join("/")],
  ])
  for (const variant of ["withoutSkills", "withSkills"] as const) {
    for (const metric of adventureMetrics) {
      cells.set(`${variant}.${metric}`, formatMetric(row[variant], metric))
    }
  }
  return cells
}

export function adventureTable(report: BalanceReport, against?: BalanceReport): ComparedRow[] {
  const rowKey = (row: AdventureRow) => `${row.placeId}/${row.jobId}`
  const others = new Map(against?.adventures.map((row) => [rowKey(row), adventureCells(row)]))
  return report.adventures.map((row) => {
    const other = others.get(rowKey(row))
    return {
      key: rowKey(row),
      label: [row.restricted ? `${row.placeName}（禁區）` : row.placeName, row.jobName],
      cells: [...adventureCells(row)].map(([key, value]) =>
        compare(key, value, against ? (other?.get(key) ?? "") : undefined),
      ),
    }
  })
}
