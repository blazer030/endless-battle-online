import type { ReactNode } from "react"
import {
  bonusText,
  clampQuantity,
  holeMarks,
  isSupply,
  itemTitle,
  supplyText,
  type Requirement,
} from "../game/inventory"
import type { CharacterView, InventoryItem, InventoryView } from "../game/types"

// 變更的結果：背包換成回應內容；有角色就直接用，否則重新讀 /me 同步金錢與能力
export type Outcome = { inventory?: InventoryView; character?: CharacterView }

// 回傳錯誤訊息，成功時為 null
export type Mutate = (action: () => Promise<Outcome>) => Promise<string | null>

export function Tabs<T extends string | number>({
  label,
  options,
  selected,
  onSelect,
  variant = "chips",
}: {
  label: string
  options: { id: T; label: string }[]
  selected: T
  onSelect: (id: T) => void
  variant?: "pages" | "chips"
}) {
  return (
    <div className={`bag__tabs bag__tabs--${variant}`} role="tablist" aria-label={label}>
      {options.map((option) => (
        <button
          key={option.id}
          type="button"
          role="tab"
          className="bag__tab"
          aria-selected={option.id === selected}
          onClick={() => onSelect(option.id)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

export function ItemName({ item }: { item: InventoryItem }) {
  const holes = holeMarks(item)
  return (
    <span className="bag__item-head">
      <span className="bag__item-name">{itemTitle(item)}</span>
      {holes !== "" && (
        <>
          {" "}
          <span className="bag__holes">{holes}</span>
        </>
      )}
    </span>
  )
}

export function ItemDetail({ item }: { item: InventoryItem }): ReactNode {
  const text = isSupply(item) ? supplyText(item) : bonusText(item.bonus)
  return text === "" ? null : <p className="bag__detail">{text}</p>
}

export function Requirements({ rows }: { rows: Requirement[] }) {
  if (rows.length === 0) return null
  return (
    <ul className="bag__requirements" aria-label="需求">
      {rows.map(({ text, met }) => (
        <li key={text} className={met ? "bag__requirement" : "bag__requirement crimson-text"}>
          {text}
        </li>
      ))}
    </ul>
  )
}

export function QuantityStepper({
  value,
  max,
  disabled,
  onChange,
}: {
  value: number
  max: number
  disabled: boolean
  onChange: (value: number) => void
}) {
  return (
    <span className="bag__stepper">
      <button
        type="button"
        aria-label="減少數量"
        disabled={disabled || value <= 1}
        onClick={() => onChange(clampQuantity(value - 1, max))}
      >
        −
      </button>
      <span className="bag__stepper-value">{value}</span>
      <button
        type="button"
        aria-label="增加數量"
        disabled={disabled || value >= max}
        onClick={() => onChange(clampQuantity(value + 1, max))}
      >
        ＋
      </button>
    </span>
  )
}
