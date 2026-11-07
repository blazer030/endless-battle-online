import { useMutation, useQueryClient } from "@tanstack/react-query"
import { Alert, Form, Modal, Select, type FormInstance } from "antd"
import type { ReactNode } from "react"
import { filterChoice, namedChoices } from "./choices"
import type { ContentRepository } from "./repository"
import { saveErrorMessage } from "./saveError"

export type ContentEditorState<TVo> = { mode: "edit"; record: TVo } | { mode: "create" } | null

type ContentVo = { readonly id: string | number; readonly name: string }

type Props<TVo extends ContentVo, TValues extends object> = {
  label: string
  state: ContentEditorState<TVo>
  onClose: () => void
  repository: ContentRepository<TVo>
  records: readonly TVo[]
  blankValues: TValues
  toValues: (vo: TVo) => TValues
  toVo: (values: TValues) => TVo
  children: (form: FormInstance<TValues>, mode: "edit" | "create") => ReactNode
}

// 編輯與新增共用的彈窗：成功才關閉並更新列表與草稿狀態，失敗時留著並顯示錯誤
export function ContentFormModal<TVo extends ContentVo, TValues extends object>(
  props: Props<TVo, TValues>,
) {
  return props.state ? <OpenFormModal {...props} state={props.state} /> : null
}

function OpenFormModal<TVo extends ContentVo, TValues extends object>({
  label,
  state,
  onClose,
  repository,
  records,
  blankValues,
  toValues,
  toVo,
  children,
}: Props<TVo, TValues> & { state: NonNullable<ContentEditorState<TVo>> }) {
  const [form] = Form.useForm<TValues>()
  const queryClient = useQueryClient()
  const save = useMutation({
    mutationFn: (vo: TVo) =>
      state.mode === "edit"
        ? repository.update(String(state.record.id), vo)
        : repository.create(vo),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin"] })
      onClose()
    },
  })

  // 先回到空白表單：能力值是巢狀物件，直接設值會和上一筆合併，留下新紀錄沒有的數值
  const copyFrom = (id: string | number | undefined) => {
    form.resetFields()
    const source = records.find((record) => record.id === id)
    if (source) {
      form.setFieldsValue({
        ...toValues(source),
        id: (blankValues as { id?: unknown }).id,
      } as never)
    }
  }

  return (
    <Modal
      open
      forceRender
      maskClosable={false}
      width={760}
      title={state.mode === "edit" ? `編輯${label}：${state.record.id}` : `新增${label}`}
      okText="儲存"
      cancelText="取消"
      confirmLoading={save.isPending}
      onOk={() => form.submit()}
      onCancel={onClose}
    >
      {save.error && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
          message={saveErrorMessage(save.error)}
        />
      )}
      {state.mode === "create" && (
        <Form.Item label="複製自" htmlFor="content-copy-from" layout="vertical">
          <Select
            id="content-copy-from"
            showSearch
            allowClear
            placeholder="從空白開始"
            filterOption={filterChoice}
            options={namedChoices(records).map((choice) => ({
              ...choice,
              label: `${choice.value} ${records.find((record) => record.id === choice.value)?.name ?? ""}`,
            }))}
            onChange={copyFrom}
          />
        </Form.Item>
      )}
      <Form
        form={form}
        layout="vertical"
        initialValues={state.mode === "edit" ? toValues(state.record) : blankValues}
        onFinish={() => save.mutate(toVo(form.getFieldsValue(true)))}
      >
        {children(form, state.mode)}
      </Form>
    </Modal>
  )
}
