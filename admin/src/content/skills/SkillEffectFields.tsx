import { DeleteOutlined } from "@ant-design/icons"
import { Button, Checkbox, Col, Form, InputNumber, Row, Select } from "antd"
import type { ReactNode } from "react"
import { contentElements, elementLabels, type ContentElement } from "../shared/elements"
import { requiredNumber } from "../shared/rules"
import { defaultEffectRow, type EffectKind, type EffectMode } from "./skillEffectRows"
import { skillBasisChoices } from "./skillLabels"
import {
  skillAttackKinds,
  skillResources,
  skillStatChoices,
  skillTargets,
  type SkillAttackKind,
  type SkillResource,
  type SkillStat,
  type SkillTarget,
} from "./SkillVo"

const effectKindChoices: { value: EffectKind; label: string }[] = [
  { value: "StatChange", label: "能力增減" },
  { value: "BonusDamage", label: "追加傷害" },
  { value: "MultiHit", label: "連擊" },
  { value: "DamageReduction", label: "減傷" },
  { value: "Restore", label: "回復" },
  { value: "Stun", label: "暈眩" },
  { value: "Silence", label: "封鎖" },
  { value: "PercentDamage", label: "百分比傷害" },
  { value: "ElementAdvantage", label: "屬性剋制" },
  { value: "DamageOverTime", label: "持續傷害" },
  { value: "ReduceSkillPoints", label: "減少 SP" },
  { value: "AttackElement", label: "攻擊屬性" },
  { value: "DefenseElement", label: "防禦屬性" },
]

const statLabels: Record<SkillStat, string> = {
  Attack: "攻擊",
  MagicAttack: "魔攻",
  Defense: "防禦",
  MagicDefense: "魔防",
  Agility: "敏捷",
  Charisma: "魅力",
  HitRate: "命中",
  Evasion: "迴避",
  PhysicalCriticalChance: "物理暴擊率",
  MagicCriticalChance: "魔法暴擊率",
  PhysicalCriticalCap: "物理暴擊上限",
  MagicCriticalCap: "魔法暴擊上限",
  EscapeRate: "逃跑率",
}

const targetLabels: Record<SkillTarget, string> = { Caster: "自己", Opponent: "對手" }

const attackKindLabels: Record<SkillAttackKind, string> = {
  Physical: "物理",
  Magic: "魔法",
  Any: "全部",
}

const resourceLabels: Record<SkillResource, string> = { HitPoints: "HP", SkillPoints: "SP" }

const modeLabels: Record<EffectMode, string> = {
  percent: "百分比",
  amount: "固定值",
  scaling: "依能力",
  percentOfDamage: "傷害的百分比",
  percentOfMaximum: "最大值的百分比",
}

const modeChoices = (modes: EffectMode[]) =>
  modes.map((value) => ({ value, label: modeLabels[value] }))

const statChoices = skillStatChoices.map((value) => ({ value, label: statLabels[value] }))

const elementChoices = contentElements
  .filter((element): element is Exclude<ContentElement, "None"> => element !== "None")
  .map((value) => ({ value, label: elementLabels[value] }))

function Field({ span = 6, children }: { span?: number; children: ReactNode }) {
  return <Col span={span}>{children}</Col>
}

function RequiredNumber({
  name,
  label,
  aria,
  integer,
}: {
  name: (string | number)[]
  label: string
  aria: string
  integer?: boolean
}) {
  return (
    <Form.Item name={name} label={label} rules={[requiredNumber(label)]}>
      <InputNumber
        aria-label={aria}
        precision={integer ? 0 : undefined}
        style={{ width: "100%" }}
      />
    </Form.Item>
  )
}

