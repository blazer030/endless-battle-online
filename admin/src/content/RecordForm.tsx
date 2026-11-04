import { Button, Form, Input, InputNumber, Switch } from "antd"
import { useMemo } from "react"
import { fromFields, toFields, type FieldValues, type JsonRecord } from "./recordFields"

type Props = {
  base: JsonRecord
  lockId: boolean
  saving: boolean
  onSubmit: (record: JsonRecord) => void
}

// 依紀錄現有欄位產生表單；送出時以 base 為底，沒有改的欄位原樣送回
export function RecordForm({ base, lockId, saving, onSubmit }: Props) {
  const [form] = Form.useForm<FieldValues>()
  const fields = useMemo(() => toFields(base), [base])
  const initialValues = useMemo(
    () => Object.fromEntries(fields.map((field) => [field.name, field.value])),
    [fields],
  )

  const finish = (values: FieldValues) => {
    const result = fromFields(base, values)
    if ("errors" in result) {
      form.setFields(
        Object.entries(result.errors).map(([name, error]) => ({ name, errors: [error] })),
      )
      return
    }
    onSubmit(result.record)
  }

  return (
    <Form form={form} layout="vertical" initialValues={initialValues} onFinish={finish}>
      {fields.map((field) => (
        <Form.Item
          key={field.name}
          name={field.name}
          label={field.name}
          valuePropName={field.kind === "boolean" ? "checked" : "value"}
        >
          {field.kind === "number" ? (
            <InputNumber />
          ) : field.kind === "boolean" ? (
            <Switch />
          ) : field.kind === "json" ? (
            <Input.TextArea autoSize spellCheck={false} />
          ) : (
            <Input disabled={lockId && field.name === "id"} />
          )}
        </Form.Item>
      ))}
      <Button type="primary" htmlType="submit" loading={saving}>
        儲存
      </Button>
    </Form>
  )
}
