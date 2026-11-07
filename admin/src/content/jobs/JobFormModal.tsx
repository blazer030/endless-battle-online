import { DeleteOutlined } from "@ant-design/icons"
import { Button, Col, Form, Input, InputNumber, Row, Select, Space, Typography } from "antd"
import { filterChoice, namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { NumberField } from "../shared/fields"
import { requiredNumber, requiredText } from "../shared/rules"
import {
  contentStatChoices,
  contentStats,
  type ContentStatKey,
  type ContentStatValue,
} from "../shared/stats"
import { jobRepository } from "./jobRepository"
import { JobVo } from "./JobVo"

type JobFormValues = {
  id: string
  name: string
  tier: number
  // 空字串代表「無」
  parentJobId: string
  unlockLevel: number
  proficiencyRequired: number
  statRequirements: { stat: ContentStatValue; minimum: number }[]
  growth: { stat: ContentStatKey; minimum: number; maximum: number }[]
}

const blankJobValues: JobFormValues = {
  id: "",
  name: "",
  tier: 1,
  parentJobId: "",
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
  growth: contentStats.map(({ key }) => ({ stat: key, minimum: 0, maximum: 0 })),
}

const jobToValues = (job: JobVo): JobFormValues => ({
  id: job.id,
  name: job.name,
  tier: job.tier,
  parentJobId: job.parentJobId ?? "",
  unlockLevel: job.unlockLevel,
  proficiencyRequired: job.proficiencyRequired,
  statRequirements: job.statRequirements.map((requirement) => ({ ...requirement })),
  growth: job.growth.map((row) => ({ ...row })),
})

const jobFromValues = (values: JobFormValues): JobVo =>
  JobVo.create({
    ...values,
    id: values.id.trim(),
    parentJobId: values.parentJobId === "" ? null : values.parentJobId,
  })

function StatRequirementList() {
  return (
    <Form.List name="statRequirements">
      {(rows, { add, remove }) => (
        <Form.Item label="能力需求">
          {rows.map((row, index) => (
            <Space key={row.key} align="start" style={{ display: "flex", marginBottom: 8 }}>
              <Form.Item
                name={[row.name, "stat"]}
                noStyle
                rules={[{ required: true, message: "請選擇能力" }]}
              >
                <Select
                  aria-label={`能力 ${index + 1}`}
                  options={contentStatChoices}
                  style={{ width: 120 }}
                />
              </Form.Item>
              <Form.Item name={[row.name, "minimum"]} noStyle rules={[requiredNumber("能力需求")]}>
                <InputNumber
                  aria-label={`能力需求 ${index + 1}`}
                  precision={0}
                  min={0}
                  placeholder="最低"
                />
              </Form.Item>
              <Button
                aria-label={`刪除能力需求 ${index + 1}`}
                icon={<DeleteOutlined />}
                onClick={() => remove(row.name)}
              />
            </Space>
          ))}
          <Button onClick={() => add({ stat: "Strength" })}>新增能力需求</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}

function GrowthTable() {
  return (
    <Form.Item label="升級成長（最小～最大）">
      <Row gutter={[16, 8]}>
        {contentStats.map(({ key, label }, index) => (
          <Col key={key} span={12}>
            <Space align="baseline">
              <Typography.Text style={{ display: "inline-block", width: 40 }}>
                {label}
              </Typography.Text>
              <Form.Item
                name={["growth", index, "minimum"]}
                noStyle
                rules={[requiredNumber(`${label}最小`)]}
              >
                <InputNumber aria-label={`${label}最小`} precision={0} min={0} />
              </Form.Item>
              ～
              <Form.Item
                name={["growth", index, "maximum"]}
                noStyle
                rules={[requiredNumber(`${label}最大`)]}
              >
                <InputNumber aria-label={`${label}最大`} precision={0} min={0} />
              </Form.Item>
            </Space>
          </Col>
        ))}
      </Row>
    </Form.Item>
  )
}

function JobFields({ jobs, mode }: { jobs: readonly JobVo[]; mode: "edit" | "create" }) {
  const form = Form.useFormInstance<JobFormValues>()
  const id = Form.useWatch("id", form)
  return (
    <>
      <Row gutter={16}>
        <Col span={8}>
          <Form.Item name="id" label="id" rules={[requiredText("id")]}>
            <Input disabled={mode === "edit"} />
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item name="name" label="名稱" rules={[requiredText("名稱")]}>
            <Input />
          </Form.Item>
        </Col>
        <Col span={8}>
          <NumberField name="tier" label="階級" min={1} />
        </Col>
        <Col span={8}>
          <Form.Item name="parentJobId" label="上級職業">
            <Select
              showSearch
              filterOption={filterChoice}
              options={[
                { value: "", label: "無", searchText: "無" },
                ...namedChoices(jobs.filter((job) => job.id !== id)),
              ]}
            />
          </Form.Item>
        </Col>
        <Col span={8}>
          <NumberField name="unlockLevel" label="解鎖等級" min={0} />
        </Col>
        <Col span={8}>
          <NumberField name="proficiencyRequired" label="所需熟練度" min={0} />
        </Col>
      </Row>
      <StatRequirementList />
      <GrowthTable />
    </>
  )
}

type Props = {
  state: ContentEditorState<JobVo>
  jobs: readonly JobVo[]
  onClose: () => void
}

export function JobFormModal({ state, jobs, onClose }: Props) {
  return (
    <ContentFormModal
      label="職業"
      state={state}
      onClose={onClose}
      repository={jobRepository}
      records={jobs}
      blankValues={blankJobValues}
      toValues={jobToValues}
      toVo={jobFromValues}
    >
      {(_form, mode) => <JobFields jobs={jobs} mode={mode} />}
    </ContentFormModal>
  )
}