function ScalingList({ rowName, label }: { rowName: number; label: string }) {
  return (
    <Col span={24}>
      <Form.List name={[rowName, "scaling"]}>
        {(rows, { add, remove }) => (
          <Form.Item label="依能力計算（基準 × 倍率）">
            {rows.map((row, index) => (
              <Row key={row.key} gutter={12} style={{ marginBottom: 8 }}>
                <Col span={8}>
                  <Form.Item name={[row.name, "basis"]} noStyle>
                    <Select
                      aria-label={`${label} 基準 ${index + 1}`}
                      options={skillBasisChoices}
                      style={{ width: "100%" }}
                    />
                  </Form.Item>
                </Col>
                <Col span={6}>
                  <Form.Item name={[row.name, "ratio"]} noStyle rules={[requiredNumber("倍率")]}>
                    <InputNumber
                      aria-label={`${label} 倍率 ${index + 1}`}
                      style={{ width: "100%" }}
                    />
                  </Form.Item>
                </Col>
                <Col flex="none">
                  <Button
                    aria-label={`刪除${label} 基準 ${index + 1}`}
                    icon={<DeleteOutlined />}
                    onClick={() => remove(row.name)}
                  />
                </Col>
              </Row>
            ))}
            <Button
              aria-label={`新增${label} 基準`}
              onClick={() => add({ basis: "Strength", ratio: 1 })}
            >
              新增基準
            </Button>
          </Form.Item>
        )}
      </Form.List>
    </Col>
  )
}

type RowProps = { levelIndex: number; rowName: number; index: number; onRemove: () => void }

