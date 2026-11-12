import { useEffect, useRef, useState, type CSSProperties } from "react"
import type { BuffKind, BuffVo } from "../../game/buffs"
import type { InnVo } from "../../game/inn"
import { bonusRows, expiredSince, type BonusRow } from "../../game/town"
import { BuffTickMilliseconds } from "../../motion/timings"
import { ContentImage } from "../ContentImage"
import roomApprenticeLoft from "../../assets/town/room-apprentice-loft.webp"
import roomBulwarkRoom from "../../assets/town/room-bulwark-room.webp"
import roomFightersBunk from "../../assets/town/room-fighters-bunk.webp"
import roomQuietChapel from "../../assets/town/room-quiet-chapel.webp"
import roomStargazerTower from "../../assets/town/room-stargazer-tower.webp"
import roomTrainingChamber from "../../assets/town/room-training-chamber.webp"
import type { TownApi } from "./TownScreen"
import "./TownBonuses.css"

// 房型徽章依加成種類（設計稿 HomeJ 旅館板的對應）
const roomBadges: Record<BuffKind, string> = {
  Experience: roomApprenticeLoft,
  JobExperience: roomTrainingChamber,
  Attack: roomFightersBunk,
  Magic: roomStargazerTower,
  Defense: roomBulwarkRoom,
  MagicDefense: roomQuietChapel,
}

const kindClass: Record<BuffKind, string> = {
  Experience: "experience",
  JobExperience: "job-experience",
  Attack: "attack",
  Magic: "magic",
  Defense: "defense",
  MagicDefense: "magic-defense",
}

type Loaded = { buffs: readonly BuffVo[]; inn: InnVo; at: Date }

const badgeClass = (row: BonusRow) =>
  `town-bonus__disc town-bonus__disc--${row.isLodging ? "lodging" : kindClass[row.kind]}`

const BadgeImage = ({ row }: { row: BonusRow }) =>
  row.isLodging ? (
    <img src={roomBadges[row.kind]} alt="" />
  ) : (
    <ContentImage category="items" assetKey={row.itemId ?? "unknown"} alt="" />
  )

type Props = {
  api: TownApi
  open: boolean
  onToggle: () => void
  onClose: () => void
}

// 加成列：住宿與密藥的圓章，角落是剩餘時間；點開看清單。剩餘時間用本機時鐘倒數，有一項到期就重新讀一次
export const TownBonuses = ({ api, open, onToggle, onClose }: Props) => {
  const [loaded, setLoaded] = useState<Loaded | null>(null)
  const [version, setVersion] = useState(0)
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    let isCurrent = true
    Promise.all([api.getBuffs(), api.getInn()]).then(
      ([buffs, inn]) => {
        if (isCurrent) setLoaded({ buffs, inn, at: new Date() })
      },
      // 讀不到就不顯示加成列；城鎮不顯示連線錯誤
      () => {
        if (isCurrent) setLoaded(null)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, version])

  // 倒數的計時器讀最新一次讀到的資料
  const latest = useRef(loaded)
  useEffect(() => {
    latest.current = loaded
  })

  useEffect(() => {
    const timer = setInterval(() => {
      const current = new Date()
      setNow(current)
      const data = latest.current
      // 讀進來時還有效、現在到期了：重新讀一次（讀到後 at 換新，不會重複觸發）
      if (data !== null && expiredSince(data.buffs, data.inn.lodging, data.at, current))
        setVersion((value) => value + 1)
    }, BuffTickMilliseconds)
    return () => clearInterval(timer)
  }, [])

  if (loaded === null) return null
  const rows = bonusRows(loaded.buffs, loaded.inn.lodging, loaded.inn.rooms, now)
  if (rows.length === 0) return null

  const lodgingCount = rows.filter((row) => row.isLodging).length
  const elixirCount = rows.length - lodgingCount
  const countText = `${lodgingCount > 0 ? `住宿 ${lodgingCount}・` : ""}密藥 ${elixirCount}`

  return (
    <>
      <div className="town-bonuses">
        <ul className="town-bonuses__row" aria-label="加成">
          {rows.map((row) => (
            <li key={row.key}>
              <button
                type="button"
                className={`${badgeClass(row)} town-bonus__badge`}
                data-ending-soon={row.endingSoon}
                aria-label={`${row.name}，剩 ${row.remaining}`}
                aria-expanded={open}
                onClick={onToggle}
              >
                <BadgeImage row={row} />
                <i>{row.remaining}</i>
              </button>
            </li>
          ))}
        </ul>
        {open && (
          <div className="town-bonus-list" role="dialog" aria-label="生效中的加成">
            <h5>
              加成效果<small>{countText}</small>
            </h5>
            {rows.map((row) => (
              <div key={row.key} className="town-bonus-list__row" data-ending-soon={row.endingSoon}>
                <span className={badgeClass(row)}>
                  <BadgeImage row={row} />
                </span>
                <span className="town-bonus-list__text">
                  <b>{row.name}</b>
                  {row.effect !== null && <small>{row.effect}</small>}
                  <span className="town-bonus-list__bar">
                    <i style={{ width: `${Math.round(row.progress * 100)}%` } as CSSProperties} />
                  </span>
                </span>
                <span className="town-bonus-list__left">{row.remaining}</span>
              </div>
            ))}
          </div>
        )}
      </div>
      {open && (
        <button type="button" className="town-bonus-close" aria-label="關閉" onClick={onClose} />
      )}
    </>
  )
}
