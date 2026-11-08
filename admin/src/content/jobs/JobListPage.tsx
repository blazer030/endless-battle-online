import { useQuery } from "@tanstack/react-query"
import { Button, Select, Table } from "antd"
import { useState } from "react"
import type { ContentEditorState } from "../shared/ContentFormModal"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import { RowEditButton } from "../shared/fields"
import { matchesSearch } from "../shared/search"
import { JobFormModal } from "./JobFormModal"
import { jobRepository } from "./jobRepository"
import type { JobVo } from "./JobVo"

export function JobListPage() {
  const jobs = useQuery({
    queryKey: jobRepository.queryKey,
    queryFn: jobRepository.list,
    retry: false,
  })
  const [search, setSearch] = useState("")
  const [tierFilter, setTierFilter] = useState<number>()
  const [editor, setEditor] = useState<ContentEditorState<JobVo>>(null)

  const all = jobs.data ?? []
  const tiers = [...new Set(all.map((job) => job.tier))].sort((a, b) => a - b)
  const rows = all.filter(
    (job) => matchesSearch(job, search) && (tierFilter === undefined || job.tier === tierFilter),
  )

  return (
    <ContentListLayout
      title="職業"
      search={search}
      onSearch={setSearch}
      error={jobs.error}
      headerButtons={
        <Button type="primary" onClick={() => setEditor({ mode: "create" })}>
          新增
        </Button>
      }
      filters={
        <Select
          allowClear
          aria-label="階級"
          placeholder="階級"
          style={{ width: 120 }}
          value={tierFilter}
          onChange={setTierFilter}
          options={tiers.map((tier) => ({ value: tier, label: `${tier} 階` }))}
        />
      }
    >
      <Table<JobVo>
        rowKey="id"
        size="small"
        loading={jobs.isLoading}
        dataSource={rows}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          { key: "name", title: "名稱", dataIndex: "name" },
          { key: "tier", title: "階級", dataIndex: "tier" },
          {
            key: "parent",
            title: "上級職業",
            render: (_, job) =>
              all.find((candidate) => candidate.id === job.parentJobId)?.name ??
              job.parentJobId ??
              "",
          },
          { key: "unlockLevel", title: "解鎖等級", dataIndex: "unlockLevel" },
          {
            key: "actions",
            title: "",
            width: 56,
            render: (_, job) => (
              <RowEditButton onClick={() => setEditor({ mode: "edit", record: job })} />
            ),
          },
        ]}
      />
      <JobFormModal state={editor} jobs={all} onClose={() => setEditor(null)} />
    </ContentListLayout>
  )
}
