import { Checkbox, Col, Form, Input, InputNumber, Row, Select } from "antd"
import { contentElements } from "../monsters/MonsterVo"
import { filterChoice, namedChoices } from "../shared/choices"
import { ContentFormModal, type ContentEditorState } from "../shared/ContentFormModal"
import { elementLabels } from "../shared/elementLabels"
import { NumberField } from "../shared/fields"
import { useSkillOptions } from "../shared/namedOptions"
import { requiredNumber, requiredText, skillLevelRule } from "../shared/rules"
import { usesAmount } from "./ItemConverter"
import {
  blankItemValues,
  itemFromValues,
  itemToValues,
  type ItemFormValues,
} from "./itemFormValues"
import {
  amountMeanings,
  consumableKindLabels,
  elixirKindLabels,
  itemCategoryLabels,
  itemStatLabels,
} from "./itemLabels"
import { itemRepository } from "./itemRepository"
import {
  consumableKinds,
  elixirKinds,
  itemCategories,
  itemRequirementKeys,
  itemStatKeys,
  type ConsumableKind,
  type ItemCategory,
  type ItemStatKey,
  type ItemVo,
} from "./ItemVo"

const equipmentStatKeys = itemStatKeys.filter((key) => key !== "maxHitPoints")

const isEquipment = (category: ItemCategory | undefined) =>
  category === "Weapon" ||
  category === "Head" ||
  category === "Body" ||
  category === "Hand" ||
  category === "Foot"

export function StatInputs({
  group,
  name,
  keys,
}: {
  group: string
  name: (string | number)[]
  keys: readonly ItemStatKey[]
}) {
  return (
    <Form.Item label={group}>
      <Row gutter={[12, 8]}>
        {keys.map((key) => (
          <Col key={key} span={6}>
            <Form.Item name={[...name, key]} noStyle>
              <InputNumber
                aria-label={`${group} ${itemStatLabels[key]}`}
                precision={0}
                placeholder={itemStatLabels[key]}
                style={{ width: "100%" }}
              />
            </Form.Item>
          </Col>
        ))}
      </Row>
    </Form.Item>
  )
}

function WeaponFields() {
  const form = Form.useFormInstance<ItemFormValues>()
  const skillId = Form.useWatch("weaponSkillId", form)
  const skills = useSkillOptions().data ?? []
  return (
    <Row gutter={16}>
      <Col span={6}>
        <Form.Item name="element" label="屬性">
          <Select
            options={[
              { value: "", label: elementLabels.None },
              ...contentElements
                .filter((element) => element !== "None")
                .map((value) => ({ value, label: elementLabels[value] })),
            ]}
          />
        </Form.Item>
      </Col>
      <Col span={8}>
        <Form.Item name="weaponSkillId" label="武器技能">
          <Select
            showSearch
            filterOption={filterChoice}
            options={[{ value: "", label: "無", searchText: "無" }, ...namedChoices(skills)]}
          />
        </Form.Item>
      </Col>
      {skillId ? (
        <>
          <Col span={5}>
            <Form.Item
              name="weaponSkillLevel"
              label="武器技能等級"
              dependencies={["weaponSkillId"]}
              rules={[
                requiredNumber("等級"),
                skillLevelRule(skills, () => form.getFieldValue("weaponSkillId")),
              ]}
            >
              <InputNumber precision={0} min={1} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item
              name="weaponSkillRate"
              label="武器技能發動率"
              rules={[requiredNumber("發動率")]}
            >
              <InputNumber precision={0} min={1} max={100} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
        </>
      ) : null}
    </Row>
  )
}

