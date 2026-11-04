import { useQuery, useQueryClient } from "@tanstack/react-query"
import { Alert, Button, Popconfirm, Space, Tag } from "antd"
import { useState } from "react"
import { discardDraft, draftQueryKey, getDraft, publishDraft } from "../api/draft"

type Notice = { type: "success" | "error"; text: string }

// 每頁頂端的草稿狀態：是否有草稿、是否通過檢查；只有通過檢查才能發布
export function DraftBar() {
  const queryClient = useQueryClient()
  const { data: draft } = useQuery({ queryKey: draftQueryKey, queryFn: getDraft })
  const [notice, setNotice] = useState<Notice>()
  const [busy, setBusy] = useState(false)

  // 草稿狀態與內容列表都要重新讀
  const refresh = () => queryClient.invalidateQueries()

  const publish = async () => {
    setBusy(true)
    try {
      const refusal = await publishDraft()
      setNotice(
        refusal === null
          ? { type: "success", text: "已發布，重啟 API 後遊戲才會套用" }
          : refusal.reason === "officialChanged"
            ? {
                type: "error",
                text: `建立草稿後正式內容被改過：${refusal.changedFiles.join("、")}`,
              }
            : { type: "error", text: `草稿沒有通過檢查：${refusal.error ?? ""}` },
      )
    } finally {
      setBusy(false)
      await refresh()
    }
  }

  const discard = async () => {
    setBusy(true)
    try {
      await discardDraft()
      setNotice(undefined)
    } finally {
      setBusy(false)
      await refresh()
    }
  }

  if (!draft) {
    return null
  }

  return (
    <Space direction="vertical" style={{ width: "100%", marginBottom: 16 }}>
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
      {draft.exists && !draft.valid && draft.error && <Alert type="error" message={draft.error} />}
      {notice && (
        <Alert
          type={notice.type}
          message={notice.text}
          closable
          onClose={() => setNotice(undefined)}
        />
      )}
    </Space>
  )
}
