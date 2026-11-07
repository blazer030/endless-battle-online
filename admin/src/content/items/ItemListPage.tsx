import { useQuery } from "@tanstack/react-query"
import { Button, Select, Table } from "antd"
import { useMemo, useState } from "react"
import type { ContentEditorState } from "../shared/ContentFormModal"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import { RowEditButton } from "../shared/fields"
import { matchesSearch } from "../shared/search"
import { ItemFormModal } from "./ItemFormModal"
import { itemCategoryLabels } from "./itemLabels"
import { itemRepository } from "./itemRepository"
import { itemCategories, type ItemCategory, type ItemVo } from "./ItemVo"

export function ItemListPage() {
  const items = useQuery({
    queryKey: itemRepository.queryKey,
    queryFn: itemRepository.list,
    retry: false,
  })
  const [search, setSearch] = useState("")
  const [categoryFilter, setCategoryFilter] = useState<ItemCategory>()
  const [shopLevelFilter, setShopLevelFilter] = useState<number>()
  const [editor, setEditor] = useState<ContentEditorState<ItemVo>>(null)

  const all = useMemo(() => items.data ?? [], [items.data])
  const shopLevels = useMemo(
    () => [...new Set(all.map((item) => item.shopLevel))].sort((a, b) => a - b),
    [all],
  )
  const rows = useMemo(
    () =>
      all.filter(
        (item) =>
          matchesSearch(item, search) &&
          (categoryFilter === undefined || item.category === categoryFilter) &&
          (shopLevelFilter === undefined || item.shopLevel === shopLevelFilter),
      ),
    [all, search, categoryFilter, shopLevelFilter],
  )

  return (
    <ContentListLayout
      title="道具"
      search={search}
      onSearch={setSearch}
      error={items.error}
      headerButtons={
        <Button type="primary" onClick={() => setEditor({ mode: "create" })}>
          新增
        </Button>
      }
      filters={
        <>
          <Select
            allowClear
            virtual={false}
            aria-label="分類"
            placeholder="分類"
            style={{ width: 120 }}
            value={categoryFilter}
            onChange={setCategoryFilter}
            options={itemCategories.map((value) => ({ value, label: itemCategoryLabels[value] }))}
          />
          <Select
            allowClear
            aria-label="商店等級"
            placeholder="商店等級"
            style={{ width: 120 }}
            value={shopLevelFilter}
            onChange={setShopLevelFilter}
            options={shopLevels.map((value) => ({ value, label: String(value) }))}
          />
        </>
      }
    >
      <Table<ItemVo>
        rowKey="id"
        size="small"
        loading={items.isLoading}
        dataSource={rows}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          { key: "name", title: "名稱", dataIndex: "name" },
          {
            key: "category",
            title: "分類",
            render: (_, item) => itemCategoryLabels[item.category],
          },
          { key: "shopLevel", title: "商店等級", dataIndex: "shopLevel" },
          { key: "price", title: "價格", dataIndex: "price" },
          {
            key: "actions",
            title: "",
            width: 56,
            render: (_, item) => (
              <RowEditButton onClick={() => setEditor({ mode: "edit", record: item })} />
            ),
          },
        ]}
      />
      <ItemFormModal state={editor} items={all} onClose={() => setEditor(null)} />
    </ContentListLayout>
  )
}
