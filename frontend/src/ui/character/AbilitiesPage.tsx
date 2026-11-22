import { useEffect, useState } from "react"
import { abilityRows, combatRows } from "../../game/characterScreen"
import { bonusRows } from "../../game/town"
import { CountdownTickMilliseconds } from "../../motion/timings"
import { BonusBadgeImage } from "../town/BonusBadgeImage"
import { bonusBadgeClass } from "../town/bonusBadge"
import type { CharacterData } from "./CharacterScreen"
import "../town/TownBonuses.css"

type Props = {
  data: CharacterData
  onAskSignOut: () => void
}

// 能力頁：七項能力（裸點＋裝備加成）、戰鬥數值、密藥與住宿（同城鎮格式、每秒倒數）、帳號與登出
export const AbilitiesPage = ({ data, onAskSignOut }: Props) => {
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), CountdownTickMilliseconds)
    return () => clearInterval(timer)
  }, [])
  const bonuses = bonusRows(data.buffs, data.inn.lodging, data.inn.rooms, now)

  return (
    <div role="tabpanel" aria-label="能力" className="character-abilities">
      <section className="character-section">
        <h3 className="character-section__title">能力</h3>
        <div className="character-stats">
          {abilityRows(data.character).map((row) => (
            <div key={row.label} className="character-stat">
              <span>{row.label}</span>
              {row.bonus !== null && <em className="character-stat__bonus">{row.bonus}</em>}
              <b>{row.value}</b>
            </div>
          ))}
        </div>
      </section>
      <section className="character-section">
        <h3 className="character-section__title">戰鬥數值</h3>
        <div className="character-combats">
          {combatRows(data.character).map((row) => (
            <div key={row.label} className="character-combat">
              {row.label}
              <b>{row.value}</b>
            </div>
          ))}
        </div>
      </section>
      <section className="character-section">
        <h3 className="character-section__title">密藥與住宿</h3>
        {bonuses.length === 0 ? (
          <p className="character-bonus character-bonus--none">沒有生效中的密藥或住宿</p>
        ) : (
          <ul className="character-bonuses" aria-label="密藥與住宿">
            {bonuses.map((row) => (
              <li
                key={row.key}
                className="character-bonus"
                {...(row.endingSoon ? { "data-ending-soon": "true" } : {})}
              >
                <span className={`${bonusBadgeClass(row)} character-bonus__disc`}>
                  <BonusBadgeImage row={row} />
                </span>
                <span className="character-bonus__name">{row.name}</span>
                {row.effect !== null && (
                  <span className="character-bonus__effect">{row.effect}</span>
                )}
                <span className="character-bonus__left">{row.remaining}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
      <div className="character-sign-out">
        <span>{data.account.username}</span>
        <button type="button" onClick={onAskSignOut}>
          登出
        </button>
      </div>
    </div>
  )
}
