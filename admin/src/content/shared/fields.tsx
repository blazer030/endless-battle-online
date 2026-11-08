import { EditOutlined } from "@ant-design/icons"
import { Button, Form, InputNumber } from "antd"
import type { NamePath } from "antd/es/form/interface"
import { requiredNumber } from "./rules"

export function NumberField({ name, label, min }: { name: NamePath; label: string; min?: number }) {
  return (
    <Form.Item name={name} label={label} rules={[requiredNumber(label)]}>
      <InputNumber precision={0} min={min} style={{ width: "100%" }} />
    </Form.Item>
  )
}

export function RowEditButton({ onClick }: { onClick: () => void }) {
  return <Button size="small" aria-label="編輯" icon={<EditOutlined />} onClick={onClick} />
}
