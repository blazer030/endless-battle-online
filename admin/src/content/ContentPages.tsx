import { Create, Edit, EditButton, List } from "@refinedev/antd"
import { useCreate, useList, useOne, useUpdate } from "@refinedev/core"
import { useQueryClient } from "@tanstack/react-query"
import { Select, Space, Table } from "antd"
import { useState } from "react"
import { useNavigate, useParams } from "react-router"
import { draftQueryKey } from "../api/draft"
import { RecordForm } from "./RecordForm"
import type { JsonRecord, JsonValue } from "./recordFields"
import type { ContentResource } from "./resources"

type Props = { resource: ContentResource }

const display = (value: JsonValue | undefined) =>
  value === undefined ? "" : typeof value === "object" ? JSON.stringify(value) : String(value)

export function ContentList({ resource }: Props) {
  const { result, query } = useList<JsonRecord>({
    resource: resource.name,
    pagination: { mode: "off" },
  })

  return (
    <List title={resource.label}>
      <Table<JsonRecord>
        rowKey={(record) => display(record.id)}
        dataSource={result.data}
        loading={query.isLoading}
        pagination={{ pageSize: 50 }}
        size="small"
        columns={[
          ...resource.columns.map((column) => ({
            key: column,
            title: column,
            render: (_: unknown, record: JsonRecord) => display(record[column]),
          })),
          {
            key: "actions",
            title: "",
            render: (_: unknown, record: JsonRecord) => (
              <EditButton
                hideText
                size="small"
                recordItemId={display(record.id)}
                resource={resource.name}
              />
            ),
          },
        ]}
      />
    </List>
  )
}

export function ContentEdit({ resource }: Props) {
  const { id = "" } = useParams()
  const { result } = useOne<JsonRecord>({ resource: resource.name, id })
  const { mutate, mutation } = useUpdate<JsonRecord>()
  const queryClient = useQueryClient()

  return (
    <Edit title={`${resource.label}：${id}`} footerButtons={() => null} headerButtons={() => null}>
      {result && (
        <RecordForm
          key={id}
          base={result}
          lockId
          saving={mutation.isPending}
          onSubmit={(record) =>
            mutate(
              { resource: resource.name, id, values: record },
              { onSettled: () => queryClient.invalidateQueries({ queryKey: draftQueryKey }) },
            )
          }
        />
      )}
    </Edit>
  )
}

export function ContentCreate({ resource }: Props) {
  const navigate = useNavigate()
  const { result } = useList<JsonRecord>({ resource: resource.name, pagination: { mode: "off" } })
  const [templateId, setTemplateId] = useState<string>()
  const records = result.data
  const template = records.find((record) => display(record.id) === templateId) ?? records[0]
  const { mutate, mutation } = useCreate<JsonRecord>()
  const queryClient = useQueryClient()

  return (
    <Create title={`新增${resource.label}`} footerButtons={() => null}>
      <Space direction="vertical" style={{ width: "100%" }}>
        <Select
          showSearch
          style={{ minWidth: 240 }}
          value={template ? display(template.id) : undefined}
          options={records.map((record) => ({
            value: display(record.id),
            label: `${display(record.id)} ${display(record.name)}`,
          }))}
          onChange={setTemplateId}
        />
        {template && (
          <RecordForm
            key={display(template.id)}
            base={template}
            lockId={false}
            saving={mutation.isPending}
            onSubmit={(record) =>
              mutate(
                { resource: resource.name, values: record },
                {
                  onSuccess: () =>
                    navigate(`/${resource.name}/edit/${encodeURIComponent(display(record.id))}`),
                  onSettled: () => queryClient.invalidateQueries({ queryKey: draftQueryKey }),
                },
              )
            }
          />
        )}
      </Space>
    </Create>
  )
}
