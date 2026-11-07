import { List } from "@refinedev/antd"
import { Alert, Input, Space } from "antd"
import type { ReactNode } from "react"

type Props = {
  title: string
  search: string
  onSearch: (search: string) => void
  filters?: ReactNode
  headerButtons?: ReactNode
  error: Error | null
  children: ReactNode
}

// 內容列表共用的版面：搜尋框、篩選；回應有問題時只顯示錯誤，不顯示部分資料
export function ContentListLayout({
  title,
  search,
  onSearch,
  filters,
  headerButtons,
  error,
  children,
}: Props) {
  return (
    <List title={title} headerButtons={() => headerButtons}>
      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          allowClear
          placeholder="搜尋 id 或名稱"
          value={search}
          style={{ width: 240 }}
          onChange={(event) => onSearch(event.target.value)}
        />
        {filters}
      </Space>
      {error ? <Alert type="error" showIcon message={error.message} /> : children}
    </List>
  )
}

export const contentPageSize = 50
