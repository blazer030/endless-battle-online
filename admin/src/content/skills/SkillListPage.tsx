import { useQuery } from "@tanstack/react-query"
import { Button, Select, Table } from "antd"
import { useState } from "react"
import { jobRepository } from "../jobs/jobRepository"
import { namedChoices } from "../shared/choices"
import type { ContentEditorState } from "../shared/ContentFormModal"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import { RowEditButton } from "../shared/fields"
import { matchesSearch } from "../shared/search"
import { SkillFormModal } from "./SkillFormModal"
import { skillSlotLabels } from "./skillLabels"
import { skillRepository } from "./skillRepository"
import { skillSlotTypes, type SkillSlotType, type SkillVo } from "./SkillVo"

export function SkillListPage() {
  const skills = useQuery({
    queryKey: skillRepository.queryKey,
    queryFn: skillRepository.list,
    retry: false,
  })
  const jobs = useQuery({
    queryKey: jobRepository.queryKey,
    queryFn: jobRepository.list,
    retry: false,
  })
  const [search, setSearch] = useState("")
  const [jobFilter, setJobFilter] = useState<string>()
  const [slotFilter, setSlotFilter] = useState<SkillSlotType>()
  const [editor, setEditor] = useState<ContentEditorState<SkillVo>>(null)

  const all = skills.data ?? []
  const jobList = jobs.data ?? []
  const rows = all.filter(
    (skill) =>
      matchesSearch(skill, search) &&
      (jobFilter === undefined || skill.jobIds.includes(jobFilter)) &&
      (slotFilter === undefined || skill.slotType === slotFilter),
  )

  return (
    <ContentListLayout
      title="技能"
      search={search}
      onSearch={setSearch}
      error={skills.error ?? jobs.error}
      headerButtons={
        <Button type="primary" onClick={() => setEditor({ mode: "create" })}>
          新增
        </Button>
      }
      filters={
        <>
          <Select
            allowClear
            aria-label="職業"
            placeholder="職業"
            style={{ width: 140 }}
            value={jobFilter}
            onChange={setJobFilter}
            options={namedChoices(jobList)}
          />
          <Select
            allowClear
            aria-label="欄位"
            placeholder="欄位"
            style={{ width: 100 }}
            value={slotFilter}
            onChange={setSlotFilter}
            options={skillSlotTypes.map((value) => ({ value, label: skillSlotLabels[value] }))}
          />
        </>
      }
    >
      <Table<SkillVo>
        rowKey="id"
        size="small"
        loading={skills.isLoading}
        dataSource={rows}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          { key: "name", title: "名稱", dataIndex: "name" },
          { key: "slot", title: "欄位", render: (_, skill) => skillSlotLabels[skill.slotType] },
          {
            key: "jobs",
            title: "職業",
            render: (_, skill) =>
              skill.jobIds.map((id) => jobList.find((job) => job.id === id)?.name ?? id).join("、"),
          },
          { key: "maxLevel", title: "等級上限", dataIndex: "maxLevel" },
          {
            key: "actions",
            title: "",
            width: 56,
            render: (_, skill) => (
              <RowEditButton onClick={() => setEditor({ mode: "edit", record: skill })} />
            ),
          },
        ]}
      />
      <SkillFormModal state={editor} skills={all} onClose={() => setEditor(null)} />
    </ContentListLayout>
  )
}
