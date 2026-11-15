import { useRef, type CSSProperties } from "react"
import {
  boardLoadFailedLine,
  completedRegular,
  dailyNotes,
  displayTitle,
  kindLabel,
  pageCounts,
  regularNotes,
  type BoardNote,
  type BoardPage,
  type MissionBoardVo,
} from "../../game/missions"
import { formatNumber } from "../../game/format"
import { KeeperSay } from "../companions/KeeperSay"

type Props = {
  board: MissionBoardVo | "failed" | null
  page: BoardPage
  level: number
  onPage: (page: BoardPage) => void
  onPick: (missionId: string) => void
  onPile: () => void
  onRetry: () => void
}

const pageNames: Record<BoardPage, string> = { daily: "每日委託", regular: "一般委託" }
const pages: BoardPage[] = ["daily", "regular"]

// 單子輪流微微傾斜（照設計稿）
const tilts = [-2.5, 1.8, -1.2, 2.6, -1.8, 1.2]
const tilt = (index: number) => ({ "--r": `${tilts[index % tilts.length]}deg` }) as CSSProperties

// 空板只剩幾根圖釘
const pins = [
  { left: "16%", top: "14%" },
  { left: "58%", top: "8%" },
  { left: "80%", top: "46%" },
  { left: "30%", top: "62%" },
  { left: "66%", top: "84%" },
]

const SwipeDistance = 40

// 佈告欄：上緣兩張標題紙換頁（目前頁在前）、左右滑也能換；每日頁 3×3、一般頁在軟木裡捲動
export const MissionBoard = ({ board, page, level, onPage, onPick, onPile, onRetry }: Props) => {
  const start = useRef<number | null>(null)
  const swiped = useRef(false)
  const loaded = board !== null && board !== "failed" ? board : null
  const notes: BoardNote[] =
    loaded === null
      ? []
      : page === "daily"
        ? dailyNotes(loaded)
        : regularNotes(loaded).map((mission) => ({ mission, done: false }))
  const counts = loaded === null ? null : pageCounts(loaded)
  const pile = loaded === null || page !== "regular" ? [] : completedRegular(loaded)
  const locked =
    loaded !== null &&
    page === "daily" &&
    notes.length === 0 &&
    loaded.firstDailyLevel !== null &&
    level < loaded.firstDailyLevel

  const go = (next: number) => {
    const target = pages[next]
    if (target !== undefined && target !== page) onPage(target)
  }

  return (
    <section
      className="missions-board"
      aria-label="佈告欄"
      onPointerDown={(event) => {
        start.current = event.clientX
        swiped.current = false
      }}
      onPointerUp={(event) => {
        if (start.current === null) return
        const distance = event.clientX - start.current
        start.current = null
        if (Math.abs(distance) <= SwipeDistance) return
        swiped.current = true
        go(pages.indexOf(page) + (distance < 0 ? 1 : -1))
        // 這次放開後的 click 不算點到單子
        setTimeout(() => {
          swiped.current = false
        }, 0)
      }}
    >
      <div className="missions-board__cork" aria-hidden="true" />
      <div className={`missions-board__page missions-board__page--${page}`} key={page}>
        <div
          className={`missions-board__notes${page === "regular" ? " missions-board__notes--scroll" : ""}`}
          data-testid="board-notes"
          data-scroll={page === "regular"}
        >
          {board === null &&
            Array.from({ length: 6 }, (_, index) => (
              <span
                key={index}
                className="missions-slip missions-slip--wait"
                style={tilt(index)}
                data-testid="board-blank"
                aria-hidden="true"
              />
            ))}
          {notes.map(({ mission, done }, index) => (
            <button
              key={mission.id}
              type="button"
              className={`missions-slip${mission.daily ? " missions-slip--daily" : ""}${done ? " missions-slip--done" : ""}`}
              style={tilt(index)}
              data-testid="board-slip"
              data-done={done}
              aria-label={`${done ? "今天已完成，" : ""}${displayTitle(mission)}，Lv ${formatNumber(mission.level)}`}
              onClick={() => {
                if (!swiped.current) onPick(mission.id)
              }}
            >
              <span className="missions-slip__pin" aria-hidden="true" />
              <b data-testid="slip-title">{displayTitle(mission)}</b>
              <small>{mission.giver}</small>
              <span className="missions-slip__foot">
                <em>{`Lv ${formatNumber(mission.level)}`}</em>
                <i>{kindLabel(mission)}</i>
              </span>
              {done && (
                <span className="missions-slip__done" aria-hidden="true">
                  完
                </span>
              )}
            </button>
          ))}
        </div>
        {loaded !== null && notes.length === 0 && (
          <div className="missions-board__pins" aria-hidden="true">
            {pins.map((pin, index) => (
              <i key={index} style={pin} />
            ))}
          </div>
        )}
        {locked && (
          <div
            className="missions-board__lock"
            role="img"
            aria-label={`每日委託 Lv ${loaded.firstDailyLevel} 開放`}
          >
            {`Lv ${loaded.firstDailyLevel}`}
          </div>
        )}
        {board === "failed" && (
          <div className="missions-board__failed">
            <KeeperSay line={{ keeper: "lise", text: boardLoadFailedLine, failed: true }} />
            <button type="button" className="seal missions-board__retry" onClick={onRetry}>
              <span>
                再試
                <br />
                一次
              </span>
            </button>
          </div>
        )}
        {pile.length > 0 && (
          <button
            type="button"
            className="missions-pile"
            aria-label={`完成的委託 ${pile.length} 件`}
            onClick={onPile}
          >
            <span className="missions-pile__pin" aria-hidden="true" />
            <span className="missions-pile__stamp" aria-hidden="true">
              完
            </span>
            <b>{formatNumber(pile.length)}</b>
          </button>
        )}
      </div>
      <div className="missions-board__tabs" role="group" aria-label="佈告欄頁">
        {pages.map((name) => {
          const count = counts === null || name === page ? 0 : counts[name]
          return (
            <button
              key={name}
              type="button"
              className={`missions-tab missions-tab--${name}${name === page ? " missions-tab--on" : ""}`}
              aria-pressed={name === page}
              aria-label={`${pageNames[name]}${count > 0 ? `，${count} 張可接` : ""}`}
              onClick={() => onPage(name)}
            >
              <span className="missions-tab__paper" aria-hidden="true" />
              <span className="missions-tab__pin" aria-hidden="true" />
              {pageNames[name]}
              <span className="missions-tab__pin" aria-hidden="true" />
              {count > 0 && (
                <span className="missions-tab__count" aria-hidden="true">
                  {count}
                </span>
              )}
            </button>
          )
        })}
      </div>
    </section>
  )
}
