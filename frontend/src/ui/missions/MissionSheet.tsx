import type { ReactNode } from "react"
import { formatNumber } from "../../game/format"
import {
  completionRows,
  displayTitle,
  objectiveRows,
  progressRatio,
  requirementText,
  rewardRows,
  type LiseLine,
  type Mission,
  type MissionCompletionVo,
} from "../../game/missions"
import type { ItemCategory } from "../../game/inventory"
import { ContentImage } from "../ContentImage"
import { KeeperSay } from "../companions/KeeperSay"
import { categoryPlaceholders } from "../itemPlaceholders"
import purse from "../../assets/town/purse.webp"

type FrameProps = {
  label: string
  daily: boolean
  closeDisabled?: boolean
  onClose: () => void
  children: ReactNode
}

// 拉近的羊皮紙：頂上圖釘（每日紅、一般黃銅）、右上關閉；點外面也收起
const SheetFrame = ({ label, daily, closeDisabled = false, onClose, children }: FrameProps) => (
  <>
    <div
      className="missions-sheet__mask"
      aria-hidden="true"
      onClick={() => {
        if (!closeDisabled) onClose()
      }}
    />
    <section
      className={`missions-sheet${daily ? " missions-sheet--daily" : ""}`}
      role="dialog"
      aria-modal="true"
      aria-label={label}
    >
      <button
        type="button"
        className="missions-sheet__close"
        aria-label="關閉"
        disabled={closeDisabled}
        onClick={onClose}
      />
      {children}
    </section>
  </>
)

export const ItemPicture = ({ itemId, category }: { itemId: string; category: ItemCategory }) => (
  <ContentImage
    className="missions-sheet__item"
    category="items"
    assetKey={itemId}
    alt=""
    placeholder={categoryPlaceholders[category]}
  />
)

export const RewardList = ({ mission }: { mission: Mission }) => (
  <>
    <h4>委託報酬</h4>
    <div className="missions-sheet__rewards">
      {rewardRows(mission.rewards).map((row, index) => (
        <span key={index}>
          {row.icon === "coin" && <img className="missions-sheet__coin" src={purse} alt="" />}
          {row.category !== null && row.itemId !== null && (
            <ItemPicture itemId={row.itemId} category={row.category} />
          )}
          {row.text}
        </span>
      ))}
    </div>
  </>
)

// open＝可接、active＝手邊的、done＝完成的
export type SheetView = "open" | "active" | "done"

// 印章那排：放棄（再確認）、停用說明、紅印章；完成檢視沒有這排
export type SheetActions = {
  seal: string
  sealDisabled: boolean
  note: string | null
  busy: boolean
  asking: boolean
  onSeal: () => void
  onAsk: () => void
  onCancelAsk: () => void
  onAbandon: () => void
}

type SheetProps = {
  mission: Mission
  view: SheetView
  resetText: string
  // 剛完成的這一筆：委託者的話＋這次拿到
  result: MissionCompletionVo | null
  say: LiseLine | null
  actions: SheetActions
  onClose: () => void
}

