import { useEffect, useRef, useState } from "react"
import type { LogLine, LogLineKind } from "../game/battleLog"
import "./BattleLog.css"

type Tab = "all" | "drop" | "trigger"

const tabs: { id: Tab; label: string }[] = [
  { id: "all", label: "全部" },
  { id: "drop", label: "掉落" },
  { id: "trigger", label: "觸發" },
]

// 掉落與觸發行把數字（含千分位逗號）包成強調色
const emphasizeNumbers = (text: string) =>
  text
    .split(/(\d[\d,]*)/)
    .map((part, index) => (index % 2 === 1 ? <b key={index}>{part}</b> : part))

const matches = (tab: Tab, kind: LogLineKind) => tab === "all" || tab === kind

export function BattleLog({ lines }: { lines: LogLine[] }) {
  const [tab, setTab] = useState<Tab>("all")
  const scroller = useRef<HTMLUListElement>(null)
  const visible = lines.filter((line) => matches(tab, line.kind))

  useEffect(() => {
    const element = scroller.current
    if (element) element.scrollTop = element.scrollHeight
  }, [visible.length, tab])

  return (
    <section className="battle-log">
      <div className="battle-log__tabs" role="tablist">
        {tabs.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            className="battle-log__tab"
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
      </div>
      <ul className="battle-log__lines" ref={scroller}>
        {visible.map((line) => (
          <li key={line.id} className={`battle-log__line battle-log__line--${line.kind}`}>
            {line.kind === "drop" || line.kind === "trigger"
              ? emphasizeNumbers(line.text)
              : line.text}
          </li>
        ))}
      </ul>
    </section>
  )
}
