import { DeleteOutlined } from "@ant-design/icons"
import { Button, Checkbox, Col, Form, Input, InputNumber, Row, Select, Space } from "antd"
import { filterChoice, namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { contentElements, elementLabels } from "../shared/elements"
import { NumberField } from "../shared/fields"
import { requiredText, skillLevelRule } from "../shared/rules"
import { itemRepository } from "../items/itemRepository"
import { missionRepository } from "../missions/missionRepository"
import { skillRepository } from "../skills/skillRepository"
import type { SkillVo } from "../skills/SkillVo"
import { useContentList } from "../shared/repository"
import { globalPlaceId, globalPlaceName, usePlaceOptions } from "../shared/placeOptions"
import {
  blankMonsterValues,
  monsterFromValues,
  monsterToValues,
  type MonsterFormValues,
} from "./monsterFormValues"
import { monsterRepository } from "./monsterRepository"
import type { MonsterVo } from "./MonsterVo"

const combatFields: readonly (readonly [keyof MonsterFormValues, string])[] = [
  ["level", "等級"],
  ["hitPoints", "HP"],
  ["skillPoints", "SP"],
  ["attack", "攻擊"],
  ["magicAttack", "魔攻"],
  ["defense", "防禦"],
  ["magicDefense", "魔防"],
  ["strength", "力量"],
  ["intelligence", "智力"],
  ["vitality", "體質"],
  ["agility", "敏捷"],
  ["charisma", "魅力"],
  ["faith", "信仰"],
  ["jobExperience", "職業經驗"],
]

function DropList() {
  const items = useContentList(itemRepository)
  const choices = namedChoices(items.data ?? [])
  return (
    <Form.List name="drops">
      {(rows, { add, remove }) => (
        <Form.Item label="掉落（道具、掉落難度）">
          {rows.map((row, index) => (
            <Space key={row.key} align="start" style={{ display: "flex", marginBottom: 8 }}>
              <Form.Item
                name={[row.name, "itemId"]}
                noStyle
                rules={[{ required: true, message: "請選擇道具" }]}
              >
                <Select
                  aria-label={`掉落道具 ${index + 1}`}
                  showSearch
                  filterOption={filterChoice}
                  options={choices}
                  style={{ width: 280 }}
                />
              </Form.Item>
              <Form.Item
                name={[row.name, "dropRarity"]}
                noStyle
                rules={[{ required: true, message: "請填寫掉落難度" }]}
              >
                <InputNumber
                  aria-label={`掉落難度 ${index + 1}`}
                  precision={0}
                  min={1}
                  placeholder="掉落難度"
                />
              </Form.Item>
              <Button
                aria-label={`刪除掉落 ${index + 1}`}
                icon={<DeleteOutlined />}
                onClick={() => remove(row.name)}
              />
            </Space>
          ))}
          <Button onClick={() => add({ dropRarity: 100 })}>新增掉落</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}

function SkillRow({
  rowName,
  index,
  skills,
  onRemove,
}: {
  rowName: number
  index: number
  skills: readonly SkillVo[]
  onRemove: () => void
}) {
  const form = Form.useFormInstance<MonsterFormValues>()
  return (
    <Space align="start" style={{ display: "flex", marginBottom: 8 }}>
      <Form.Item
        name={[rowName, "skillId"]}
        noStyle
        rules={[{ required: true, message: "請選擇技能" }]}
      >
        <Select
          aria-label={`技能 ${index + 1}`}
          showSearch
          filterOption={filterChoice}
          options={namedChoices(skills)}
          style={{ width: 240 }}
        />
      </Form.Item>
      <Form.Item
        name={[rowName, "level"]}
        dependencies={[["skills", rowName, "skillId"]]}
        style={{ marginBottom: 0 }}
        rules={[
          { required: true, message: "請填寫等級" },
          skillLevelRule(skills, () => form.getFieldValue(["skills", rowName, "skillId"])),
        ]}
      >
        <InputNumber
          aria-label={`技能等級 ${index + 1}`}
          precision={0}
          min={1}
          placeholder="等級"
        />
      </Form.Item>
      <Form.Item
        name={[rowName, "activationRate"]}
        noStyle
        rules={[{ required: true, message: "請填寫發動率" }]}
      >
        <InputNumber
          aria-label={`發動率 ${index + 1}`}
          precision={0}
          min={1}
          max={100}
          placeholder="發動率"
        />
      </Form.Item>
      <Button aria-label={`刪除技能 ${index + 1}`} icon={<DeleteOutlined />} onClick={onRemove} />
    </Space>
  )
}

function SkillList() {
  const skills = useContentList(skillRepository).data ?? []
  return (
    <Form.List name="skills">
      {(rows, { add, remove }) => (
        <Form.Item label="技能（技能、等級、發動率）">
          {rows.map((row, index) => (
            <SkillRow
              key={row.key}
              rowName={row.name}
              index={index}
              skills={skills}
              onRemove={() => remove(row.name)}
            />
          ))}
          <Button onClick={() => add({ level: 1, activationRate: 10 })}>新增技能</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}

function MonsterFields({ mode }: { mode: "edit" | "create" }) {
  const form = Form.useFormInstance<MonsterFormValues>()
  const placeId = Form.useWatch("placeId", form)
  const isNpc = Form.useWatch("isNpc", form)
  const places = usePlaceOptions().data ?? []
  const missions = useContentList(missionRepository).data ?? []
  const subPlaceNames = places.find((place) => place.id === placeId)?.subPlaceNames ?? []

  return (
    <>
      <Row gutter={16}>
        <Col span={8}>
          <Form.Item name="id" label="id" rules={[requiredText("id")]}>
            <Input disabled={mode === "edit"} />
          </Form.Item>
        </Col>
        <Col span={10}>
          <Form.Item name="name" label="名稱" rules={[requiredText("名稱")]}>
            <Input />
          </Form.Item>
        </Col>
        <Col span={6}>
          <Form.Item name="isNpc" valuePropName="checked" style={{ marginTop: 30 }}>
            <Checkbox>NPC</Checkbox>
          </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        <Col span={10}>
          <Form.Item name="placeId" label="地區">
            <Select
              showSearch
              filterOption={filterChoice}
              options={[
                { value: globalPlaceId, label: globalPlaceName, searchText: globalPlaceName },
                ...namedChoices(places),
              ]}
              onChange={() => form.setFieldValue("subPlaceIndexes", [])}
            />
          </Form.Item>
        </Col>
        <Col span={14}>
          <Form.Item name="exclusiveMissionId" label="限定任務">
            <Select
              showSearch
              filterOption={filterChoice}
              options={[
                { value: "", label: "無", searchText: "無" },
                ...namedChoices(
                  missions
                    .filter((mission) => !mission.daily)
                    .map((mission) => ({ id: mission.id, name: mission.title })),
                ),
              ]}
            />
          </Form.Item>
        </Col>
      </Row>
      {subPlaceNames.length > 0 && (
        <Form.Item name="subPlaceIndexes" label="出現的子地區（都不勾代表每個子地區都會出現）">
          {/* 子地區序號從 1 開始 */}
          <Checkbox.Group
            options={subPlaceNames.map((name, index) => ({ label: name, value: index + 1 }))}
          />
        </Form.Item>
      )}
      {isNpc ? (
        <Row gutter={16}>
          <Col span={6}>
            <NumberField name="encounterRate" label="遇敵率" />
          </Col>
          <Col span={18}>
            <Form.Item name="npcMessage" label="遭遇訊息">
              <Input.TextArea autoSize={{ minRows: 1, maxRows: 4 }} />
            </Form.Item>
          </Col>
        </Row>
      ) : (
        <>
          <Row gutter={16}>
            <Col span={6}>
              <NumberField name="encounterRate" label="遇敵率" />
            </Col>
            <Col span={6}>
              <Form.Item name="element" label="屬性">
                <Select
                  options={contentElements.map((element) => ({
                    value: element,
                    label: elementLabels[element],
                  }))}
                />
              </Form.Item>
            </Col>
            {combatFields.map(([name, label]) => (
              <Col key={name} span={6}>
                <NumberField name={name} label={label} />
              </Col>
            ))}
          </Row>
          <DropList />
          <SkillList />
        </>
      )}
    </>
  )
}

function useMonsterOptionError() {
  const items = useContentList(itemRepository)
  const skills = useContentList(skillRepository)
  const missions = useContentList(missionRepository)
  const places = usePlaceOptions()
  return items.error ?? skills.error ?? missions.error ?? places.error
}

type Props = {
  state: ContentEditorState<MonsterVo>
  monsters: readonly MonsterVo[]
  onClose: () => void
}

export function MonsterFormModal({ state, monsters, onClose }: Props) {
  return (
    <ContentFormModal
      label="怪物"
      state={state}
      onClose={onClose}
      repository={monsterRepository}
      records={monsters}
      blankValues={blankMonsterValues}
      toValues={monsterToValues}
      toVo={monsterFromValues}
      useOptionError={useMonsterOptionError}
    >
      {(_form, mode) => <MonsterFields mode={mode} />}
    </ContentFormModal>
  )
}
