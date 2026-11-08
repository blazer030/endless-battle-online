import { DeleteOutlined } from "@ant-design/icons"
import { Button, Col, Form, InputNumber, Row, Select } from "antd"
import { requiredNumber } from "../shared/rules"
import {
  defaultConditionRow,
  defaultCostRow,
  type ConditionRow,
  type CostMode,
  type CostRow,
} from "./skillFormValues"
import { skillBasisChoices } from "./skillLabels"

const conditionKindChoices: { value: ConditionRow["kind"]; label: string }[] = [
  { value: "Chance", label: "機率" },
  { value: "HitPointsBelow", label: "HP 低於" },
  { value: "HitPointsBelowOpponent", label: "HP 比對手低" },
  { value: "StatAbove", label: "能力比對手高" },
]

const costKindChoices: { value: CostRow["kind"]; label: string }[] = [
  { value: "HitPoints", label: "HP" },
  { value: "SkillPoints", label: "SP" },
  { value: "Money", label: "金錢" },
]

const costModeLabels: Record<CostMode, string> = {
  amount: "固定值",
  percent: "百分比",
  all: "全部",
}

type RowProps = { levelIndex: number; rowName: number; index: number; onRemove: () => void }

function ConditionRowFields({ levelIndex, rowName, index, onRemove }: RowProps) {
  const form = Form.useFormInstance()
  const path = ["levels", levelIndex, "conditions", rowName]
  const kind = Form.useWatch([...path, "kind"], form) as ConditionRow["kind"] | undefined
  const label = `條件 ${index + 1}`
  return (
    <Row gutter={12} align="bottom">
      <Col span={8}>
        <Form.Item name={[rowName, "kind"]} label="種類">
          <Select
            aria-label={`${label} 種類`}
            options={conditionKindChoices}
            onChange={(next: ConditionRow["kind"]) =>
              form.setFieldValue(path, defaultConditionRow(next))
            }
          />
        </Form.Item>
      </Col>
      {(kind === "Chance" || kind === "HitPointsBelow") && (
        <Col span={6}>
          <Form.Item name={[rowName, "percent"]} label="百分比" rules={[requiredNumber("百分比")]}>
            <InputNumber
              aria-label={`${label} 百分比`}
              precision={0}
              min={0}
              style={{ width: "100%" }}
            />
          </Form.Item>
        </Col>
      )}
      {kind === "StatAbove" && (
        <Col span={8}>
          <Form.Item name={[rowName, "stat"]} label="能力">
            <Select aria-label={`${label} 能力`} options={skillBasisChoices} />
          </Form.Item>
        </Col>
      )}
      <Col flex="none">
        <Form.Item>
          <Button
            aria-label={`刪除條件 ${index + 1}`}
            icon={<DeleteOutlined />}
            onClick={onRemove}
          />
        </Form.Item>
      </Col>
    </Row>
  )
}

export function ConditionList({ levelIndex }: { levelIndex: number }) {
  return (
    <Form.List name={["levels", levelIndex, "conditions"]}>
      {(rows, { add, remove }) => (
        <Form.Item label="發動條件（沒有條件代表常駐）">
          {rows.map((row, index) => (
            <ConditionRowFields
              key={row.key}
              levelIndex={levelIndex}
              rowName={row.name}
              index={index}
              onRemove={() => remove(row.name)}
            />
          ))}
          <Button onClick={() => add(defaultConditionRow("Chance"))}>新增條件</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}

function CostRowFields({ levelIndex, rowName, index, onRemove }: RowProps) {
  const form = Form.useFormInstance()
  const path = ["levels", levelIndex, "costs", rowName]
  const kind = Form.useWatch([...path, "kind"], form) as CostRow["kind"] | undefined
  const mode = Form.useWatch([...path, "mode"], form) as CostMode | undefined
  const label = `消耗 ${index + 1}`
  const modes: CostMode[] =
    kind === "SkillPoints" ? ["amount", "percent", "all"] : ["amount", "percent"]
  return (
    <Row gutter={12} align="bottom">
      <Col span={6}>
        <Form.Item name={[rowName, "kind"]} label="種類">
          <Select
            aria-label={`${label} 種類`}
            options={costKindChoices}
            onChange={(next: CostRow["kind"]) => form.setFieldValue(path, defaultCostRow(next))}
          />
        </Form.Item>
      </Col>
      {kind !== "Money" && (
        <Col span={6}>
          <Form.Item name={[rowName, "mode"]} label="方式">
            <Select
              aria-label={`${label} 方式`}
              options={modes.map((value) => ({ value, label: costModeLabels[value] }))}
            />
          </Form.Item>
        </Col>
      )}
      {(kind === "Money" || mode === "amount") && (
        <Col span={6}>
          <Form.Item name={[rowName, "amount"]} label="固定值" rules={[requiredNumber("固定值")]}>
            <InputNumber
              aria-label={`${label} 固定值`}
              precision={0}
              min={0}
              style={{ width: "100%" }}
            />
          </Form.Item>
        </Col>
      )}
      {kind !== "Money" && mode === "percent" && (
        <Col span={6}>
          <Form.Item name={[rowName, "percent"]} label="百分比" rules={[requiredNumber("百分比")]}>
            <InputNumber aria-label={`${label} 百分比`} min={0} style={{ width: "100%" }} />
          </Form.Item>
        </Col>
      )}
      <Col flex="none">
        <Form.Item>
          <Button
            aria-label={`刪除消耗 ${index + 1}`}
            icon={<DeleteOutlined />}
            onClick={onRemove}
          />
        </Form.Item>
      </Col>
    </Row>
  )
}

export function CostList({ levelIndex }: { levelIndex: number }) {
  return (
    <Form.List name={["levels", levelIndex, "costs"]}>
      {(rows, { add, remove }) => (
        <Form.Item label="消耗">
          {rows.map((row, index) => (
            <CostRowFields
              key={row.key}
              levelIndex={levelIndex}
              rowName={row.name}
              index={index}
              onRemove={() => remove(row.name)}
            />
          ))}
          <Button onClick={() => add(defaultCostRow("SkillPoints"))}>新增消耗</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}
