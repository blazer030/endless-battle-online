import type { CSSProperties } from "react"
import { handSlip, type Mission } from "../../game/missions"

// 手邊三張輪流微微傾斜（照設計稿）
const tilts = [-3, 1.5, 3.5]
const tilt = (index: number) => ({ "--r": `${tilts[index % tilts.length]}deg` }) as CSSProperties

type Props = {
  active: readonly Mission[] | null
  limit: number
  onPick: (missionId: string) => void
}

// 櫃檯上手邊的委託：名稱、第一項目標與進度、其餘「＋N 項」、進度條；全部達成蓋「達成／可交」；空位是虛線框
export const MissionHand = ({ active, limit, onPick }: Props) => (
  <div className="missions-hand" role="group" aria-label="手邊的委託">
    {Array.from({ length: limit }, (_, index) => {
      const mission = active?.[index]
      if (mission === undefined)
        return (
          <span
            key={index}
            className="missions-hand__empty"
            style={tilt(index)}
            data-testid="hand-empty"
            aria-hidden="true"
          />
        )
      const slip = handSlip(mission)
      const percent = Math.round(slip.ratio * 100)
      return (
        <button
          key={mission.id}
          type="button"
          className="missions-held"
          style={tilt(index)}
          data-daily={slip.daily}
          aria-label={`${slip.title}${slip.stamp === null ? "" : `，${slip.stamp}`}`}
          onClick={() => onPick(mission.id)}
        >
          <b data-testid="hand-title">{slip.title}</b>
          {slip.first !== null && (
            <>
              <span className="missions-held__goal">{slip.first.text}</span>
              <span className="missions-held__count">
                <em data-met={slip.first.met} data-short={slip.first.short}>
                  {slip.first.amount}
                </em>
                {slip.more > 0 && <small>{`＋${slip.more} 項`}</small>}
              </span>
            </>
          )}
          <span
            className="missions-bar"
            role="progressbar"
            aria-label="進度"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent}
          >
            <i style={{ width: `${percent}%` }} />
          </span>
          {slip.stamp !== null && (
            <span className="missions-held__stamp" aria-hidden="true">
              {slip.stamp}
            </span>
          )}
        </button>
      )
    })}
  </div>
)
