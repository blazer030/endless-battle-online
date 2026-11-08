import { useResourceParams } from "@refinedev/core"
import { Layout, theme, Typography } from "antd"
import { DraftActions } from "../draft/DraftBar"

// 固定在頂端：左邊是目前頁面的名稱，右邊是草稿狀態與發布
export function AdminHeader() {
  const { token } = theme.useToken()
  const { resource } = useResourceParams()
  const label = resource?.meta?.label
  return (
    <Layout.Header
      style={{
        position: "sticky",
        top: 0,
        // 要蓋過表格的固定欄（2）與 sticky 表頭（3）
        zIndex: 10,
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: token.marginMD,
        padding: `0 ${token.paddingLG}px`,
        background: token.colorBgContainer,
        borderBottom: `1px solid ${token.colorBorderSecondary}`,
      }}
    >
      <Typography.Title level={5} style={{ margin: 0 }}>
        {typeof label === "string" ? label : ""}
      </Typography.Title>
      <DraftActions />
    </Layout.Header>
  )
}
