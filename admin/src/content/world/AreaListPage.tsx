import { useQuery } from "@tanstack/react-query"
import { Button, Col, Form, Input, Row, Select, Table } from "antd"
import { useState } from "react"
import { filterChoice, namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import { RowEditButton } from "../shared/fields"
import { requiredText } from "../shared/rules"
import { matchesSearch } from "../shared/search"
import { areaRepository, placeRepository } from "./worldRepository"
import { AreaVo } from "./WorldVo"

type AreaFormValues = { id: string; name: string; placeIds: number[] }

const blankAreaValues: AreaFormValues = { id: "", name: "", placeIds: [] }

const areaToValues = (area: AreaVo): AreaFormValues => ({
  id: area.id,
  name: area.name,
  placeIds: [...area.placeIds],
})

const areaFromValues = (values: AreaFormValues): AreaVo =>
  AreaVo.create({ ...values, id: values.id.trim() })

function AreaFields({ mode }: { mode: "edit" | "create" }) {
  const places = useQuery({
    queryKey: placeRepository.queryKey,
    queryFn: placeRepository.list,
    retry: false,
  })
  return (
    <>
      <Row gutter={16}>
        <Col span={12}>
          <Form.Item name="id" label="id" rules={[requiredText("id")]}>
            <Input disabled={mode === "edit"} />
          </Form.Item>
        </Col>
        <Col span={12}>
          <Form.Item name="name" label="名稱" rules={[requiredText("名稱")]}>
            <Input />
          </Form.Item>
        </Col>
      </Row>
      <Form.Item name="placeIds" label="地區">
        <Select
          mode="multiple"
          filterOption={filterChoice}
          options={namedChoices(places.data ?? [])}
        />
      </Form.Item>
    </>
  )
}

export function AreaListPage() {
  const areas = useQuery({
    queryKey: areaRepository.queryKey,
    queryFn: areaRepository.list,
    retry: false,
  })
  const places = useQuery({
    queryKey: placeRepository.queryKey,
    queryFn: placeRepository.list,
    retry: false,
  })
  const [search, setSearch] = useState("")
  const [editor, setEditor] = useState<ContentEditorState<AreaVo>>(null)

  const all = areas.data ?? []
  const placeList = places.data ?? []

  return (
    <ContentListLayout
      title="大陸"
      search={search}
      onSearch={setSearch}
      error={areas.error ?? places.error}
      headerButtons={
        <Button type="primary" onClick={() => setEditor({ mode: "create" })}>
          新增
        </Button>
      }
    >
      <Table<AreaVo>
        rowKey="id"
        size="small"
        loading={areas.isLoading}
        dataSource={all.filter((area) => matchesSearch(area, search))}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          { key: "name", title: "名稱", dataIndex: "name" },
          {
            key: "places",
            title: "地區",
            render: (_, area) =>
              area.placeIds
                .map((id) => placeList.find((place) => place.id === id)?.name ?? id)
                .join("、"),
          },
          {
            key: "actions",
            title: "",
            width: 56,
            render: (_, area) => (
              <RowEditButton onClick={() => setEditor({ mode: "edit", record: area })} />
            ),
          },
        ]}
      />
      <ContentFormModal
        label="大陸"
        state={editor}
        onClose={() => setEditor(null)}
        repository={areaRepository}
        records={all}
        blankValues={blankAreaValues}
        toValues={areaToValues}
        toVo={areaFromValues}
      >
        {(_form, mode) => <AreaFields mode={mode} />}
      </ContentFormModal>
    </ContentListLayout>
  )
}
