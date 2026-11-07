import { useQuery } from "@tanstack/react-query"
import { Col, Form, Input, InputNumber, Radio, Row, Select } from "antd"
import { useState } from "react"
import { jobRepository } from "../jobs/jobRepository"
import { filterChoice, namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { NumberField } from "../shared/fields"
import { requiredNumber, requiredText } from "../shared/rules"
import { ConditionList, CostList } from "./SkillConditionCostFields"
import { EffectList } from "./SkillEffectFields"
import { skillSlotLabels, skillTimingLabels } from "./skillLabels"
import {
  blankSkillValues,
  resizeLevels,
  skillFromValues,
  skillToValues,
  type SkillFormValues,
} from "./skillFormValues"
import { skillRepository } from "./skillRepository"
import { skillSlotTypes, skillTimings, type SkillVo } from "./SkillVo"

function LevelFields({ index }: { index: number }) {
  return (
    <>
      <Row gutter={16}>
        <Col span={8}>
          <NumberField name={["levels", index, "learningCost"]} label="學習費用" min={0} />
        </Col>
        <Col span={8}>
          <NumberField name={["levels", index, "proficiencyRequired"]} label="所需熟練度" min={0} />
        </Col>
        <Col span={8}>
          <NumberField name={["levels", index, "cooldown"]} label="冷卻" min={0} />
        </Col>
      </Row>
      <ConditionList levelIndex={index} />
      <CostList levelIndex={index} />
      <EffectList levelIndex={index} />
    </>
  )
}

type ActiveLevelProps = { activeLevel: number; onActiveLevelChange: (level: number) => void }

function LevelTabs({ activeLevel, onActiveLevelChange }: ActiveLevelProps) {
  const form = Form.useFormInstance<SkillFormValues>()
  const levelCount = Form.useWatch("levels", { form, preserve: true })?.length ?? 0
  const current = Math.min(activeLevel, Math.max(levelCount - 1, 0))
  // 等級可能很多，用會換行的按鈕列。每一級都要掛載，存檔時才驗證得到；沒有選到的只是隱藏
  return (
    <>
      <Radio.Group
        aria-label="編輯的等級"
        optionType="button"
        size="small"
        style={{ marginBottom: 16 }}
        value={current}
        onChange={(event) => onActiveLevelChange(event.target.value as number)}
        options={Array.from({ length: levelCount }, (_, index) => ({
          value: index,
          label: `第 ${index + 1} 級`,
        }))}
      />
      {Array.from({ length: levelCount }, (_, index) => (
        <div
          key={index}
          hidden={index !== current}
          data-testid={index === current ? "active-level" : undefined}
        >
          <LevelFields index={index} />
        </div>
      ))}
    </>
  )
}

function SkillFields({
  skills,
  mode,
  activeLevel,
  onActiveLevelChange,
}: { skills: readonly SkillVo[]; mode: "edit" | "create" } & ActiveLevelProps) {
  const form = Form.useFormInstance<SkillFormValues>()
  const id = Form.useWatch("id", form)
  const prerequisiteSkillId = Form.useWatch("prerequisiteSkillId", form)
  const jobs = useQuery({
    queryKey: jobRepository.queryKey,
    queryFn: jobRepository.list,
    retry: false,
  })

  // 打字時不增減等級：從 10 改成 11 的途中會先經過 1
  const applyMaxLevel = () => {
    const maxLevel = form.getFieldValue("maxLevel") as number | null
    if (maxLevel !== null && maxLevel >= 1) {
      form.setFieldValue(
        "levels",
        resizeLevels(form.getFieldValue("levels") as SkillFormValues["levels"], maxLevel),
      )
    }
  }

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
        <Col span={4}>
          <Form.Item name="slotType" label="欄位">
            <Select
              options={skillSlotTypes.map((value) => ({ value, label: skillSlotLabels[value] }))}
            />
          </Form.Item>
        </Col>
        <Col span={4}>
          <Form.Item name="timing" label="時機">
            <Select
              options={skillTimings.map((value) => ({ value, label: skillTimingLabels[value] }))}
            />
          </Form.Item>
        </Col>
      </Row>
      <Form.Item name="description" label="說明">
        <Input.TextArea autoSize={{ minRows: 1, maxRows: 3 }} />
      </Form.Item>
      <Row gutter={16}>
        <Col span={10}>
          <Form.Item name="jobIds" label="能學的職業">
            <Select
              mode="multiple"
              filterOption={filterChoice}
              options={namedChoices(jobs.data ?? [])}
            />
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item name="prerequisiteSkillId" label="前置技能">
            <Select
              showSearch
              filterOption={filterChoice}
              options={[
                { value: "", label: "無", searchText: "無" },
                ...namedChoices(skills.filter((skill) => skill.id !== id)),
              ]}
              onChange={(skillId: string) => {
                if (skillId === "") {
                  form.setFieldValue("prerequisiteLevel", null)
                }
              }}
            />
          </Form.Item>
        </Col>
        {prerequisiteSkillId ? (
          <Col span={6}>
            <Form.Item
              name="prerequisiteLevel"
              label="前置技能等級"
              rules={[requiredNumber("前置技能等級")]}
            >
              <InputNumber precision={0} min={1} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
        ) : null}
      </Row>
      <Form.Item name="maxLevel" label="等級上限" rules={[requiredNumber("等級上限")]}>
        <InputNumber precision={0} min={1} onBlur={applyMaxLevel} onPressEnter={applyMaxLevel} />
      </Form.Item>
      <LevelTabs activeLevel={activeLevel} onActiveLevelChange={onActiveLevelChange} />
    </>
  )
}

type Props = {
  state: ContentEditorState<SkillVo>
  skills: readonly SkillVo[]
  onClose: () => void
}

export function SkillFormModal({ state, skills, onClose }: Props) {
  const [activeLevel, setActiveLevel] = useState(0)
  return (
    <ContentFormModal
      label="技能"
      state={state}
      onClose={() => {
        setActiveLevel(0)
        onClose()
      }}
      repository={skillRepository}
      records={skills}
      blankValues={blankSkillValues}
      toValues={skillToValues}
      toVo={skillFromValues}
      onValidationFailed={(fieldNames) => {
        // 有問題的欄位可能在沒有顯示的那一級，要切過去才看得到錯誤
        const levelField = fieldNames.find((name) => name[0] === "levels")
        if (typeof levelField?.[1] === "number") {
          setActiveLevel(levelField[1])
        }
      }}
    >
      {(_form, mode) => (
        <SkillFields
          skills={skills}
          mode={mode}
          activeLevel={activeLevel}
          onActiveLevelChange={setActiveLevel}
        />
      )}
    </ContentFormModal>
  )
}
