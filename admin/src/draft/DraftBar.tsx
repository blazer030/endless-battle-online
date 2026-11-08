import { useQuery, useQueryClient } from "@tanstack/react-query"
import { Alert, App, Button, Popconfirm, Space, Tag } from "antd"
import { useState } from "react"
import { discardDraft, draftQueryKey, getDraft, publishDraft } from "../api/draft"

const useDraft = () => useQuery({ queryKey: draftQueryKey, queryFn: getDraft }).data

// header 裡的草稿狀態：是否有草稿、是否通過檢查；只有通過檢查才能發布
export function DraftActions() {
  const queryClient = useQueryClient()
  const { notification } = App.useApp()
  const draft = useDraft()
  const [busy, setBusy] = useState(false)

  // 草稿狀態與內容列表都要重新讀
  const refresh = () => queryClient.invalidateQueries()

  const publish = async () => {
    setBusy(true)
    try {
      const refusal = await publishDraft()
      if (refusal === null) {
        notification.success({ message: "已發布，重啟 API 後遊戲才會套用" })
      } else {
        notification.error({
          message:
            refusal.reason === "officialChanged"
              ? `建立草稿後正式內容被改過：${refusal.changedFiles.join("、")}`
              : `草稿沒有通過檢查：${refusal.error ?? ""}`,
        })
      }
    } finally {
      setBusy(false)
      await refresh()
    }
  }

  const discard = async () => {
    setBusy(true)
    try {
      await discardDraft()
    } finally {
      setBusy(false)
      await refresh()
    }
  }

  if (!draft) {
    return null
  }

  return (
    <Space wrap>
      {!draft.exists ? (
        <Tag>沒有草稿</Tag>
      ) : draft.valid ? (
        <Tag color="green">草稿通過檢查</Tag>
      ) : (
        <Tag color="red">草稿沒有通過檢查</Tag>
      )}
      {draft.exists && (
        <>
          <Button type="primary" disabled={!draft.valid} loading={busy} onClick={publish}>
            發布
          </Button>
          <Popconfirm title="捨棄草稿？" okText="捨棄" cancelText="取消" onConfirm={discard}>
            <Button danger disabled={busy}>
              捨棄草稿
            </Button>
          </Popconfirm>
        </>
      )}
    </Space>
  )
}

// 內容區最上方：草稿沒有通過檢查時的錯誤訊息
export function DraftError() {
  const draft = useDraft()
  return draft?.exists && !draft.valid && draft.error ? (
    <Alert type="error" message={draft.error} style={{ marginBottom: 16 }} />
  ) : null
}
