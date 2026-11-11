import { useEffect, useId, useState } from "react"
import type { ApiClient } from "../api/client"
import { formatNumber } from "../game/format"
import { lodgingLine, replaceHint, roomEffect, stayLength } from "../game/inn"
import { canAfford } from "../game/inventory"
import { parseSettings, settingsForm } from "../game/settings"
import type { InnView } from "../game/types"
import type { CharacterVo } from "../game/character"
import { BuffTickMilliseconds } from "../motion/timings"
import type { Mutate } from "./bagWidgets"

export type InnApi = Pick<ApiClient, "getInn" | "bookInn" | "updateSettings">

type Props = {
  api: InnApi
  character: CharacterVo
  busy: boolean
  mutate: Mutate
}

export function InnPage({ api, character, busy, mutate }: Props) {
  const roomsId = useId()
  const autoId = useId()
  const [inn, setInn] = useState<InnView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [attempt, setAttempt] = useState(0)
  // 剩餘時間定時重算，到期後加成列跟著消失
  const [now, setNow] = useState(() => new Date())
  const [error, setError] = useState<string | null>(null)
  const [restHp, setRestHp] = useState(String(character.settings.restHealthPointsPercent))
  const [restSp, setRestSp] = useState(String(character.settings.restManaPointsPercent))
  const [settingsError, setSettingsError] = useState<string | null>(null)

  useEffect(() => {
    let isCurrent = true
    api.getInn().then(
      (loaded) => {
        if (!isCurrent) return
        setInn(loaded)
        setLoadError(false)
      },
      () => {
        if (isCurrent) setLoadError(true)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt])

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), BuffTickMilliseconds)
    return () => clearInterval(timer)
  }, [])

  if (loadError)
    return (
      <section className="bag__card">
        <p className="error" role="alert">
          無法載入旅館。
        </p>
        <div className="actions">
          <button type="button" onClick={() => setAttempt((count) => count + 1)}>
            重試
          </button>
        </div>
      </section>
    )
  if (inn === null) return <p className="bag__empty">載入中…</p>

  const book = async (roomId: string) => {
    setError(null)
    let lodging = inn.lodging
    const message = await mutate(async () => {
      const booked = await api.bookInn(roomId)
      lodging = booked.lodging
      return { character: booked.character }
    })
    if (message !== null) {
      setError(message)
      return
    }
    setInn({ ...inn, lodging })
    // 入住前的結算可能升級，房價跟著變；重新讀取失敗就先保留剛入住的結果
    api.getInn().then(setInn, () => {})
  }

  const saveThresholds = async () => {
    const parsed = parseSettings({
      ...settingsForm(character),
      restHealthPointsPercent: restHp,
      restManaPointsPercent: restSp,
    })
    if (!parsed.ok) {
      setSettingsError(parsed.error)
      return
    }
    setSettingsError(null)
    const message = await mutate(async () => ({
      character: await api.updateSettings(parsed.command),
    }))
    if (message !== null) setSettingsError(message)
  }

  const current = lodgingLine(inn.lodging, inn.rooms, now)

  return (
    <section className="bag__card">
      <h3 className="bag__heading">目前入住</h3>
      <p className="bag__usage">{current ?? "目前沒有住宿加成"}</p>
      <h3 id={roomsId} className="bag__heading">
        客房
      </h3>
      <p className="bag__usage">{`金錢 ${formatNumber(character.money)}`}</p>
      <ul className="bag__list" aria-labelledby={roomsId}>
        {inn.rooms.map((room) => {
          const hint = replaceHint(inn.lodging, inn.rooms, room.id, now)
          return (
            <li key={room.id} className="bag__item">
              <span className="bag__item-name">{`${room.name}・${roomEffect(room)}・${stayLength(room.seconds)} ${formatNumber(room.price)} 金錢`}</span>
              {hint !== null && <p className="bag__detail">{hint}</p>}
              <div className="bag__buy">
                <button
                  type="button"
                  className="bag__action"
                  disabled={busy || !canAfford(character.money, room.price, 1)}
                  onClick={() => void book(room.id)}
                >
                  入住
                </button>
              </div>
            </li>
          )
        })}
      </ul>
      {error !== null && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <h3 id={autoId} className="bag__heading">
        自動住宿
      </h3>
      <p className="bag__usage">
        HP低於{" "}
        <input
          className="bag__percent"
          type="text"
          aria-label="住宿 HP 門檻"
          maxLength={3}
          value={restHp}
          onChange={(event) => setRestHp(event.target.value)}
        />{" "}
        %，SP低於{" "}
        <input
          className="bag__percent"
          type="text"
          aria-label="住宿 SP 門檻"
          maxLength={3}
          value={restSp}
          onChange={(event) => setRestSp(event.target.value)}
        />{" "}
        %自動住宿(-1表示略過條件)
      </p>
      {settingsError !== null && (
        <p className="error" role="alert">
          {settingsError}
        </p>
      )}
      <div className="actions">
        <button type="button" disabled={busy} onClick={() => void saveThresholds()}>
          儲存
        </button>
      </div>
    </section>
  )
}