function EquipmentFields({ category }: { category: ItemCategory }) {
  return (
    <>
      <StatInputs group="加成" name={["bonus"]} keys={equipmentStatKeys} />
      <Form.Item label="需求">
        <Row gutter={12}>
          {itemRequirementKeys.map((key) => (
            <Col key={key} span={6}>
              <Form.Item name={["requires", key]} noStyle>
                <InputNumber
                  aria-label={`需求 ${itemStatLabels[key]}`}
                  precision={0}
                  min={0}
                  placeholder={itemStatLabels[key]}
                  style={{ width: "100%" }}
                />
              </Form.Item>
            </Col>
          ))}
        </Row>
      </Form.Item>
      {isEquipment(category) && (
        <Row gutter={16}>
          <Col span={6}>
            <Form.Item name="holes" label="孔數">
              <InputNumber precision={0} min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="refineMax" label="精鍊上限">
              <InputNumber precision={0} min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
        </Row>
      )}
      {category === "Weapon" && <WeaponFields />}
    </>
  )
}

function ConsumableFields({ kind }: { kind: ConsumableKind | undefined }) {
  return (
    <>
      <Form.Item name="consumableKind" label="消耗品種類">
        <Select
          virtual={false}
          options={consumableKinds.map((value) => ({ value, label: consumableKindLabels[value] }))}
        />
      </Form.Item>
      {kind === "Supply" && (
        <Row gutter={16}>
          <Col span={6}>
            <Form.Item name="supplyHitPoints" label="回復 HP">
              <InputNumber precision={0} min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="supplySkillPoints" label="回復 SP">
              <InputNumber precision={0} min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
        </Row>
      )}
      {kind === "StatPotion" && (
        <StatInputs group="加成" name={["bonus"]} keys={equipmentStatKeys} />
      )}
      {kind === "Elixir" && (
        <Row gutter={16}>
          <Col span={8}>
            <Form.Item name={["elixir", "kind"]} label="密藥種類">
              <Select
                options={elixirKinds.map((value) => ({ value, label: elixirKindLabels[value] }))}
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name={["elixir", "multiplier"]}
              label="倍率"
              rules={[requiredNumber("倍率")]}
            >
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <NumberField name={["elixir", "seconds"]} label="秒數" min={0} />
          </Col>
        </Row>
      )}
      {kind && usesAmount(kind) && (
        <Row gutter={16}>
          <Col span={8}>
            <Form.Item
              name="amount"
              label="數值"
              extra={amountMeanings[kind]}
              rules={[requiredNumber("數值")]}
            >
              <InputNumber precision={0} style={{ width: "100%" }} />
            </Form.Item>
          </Col>
        </Row>
      )}
    </>
  )
}

function ItemFields({ mode }: { mode: "edit" | "create" }) {
  const form = Form.useFormInstance<ItemFormValues>()
  const category = Form.useWatch("category", form)
  const consumableKind = Form.useWatch("consumableKind", { form, preserve: true })
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
          <Form.Item name="category" label="分類">
            <Select
              virtual={false}
              options={itemCategories.map((value) => ({ value, label: itemCategoryLabels[value] }))}
            />
          </Form.Item>
        </Col>
        <Col span={6}>
          <NumberField name="price" label="價格" min={0} />
        </Col>
        <Col span={6}>
          <NumberField name="shopLevel" label="商店等級" min={0} />
        </Col>
        <Col span={6}>
          <Form.Item name="shopSellable" valuePropName="checked" style={{ marginTop: 30 }}>
            <Checkbox>商店販售</Checkbox>
          </Form.Item>
        </Col>
        <Col span={6}>
          <Form.Item name="bound" valuePropName="checked" style={{ marginTop: 30 }}>
            <Checkbox>綁定</Checkbox>
          </Form.Item>
        </Col>
      </Row>
      {category && (isEquipment(category) || category === "Accessory") && (
        <EquipmentFields category={category} />
      )}
      {category === "Consumable" && <ConsumableFields kind={consumableKind} />}
    </>
  )
}

type Props = {
  state: ContentEditorState<ItemVo>
  items: readonly ItemVo[]
  onClose: () => void
}

export function ItemFormModal({ state, items, onClose }: Props) {
  return (
    <ContentFormModal
      label="道具"
      state={state}
      onClose={onClose}
      repository={itemRepository}
      records={items}
      blankValues={blankItemValues}
      toValues={itemToValues}
      toVo={itemFromValues}
    >
      {(_form, mode) => <ItemFields mode={mode} />}
    </ContentFormModal>
  )
}
