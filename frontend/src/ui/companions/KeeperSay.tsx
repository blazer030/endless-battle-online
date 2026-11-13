import type { ReactNode } from "react"
import type { KeeperLine } from "../../game/companions"
import { formatNumber } from "../../game/format"
import purse from "../../assets/town/purse.webp"

// 管理人傳話：頭像＋一句，失敗的那句偏紅
export const KeeperSay = ({ line }: { line: KeeperLine }) => (
  <div className={`keeper-say${line.failed ? " keeper-say--failed" : ""}`} role="status">
    <span className={`keeper-say__face keeper-say__face--${line.keeper}`} aria-hidden="true" />
    <p>{line.text}</p>
  </div>
)

type HeaderProps = { title: ReactNode; line: KeeperLine | null; money: number }

// 木台標頭：平常是名稱，有傳話時名稱換成管理人頭像＋一句；金錢一直在右邊
export const CounterHeader = ({ title, line, money }: HeaderProps) => (
  <header className="inn__header companions__header">
    {line === null ? <span className="inn__name">{title}</span> : <KeeperSay line={line} />}
    <span className="inn__purse" aria-label="持有金錢">
      <img src={purse} alt="" />
      <b>{formatNumber(money)}</b>
    </span>
  </header>
)
