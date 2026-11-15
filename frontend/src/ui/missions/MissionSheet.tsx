import type { ReactNode } from "react"
import { formatNumber } from "../../game/format"
import { displayTitle, rewardRows, type Mission } from "../../game/missions"
import type { ItemCategory } from "../../game/inventory"
import { ContentImage } from "../ContentImage"
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

type SheetProps = { mission: Mission; completed: boolean; onClose: () => void }

// 委託單：完成的委託蓋「完」章，紙上是委託者的完成台詞＋報酬
export const MissionSheet = ({ mission, completed, onClose }: SheetProps) => (
  <SheetFrame label={displayTitle(mission)} daily={mission.daily} onClose={onClose}>
    {completed && (
      <span className="missions-sheet__stamp" aria-hidden="true">
        完
      </span>
    )}
    <div className="missions-sheet__scroll">
      <h3>{displayTitle(mission)}</h3>
      <p className="missions-sheet__giver">{`委託者\u3000${mission.giver}`}</p>
      {completed && mission.completionMessage !== null && (
        <div className="missions-sheet__quote">
          <small>{mission.giver}</small>
          <p>{mission.completionMessage}</p>
        </div>
      )}
      <RewardList mission={mission} />
    </div>
  </SheetFrame>
)

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
