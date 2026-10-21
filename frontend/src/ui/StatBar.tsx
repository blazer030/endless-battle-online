import { formatNumber } from "./formatNumber"
import "./StatBar.css"

type Props = { label: string; value: number; max: number; tone: "hp" | "sp" | "exp" }

export function StatBar({ label, value, max, tone }: Props) {
  const ratio = max > 0 ? Math.min(Math.max(value / max, 0), 1) : 0
  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(value, max)}
      aria-valuetext={`${formatNumber(value)} / ${formatNumber(max)}`}
      className={`stat-bar stat-bar--${tone}`}
    >
      <div className="stat-bar__fill" style={{ width: `${ratio * 100}%` }} />
    </div>
  )
}
