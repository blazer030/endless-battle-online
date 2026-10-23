import { useRef, type KeyboardEvent, type ReactNode } from "react"
import {
  bonusText,
  clampQuantity,
  holeMarks,
  itemTitle,
  supplyText,
  type Requirement,
} from "../game/inventory"
import type { CharacterView, InventoryItem, InventoryView } from "../game/types"

// 變更的結果：背包換成回應內容；有角色就直接用，否則重新讀 /me 同步金錢與能力
export type Outcome = { inventory?: InventoryView; character?: CharacterView }

// 回傳錯誤訊息，成功時為 null
export type Mutate = (action: () => Promise<Outcome>) => Promise<string | null>

const tabIdOf = (tabsId: string, option: string | number) => `${tabsId}-tab-${option}`
const panelIdOf = (tabsId: string, option: string | number) => `${tabsId}-panel-${option}`

// WAI-ARIA tablist：選中的 tab 才在 Tab 鍵順序裡，左右鍵、Home、End 切換並移動焦點
export function Tabs<T extends string | number>({
  id,
  label,
  options,
  selected,
  onSelect,
  variant = "chips",
}: {
  id: string
  label: string
  options: { id: T; label: string }[]
  selected: T
  onSelect: (id: T) => void
  variant?: "pages" | "chips"
}) {
  const buttons = useRef(new Map<T, HTMLButtonElement>())

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = options.findIndex((option) => option.id === selected)
    const last = options.length - 1
    const next =
      event.key === "ArrowRight"
        ? (index + 1) % options.length
        : event.key === "ArrowLeft"
          ? (index + last) % options.length
          : event.key === "Home"
            ? 0
            : event.key === "End"
              ? last
              : null
    if (next === null) return
    event.preventDefault()
    const target = options[next].id
    onSelect(target)
    buttons.current.get(target)?.focus()
  }

  return (
    <div
      className={`bag__tabs bag__tabs--${variant}`}
      role="tablist"
      aria-label={label}
      onKeyDown={onKeyDown}
    >
      {options.map((option) => (
        <button
          key={option.id}
          ref={(element) => {
            if (element === null) buttons.current.delete(option.id)
            else buttons.current.set(option.id, element)
          }}
          type="button"
          role="tab"
          id={tabIdOf(id, option.id)}
          className="bag__tab"
          aria-selected={option.id === selected}
          aria-controls={panelIdOf(id, option.id)}
          tabIndex={option.id === selected ? 0 : -1}
          onClick={() => onSelect(option.id)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

// 目前選中分頁的內容區，以 tab 的文字當名稱
export function TabPanel({
  tabsId,
  selected,
  children,
}: {
  tabsId: string
  selected: string | number
  children: ReactNode
}) {
  return (
    <div
      className="tab-panel"
      role="tabpanel"
      id={panelIdOf(tabsId, selected)}
      aria-labelledby={tabIdOf(tabsId, selected)}
    >
      {children}
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
  const text = item.consumable === "Supply" ? supplyText(item) : bonusText(item.bonus)
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
