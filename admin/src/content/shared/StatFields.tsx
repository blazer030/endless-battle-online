import { Col, Form, InputNumber, Row, Space, Typography } from "antd"
import type { NamePath } from "antd/es/form/interface"
import { requiredNumber } from "./rules"

type NumberRow = { key: string; label: string; name: (string | number)[] }

// 共用的能力值表：每項能力一格。留白代表 0；數字填進去後 placeholder 就看不到了，所以每格都有自己的標籤
export function StatNumberGrid({
  group,
  rows,
  min,
}: {
  group: string
  rows: readonly NumberRow[]
  min?: number
}) {
  return (
    <Form.Item label={group}>
      <Row gutter={[12, 8]}>
        {rows.map(({ key, label, name }) => (
          <Col key={key} span={6}>
            <Typography.Text
              type="secondary"
              style={{ display: "block", fontSize: 12, marginBottom: 2 }}
            >
              {label}
            </Typography.Text>
            <Form.Item name={name} noStyle>
              <InputNumber
                aria-label={`${group} ${label}`}
                precision={0}
                min={min}
                style={{ width: "100%" }}
              />
            </Form.Item>
          </Col>
        ))}
      </Row>
    </Form.Item>
  )
}

type RangeRow = {
  key: string
  label: string
  // 輸入框的無障礙名稱，例如「力量」會變成「力量最小」「力量最大」
  ariaLabel: string
  minimumName: NamePath
  maximumName: NamePath
}

// 共用的能力範圍表：每項能力一列最小～最大；required 為 false 時可以留白
export function StatRangeGrid({
  title,
  rows,
  required,
}: {
  title: string
  rows: readonly RangeRow[]
  required: boolean
}) {
  return (
    <Form.Item label={title}>
      <Row gutter={[16, 8]}>
        {rows.map(({ key, label, ariaLabel, minimumName, maximumName }) => (
          <Col key={key} span={12}>
            <Space align="baseline">
              <Typography.Text style={{ display: "inline-block", width: 40 }}>
                {label}
              </Typography.Text>
              <Form.Item
                name={minimumName}
                noStyle
                rules={required ? [requiredNumber(`${label}最小`)] : []}
              >
                <InputNumber aria-label={`${ariaLabel}最小`} precision={0} min={0} />
              </Form.Item>
              ～
              <Form.Item
                name={maximumName}
                noStyle
                rules={required ? [requiredNumber(`${label}最大`)] : []}
              >
                <InputNumber aria-label={`${ariaLabel}最大`} precision={0} min={0} />
              </Form.Item>
            </Space>
          </Col>
        ))}
      </Row>
    </Form.Item>
  )
}
