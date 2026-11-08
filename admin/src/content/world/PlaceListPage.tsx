import { DeleteOutlined } from "@ant-design/icons"
import { useQuery } from "@tanstack/react-query"
import {
  Button,
  Checkbox,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Table,
  Tag,
} from "antd"
import { useState } from "react"
import { namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import { NumberField, RowEditButton } from "../shared/fields"
import { requiredNumber, requiredText } from "../shared/rules"
import { matchesSearch } from "../shared/search"
import { areaRepository, placeRepository } from "./worldRepository"
import { PlaceVo } from "./WorldVo"

type PlaceFormValues = {
  id: number | null
  name: string
  subPlaceNames: string[]
  recommendedMinimum: number
  recommendedMaximum: number
  restricted: boolean
}

const blankPlaceValues: PlaceFormValues = {
  id: null,
  name: "",
  subPlaceNames: [],
  recommendedMinimum: 1,
  recommendedMaximum: 1,
  restricted: false,
}

const placeToValues = (place: PlaceVo): PlaceFormValues => ({
  id: place.id,
  name: place.name,
  subPlaceNames: [...place.subPlaceNames],
  recommendedMinimum: place.recommendedMinimum,
  recommendedMaximum: place.recommendedMaximum,
  restricted: place.restricted,
})

const placeFromValues = (values: PlaceFormValues): PlaceVo =>
  PlaceVo.create({ ...values, id: values.id ?? 0 })

function PlaceFields({ mode }: { mode: "edit" | "create" }) {
  return (
    <>
      <Row gutter={16}>
        <Col span={6}>
          <Form.Item name="id" label="id" rules={[requiredNumber("id")]}>
            <InputNumber
              precision={0}
              min={1}
              disabled={mode === "edit"}
              style={{ width: "100%" }}
            />
          </Form.Item>
        </Col>
        <Col span={12}>
          <Form.Item name="name" label="名稱" rules={[requiredText("名稱")]}>
            <Input />
          </Form.Item>
        </Col>
        <Col span={6}>
          <Form.Item name="restricted" valuePropName="checked" style={{ marginTop: 30 }}>
            <Checkbox>禁區</Checkbox>
          </Form.Item>
        </Col>
        <Col span={6}>
          <NumberField name="recommendedMinimum" label="推薦等級最小" min={0} />
        </Col>
        <Col span={6}>
          <NumberField name="recommendedMaximum" label="推薦等級最大" min={0} />
        </Col>
      </Row>
      <Form.List name="subPlaceNames">
        {(rows, { add, remove }) => (
          <Form.Item label="子地區">
            {rows.map((row, index) => (
              <Space key={row.key} align="start" style={{ display: "flex", marginBottom: 8 }}>
                <Form.Item name={row.name} noStyle rules={[requiredText("子地區名稱")]}>
                  <Input aria-label={`子地區 ${index + 1}`} style={{ width: 280 }} />
                </Form.Item>
                <Button
                  aria-label={`刪除子地區 ${index + 1}`}
                  icon={<DeleteOutlined />}
                  onClick={() => remove(row.name)}
                />
              </Space>
            ))}
            <Button onClick={() => add("")}>新增子地區</Button>
          </Form.Item>
        )}
      </Form.List>
    </>
  )
}

export function PlaceListPage() {
  const places = useQuery({
    queryKey: placeRepository.queryKey,
    queryFn: placeRepository.list,
    retry: false,
  })
  const areas = useQuery({
    queryKey: areaRepository.queryKey,
    queryFn: areaRepository.list,
    retry: false,
  })
  const [search, setSearch] = useState("")
  const [areaFilter, setAreaFilter] = useState<string>()
  const [restrictedFilter, setRestrictedFilter] = useState<boolean>()
  const [editor, setEditor] = useState<ContentEditorState<PlaceVo>>(null)

  const all = places.data ?? []
  const areaList = areas.data ?? []
  const areaOf = (place: PlaceVo) => areaList.find((area) => area.placeIds.includes(place.id))
  const rows = all.filter(
    (place) =>
      matchesSearch(place, search) &&
      (areaFilter === undefined || areaOf(place)?.id === areaFilter) &&
      (restrictedFilter === undefined || place.restricted === restrictedFilter),
  )

  return (
    <ContentListLayout
      title="地區"
      search={search}
      onSearch={setSearch}
      error={places.error ?? areas.error}
      headerButtons={
        <Button type="primary" onClick={() => setEditor({ mode: "create" })}>
          新增
        </Button>
      }
      filters={
        <>
          <Select
            allowClear
            aria-label="大陸"
            placeholder="大陸"
            style={{ width: 160 }}
            value={areaFilter}
            onChange={setAreaFilter}
            options={namedChoices(areaList)}
          />
          <Select
            allowClear
            aria-label="是否禁區"
            placeholder="是否禁區"
            style={{ width: 120 }}
            value={restrictedFilter}
            onChange={setRestrictedFilter}
            options={[
              { value: true, label: "禁區" },
              { value: false, label: "一般" },
            ]}
          />
        </>
      }
    >
      <Table<PlaceVo>
        rowKey="id"
        size="small"
        loading={places.isLoading}
        dataSource={rows}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          {
            key: "name",
            title: "名稱",
            render: (_, place) => (
              <>
                {place.name}
                {place.restricted && <Tag style={{ marginLeft: 8 }}>禁區</Tag>}
              </>
            ),
          },
          { key: "area", title: "大陸", render: (_, place) => areaOf(place)?.name ?? "" },
          {
            key: "recommended",
            title: "推薦等級",
            render: (_, place) => `${place.recommendedMinimum}～${place.recommendedMaximum}`,
          },
          {
            key: "actions",
            title: "",
            width: 56,
            render: (_, place) => (
              <RowEditButton onClick={() => setEditor({ mode: "edit", record: place })} />
            ),
          },
        ]}
      />
      <ContentFormModal
        label="地區"
        state={editor}
        onClose={() => setEditor(null)}
        repository={placeRepository}
        records={all}
        blankValues={blankPlaceValues}
        toValues={placeToValues}
        toVo={placeFromValues}
      >
        {(_form, mode) => <PlaceFields mode={mode} />}
      </ContentFormModal>
    </ContentListLayout>
  )
}