// 委託單：說明、條件、目標、報酬在紙內捲動，莉瑟的話與印章那排固定在下面；完成的蓋「完」章
export const MissionSheet = ({
  mission,
  view,
  resetText,
  result,
  say,
  actions,
  onClose,
}: SheetProps) => {
  const accepted = view === "active"
  const percent = Math.round(progressRatio(mission) * 100)
  const quote = result === null ? mission.completionMessage : result.completionMessage || null
  return (
    <SheetFrame
      label={displayTitle(mission)}
      daily={mission.daily}
      closeDisabled={actions.busy}
      onClose={onClose}
    >
      {view === "done" && (
        <span
          className={`missions-sheet__stamp${result === null ? "" : " missions-sheet__stamp--fresh"}`}
          aria-hidden="true"
        >
          完
        </span>
      )}
      <div className="missions-sheet__scroll">
        <h3>{displayTitle(mission)}</h3>
        <p className="missions-sheet__giver">{`委託者\u3000${mission.giver}`}</p>
        {view !== "done" && (
          <>
            <p className="missions-sheet__description">{mission.description}</p>
            {view === "open" && (
              <p className="missions-sheet__requirements">{requirementText(mission)}</p>
            )}
            <h4>委託內容</h4>
            {objectiveRows(mission, accepted).map((row, index) => (
              <div
                key={index}
                className="missions-goal"
                data-testid={`goal-${index}`}
                data-met={row.met}
                data-short={row.short}
              >
                <span className="missions-goal__text">
                  {row.category !== null && row.itemId !== null && (
                    <ItemPicture itemId={row.itemId} category={row.category} />
                  )}
                  <span>
                    {row.text}
                    {row.sub !== null && <small>{row.sub}</small>}
                  </span>
                </span>
                <em className={/^[\d×,\s/]+$/.test(row.amount) ? undefined : "missions-goal__word"}>
                  {row.amount}
                </em>
              </div>
            ))}
            {accepted && (
              <span
                className="missions-bar"
                role="progressbar"
                aria-label="委託進度"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={percent}
              >
                <i style={{ width: `${percent}%` }} />
              </span>
            )}
          </>
        )}
        {view === "done" && quote !== null && (
          <div className="missions-sheet__quote" data-testid="giver-quote">
            <small>{mission.giver}</small>
            <p>{quote}</p>
          </div>
        )}
        {result === null ? (
          <RewardList mission={mission} />
        ) : (
          <>
            <h4 id={`got-${mission.id}`}>這次拿到</h4>
            <ul className="missions-sheet__got" aria-labelledby={`got-${mission.id}`}>
              {completionRows(result).map((row, index) => (
                <li key={index} data-kind={row.kind}>
                  {row.icon === "coin" && (
                    <img className="missions-sheet__coin" src={purse} alt="" />
                  )}
                  {row.category !== null && row.itemId !== null && (
                    <ItemPicture itemId={row.itemId} category={row.category} />
                  )}
                  {row.text}
                </li>
              ))}
            </ul>
          </>
        )}
        {mission.daily && view !== "done" && (
          <p className="missions-sheet__reset">{`每日委託・${resetText}`}</p>
        )}
      </div>
      {say !== null && (
        <div data-testid="lise-say" data-failed={say.failed}>
          <KeeperSay line={{ keeper: "lise", ...say }} />
        </div>
      )}
      {view !== "done" && (
        <div className="missions-sheet__actions">
          {view === "active" &&
            (actions.asking ? (
              <>
                <button
                  type="button"
                  className="missions-sheet__ink missions-sheet__ink--danger"
                  disabled={actions.busy}
                  onClick={actions.onAbandon}
                >
                  確定放棄
                </button>
                <button
                  type="button"
                  className="missions-sheet__ink"
                  disabled={actions.busy}
                  onClick={actions.onCancelAsk}
                >
                  取消
                </button>
              </>
            ) : (
              <button
                type="button"
                className="missions-sheet__ink"
                disabled={actions.busy}
                onClick={actions.onAsk}
              >
                放棄
              </button>
            ))}
          {actions.note !== null && <span className="missions-sheet__note">{actions.note}</span>}
          <button
            type="button"
            className={`seal missions-sheet__seal${actions.busy ? " missions-sheet__seal--busy" : ""}`}
            aria-busy={actions.busy}
            disabled={actions.busy || actions.sealDisabled}
            onClick={actions.onSeal}
          >
            <span>{actions.seal}</span>
          </button>
        </div>
      )}
    </SheetFrame>
  )
}

type ListProps = { missions: readonly Mission[]; onPick: (id: string) => void; onClose: () => void }

// 完成的委託：一疊蓋了章的單子，新的在上
export const CompletedList = ({ missions, onPick, onClose }: ListProps) => (
  <SheetFrame label="完成的委託" daily={false} onClose={onClose}>
    <div className="missions-sheet__scroll">
      <h3>
        完成的委託<small>{formatNumber(missions.length)}</small>
      </h3>
      {missions.map((mission) => (
        <button
          key={mission.id}
          type="button"
          className="missions-done"
          aria-label={displayTitle(mission)}
          onClick={() => onPick(mission.id)}
        >
          <span className="missions-done__stamp" aria-hidden="true">
            完
          </span>
          <span className="missions-done__text">
            <b>{displayTitle(mission)}</b>
            <small>{mission.giver}</small>
          </span>
          <em>{`Lv ${formatNumber(mission.level)}`}</em>
        </button>
      ))}
    </div>
  </SheetFrame>
)
