import { useQuery } from "@tanstack/react-query"
import { Alert, Segmented, Space, Table, Tabs, Typography } from "antd"
import type { ColumnsType } from "antd/es/table"
import { useState } from "react"
import { fetchReport, type BalanceReport, type BalanceSkillLevel } from "../api/report"
import { adventureMetrics, adventureTable, wearLevelTable, type ComparedRow } from "./compare"

const metricLabels: Record<(typeof adventureMetrics)[number], string> = {
  fights: "場數",
  winRate: "勝率",
  losses: "敗",
  timeouts: "超時",
  expPerHour: "經驗／時",
  netMoneyPerHour: "淨收入／時",
  levelsGained: "升級",
  nextTierHours: "下一階（時）",
}

const statLabels: Record<string, string> = { str: "力量", smart: "智力", agi: "敏捷", au: "魅力" }

function Cell({ row, cellKey }: { row: ComparedRow; cellKey: string }) {
  const cell = row.cells.find((candidate) => candidate.key === cellKey)
  if (!cell) return null
  return cell.before === undefined ? (
    cell.value
  ) : (
    <Typography.Text type="warning" strong data-changed>
      {`${cell.before} → ${cell.value}`}
    </Typography.Text>
  )
}

function wearColumns(report: BalanceReport): ColumnsType<ComparedRow> {
  return [
    { key: "job", title: "職業", render: (_, row) => row.label[0] },
    { key: "stat", title: "能力", render: (_, row) => statLabels[row.label[1]] ?? row.label[1] },
    ...report.shopLevels.map((shopLevel) => ({
      key: `L${shopLevel}`,
      title: `L${shopLevel}`,
      render: (_: unknown, row: ComparedRow) => <Cell row={row} cellKey={String(shopLevel)} />,
    })),
  ]
}

const variantColumns = (variant: "withoutSkills" | "withSkills", title: string) => ({
  key: variant,
  title,
  children: adventureMetrics.map((metric) => ({
    key: `${variant}.${metric}`,
    title: metricLabels[metric],
    render: (_: unknown, row: ComparedRow) => <Cell row={row} cellKey={`${variant}.${metric}`} />,
  })),
})

const adventureColumns: ColumnsType<ComparedRow> = [
  { key: "place", title: "地區", fixed: "left", render: (_, row) => row.label[0] },
  { key: "job", title: "職業", render: (_, row) => row.label[1] },
  { key: "level", title: "等級", render: (_, row) => <Cell row={row} cellKey="level" /> },
  { key: "equipment", title: "裝備階", render: (_, row) => <Cell row={row} cellKey="equipment" /> },
  variantColumns("withoutSkills", "不帶技能"),
  variantColumns("withSkills", "帶技能"),
]

// 草稿與正式內容的報告：有草稿時顯示草稿的數字，和正式內容不同的格子標成「正式 → 草稿」
export function ReportPage() {
  const [skillLevel, setSkillLevel] = useState<BalanceSkillLevel>("Max")
  const official = useQuery({
    queryKey: ["admin", "report", "Official", skillLevel],
    queryFn: () => fetchReport("Official", skillLevel),
  })
  const draft = useQuery({
    queryKey: ["admin", "report", "Draft", skillLevel],
    queryFn: () => fetchReport("Draft", skillLevel),
  })

  const officialReport =
    official.data && "report" in official.data ? official.data.report : undefined
  const draftReport = draft.data && "report" in draft.data ? draft.data.report : undefined
  const shown = draftReport ?? officialReport
  const against = draftReport ? officialReport : undefined

  return (
    <Space direction="vertical" style={{ width: "100%" }}>
      <Typography.Title level={4}>平衡報告</Typography.Title>
      <Segmented<BalanceSkillLevel>
        value={skillLevel}
        onChange={setSkillLevel}
        options={[
          { value: "Max", label: "技能最高級" },
          { value: "First", label: "技能 Lv1" },
        ]}
      />
      {draft.data && "missing" in draft.data && (
        <Alert type="info" message="沒有草稿，顯示正式內容的報告" />
      )}
      {draft.data && "error" in draft.data && <Alert type="error" message={draft.data.error} />}
      {shown && (
        <Tabs
          items={[
            {
              key: "wear",
              label: "穿戴等級",
              children: (
                <Table<ComparedRow>
                  rowKey="key"
                  size="small"
                  pagination={false}
                  dataSource={wearLevelTable(shown, against)}
                  columns={wearColumns(shown)}
                />
              ),
            },
            {
              key: "adventure",
              label: "冒險效率",
              children: (
                <Table<ComparedRow>
                  rowKey="key"
                  size="small"
                  pagination={false}
                  scroll={{ x: "max-content" }}
                  dataSource={adventureTable(shown, against)}
                  columns={adventureColumns}
                />
              ),
            },
          ]}
        />
      )}
    </Space>
  )
}