function EffectRowFields({ levelIndex, rowName, index, onRemove }: RowProps) {
  const form = Form.useFormInstance()
  const path = ["levels", levelIndex, "effects", rowName]
  const kind = Form.useWatch([...path, "kind"], form) as EffectKind | undefined
  const mode = Form.useWatch([...path, "mode"], form) as EffectMode | undefined
  const hasChanceByStat = Form.useWatch([...path, "hasChanceByStat"], form) as boolean | undefined
  const label = `效果 ${index + 1}`

  const number = (field: string, text: string, integer = false) => (
    <Field>
      <RequiredNumber
        name={[rowName, field]}
        label={text}
        aria={`${label} ${text}`}
        integer={integer}
      />
    </Field>
  )
  const attackKind = (
    <Field>
      <Form.Item name={[rowName, "attackKind"]} label="攻擊方式">
        <Select
          aria-label={`${label} 攻擊方式`}
          options={skillAttackKinds.map((value) => ({ value, label: attackKindLabels[value] }))}
        />
      </Form.Item>
    </Field>
  )
  const optionalChance = (
    <Field>
      <Form.Item name={[rowName, "chance"]} label="機率（可不填）">
        <InputNumber
          aria-label={`${label} 機率`}
          precision={0}
          min={0}
          max={100}
          style={{ width: "100%" }}
        />
      </Form.Item>
    </Field>
  )
  const modeSelect = (modes: EffectMode[]) => (
    <Field>
      <Form.Item name={[rowName, "mode"]} label="方式">
        <Select
          aria-label={`${label} 方式`}
          options={modeChoices(modes)}
          onChange={(next: EffectMode) => {
            if (next === "scaling" && !form.getFieldValue([...path, "scaling"])) {
              form.setFieldValue([...path, "scaling"], [{ basis: "Strength", ratio: 1 }])
            }
          }}
        />
      </Form.Item>
    </Field>
  )
  const modeValue = () =>
    mode === "scaling" ? (
      <ScalingList rowName={rowName} label={label} />
    ) : mode ? (
      number(mode, modeLabels[mode])
    ) : null

  return (
    <div style={{ borderTop: "1px solid rgba(5, 5, 5, 0.06)", paddingTop: 12 }}>
      <Row gutter={12} align="bottom">
        <Field>
          <Form.Item name={[rowName, "kind"]} label="種類">
            <Select
              aria-label={`${label} 種類`}
              virtual={false}
              options={effectKindChoices}
              onChange={(next: EffectKind) =>
                form.setFieldValue(
                  path,
                  defaultEffectRow(next, form.getFieldValue([...path, "rounds"]) as number),
                )
              }
            />
          </Form.Item>
        </Field>
        {kind === "StatChange" && (
          <>
            <Field>
              <Form.Item name={[rowName, "target"]} label="對象">
                <Select
                  aria-label={`${label} 對象`}
                  options={skillTargets.map((value) => ({ value, label: targetLabels[value] }))}
                />
              </Form.Item>
            </Field>
            <Field>
              <Form.Item name={[rowName, "stat"]} label="能力">
                <Select aria-label={`${label} 能力`} options={statChoices} />
              </Form.Item>
            </Field>
            {modeSelect(["percent", "amount", "scaling"])}
            {mode !== "scaling" && modeValue()}
            {optionalChance}
          </>
        )}
        {kind === "BonusDamage" && (
          <>
            {attackKind}
            {optionalChance}
          </>
        )}
        {kind === "MultiHit" && (
          <>
            {attackKind}
            {number("hits", "連擊次數", true)}
          </>
        )}
        {kind === "DamageReduction" && (
          <>
            {attackKind}
            {number("percent", "百分比")}
          </>
        )}
        {kind === "Restore" && (
          <>
            <Field>
              <Form.Item name={[rowName, "resource"]} label="資源">
                <Select
                  aria-label={`${label} 資源`}
                  options={skillResources.map((value) => ({ value, label: resourceLabels[value] }))}
                />
              </Form.Item>
            </Field>
            {modeSelect(["scaling", "percentOfDamage", "percentOfMaximum"])}
            {mode !== "scaling" && modeValue()}
          </>
        )}
        {(kind === "Stun" || kind === "Silence") && number("chance", "機率", true)}
        {kind === "Stun" && (
          <Field>
            <Form.Item name={[rowName, "hasChanceByStat"]} valuePropName="checked">
              <Checkbox aria-label={`${label} 依能力調整`}>依能力調整機率</Checkbox>
            </Form.Item>
          </Field>
        )}
        {kind === "Stun" && hasChanceByStat && (
          <>
            <Field>
              <Form.Item
                name={[rowName, "chanceByStat", "stat"]}
                label="調整能力"
                rules={[{ required: true, message: "請選擇能力" }]}
              >
                <Select aria-label={`${label} 調整能力`} options={skillBasisChoices} />
              </Form.Item>
            </Field>
            <Field>
              <RequiredNumber
                name={[rowName, "chanceByStat", "percent"]}
                label="調整百分比"
                aria={`${label} 調整百分比`}
                integer
              />
            </Field>
          </>
        )}
        {(kind === "PercentDamage" || kind === "ElementAdvantage") && number("percent", "百分比")}
        {kind === "ReduceSkillPoints" && (
          <>
            {modeSelect(["amount", "percentOfMaximum"])}
            {modeValue()}
          </>
        )}
        {(kind === "AttackElement" || kind === "DefenseElement") && (
          <Field>
            <Form.Item name={[rowName, "element"]} label="屬性">
              <Select aria-label={`${label} 屬性`} options={elementChoices} />
            </Form.Item>
          </Field>
        )}
        {number("rounds", "回合", true)}
        <Col flex="none">
          <Form.Item>
            <Button
              aria-label={`刪除效果 ${index + 1}`}
              icon={<DeleteOutlined />}
              onClick={onRemove}
            />
          </Form.Item>
        </Col>
        {(kind === "BonusDamage" || kind === "DamageOverTime") && (
          <ScalingList rowName={rowName} label={label} />
        )}
        {(kind === "StatChange" || kind === "Restore") && mode === "scaling" && modeValue()}
      </Row>
    </div>
  )
}

export function EffectList({ levelIndex }: { levelIndex: number }) {
  return (
    <Form.List name={["levels", levelIndex, "effects"]}>
      {(rows, { add, remove }) => (
        <Form.Item label="效果">
          {rows.map((row, index) => (
            <EffectRowFields
              key={row.key}
              levelIndex={levelIndex}
              rowName={row.name}
              index={index}
              onRemove={() => remove(row.name)}
            />
          ))}
          <Button onClick={() => add(defaultEffectRow("StatChange"))}>新增效果</Button>
        </Form.Item>
      )}
    </Form.List>
  )
}
