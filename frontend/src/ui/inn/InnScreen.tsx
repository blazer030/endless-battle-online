import { useEffect, useRef, useState, type CSSProperties } from "react"
import { ApiError, type ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { formatNumber } from "../../game/format"
import {
  activeRoom,
  innCounterTop,
  innkeeperLine,
  InnVo,
  replaceNotice,
  roomEffect,
  stayRemainingClock,
  stayLength,
  stayProgress,
  type InnkeeperEvent,
  type InnRoom,
} from "../../game/inn"
import { canAfford } from "../../game/inventory"
import { BuffTickMilliseconds } from "../../motion/timings"
import { ContentImage } from "../ContentImage"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import { InnAutoRest } from "./InnAutoRest"
import innkeeper from "../../assets/inn/innkeeper.webp"
import purse from "../../assets/town/purse.webp"
import "./InnScreen.css"

export type InnApi = Pick<ApiClient, "getInn" | "bookInn" | "updateSettings" | "getMyCharacter">

type Tab = "rooms" | "auto"

type Props = {
  api: InnApi
  character: CharacterVo
  // 伺服器確認過的角色（入住、存門檻、重讀）交給外層保管，回城鎮時顯示
  onCharacterChange: (character: CharacterVo) => void
  onBack: () => void
}

const RoomArt = ({ room, className }: { room: InnRoom; className: string }) => (
  <ContentImage className={className} category="room-backgrounds" assetKey={room.id} alt="" />
)

const RoomBadge = ({ room, className }: { room: InnRoom; className: string }) => (
  <ContentImage className={className} category="room-badges" assetKey={room.id} alt="" />
)

// 旅館「暖爐亭」（設計板 InnFit）：上半部大廳與老闆娘瑪莎，下半部木台上是目前入住、客房與自動住宿
export const InnScreen = ({ api, character, onCharacterChange, onBack }: Props) => {
  const stage = useStage()
  const [inn, setInn] = useState<InnVo | null>(null)
  const [tab, setTab] = useState<Tab>("rooms")
  const [selectedId, setSelectedId] = useState<string | null>(null)
  // 詳情滑出去時沿用最後一間的內容，才不會在收起途中變空白
  const [shownId, setShownId] = useState<string | null>(null)
  const [event, setEvent] = useState<InnkeeperEvent | null>(null)
  const [booking, setBooking] = useState(false)
  const [loadFailed, setLoadFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [leaving, setLeaving] = useState(false)
  const [now, setNow] = useState(() => new Date())
  const alive = useRef(true)

  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
    }
  }, [])

  useEffect(() => {
    let isCurrent = true
    api.getInn().then(
      (loaded) => {
        if (!isCurrent) return
        setInn(loaded)
        setLoadFailed(false)
      },
      () => {
        if (!isCurrent) return
        setLoadFailed(true)
        setEvent({ kind: "loadFailed" })
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt])

  // 剩餘時間定時重算，到期後橫幅換成沒有入住
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), BuffTickMilliseconds)
    return () => clearInterval(timer)
  }, [])

  const select = (roomId: string) => {
    setEvent(null)
    setSelectedId(roomId)
    setShownId(roomId)
  }

  const book = async (room: InnRoom) => {
    if (inn === null) return
    setBooking(true)
    setEvent(null)
    try {
      const booked = await api.bookInn(room.id)
      if (!alive.current) return
      onCharacterChange(booked.character)
      setInn(InnVo.create({ rooms: inn.rooms, lodging: booked.lodging, restFee: inn.restFee }))
      setSelectedId(null)
      setEvent({ kind: "booked", roomName: room.name })
    } catch (error) {
      if (!alive.current) return
      if (error instanceof ApiError && error.reasons.includes("notEnoughMoney")) {
        setEvent({ kind: "notEnoughMoney" })
        // 錢可能在別處花掉了：重讀角色換成最新的金錢，讀不到就維持原樣
        try {
          const latest = await api.getMyCharacter()
          if (alive.current && latest !== null) onCharacterChange(latest)
        } catch {
          // 維持原本的金錢
        }
      } else {
        setEvent({ kind: "bookFailed" })
      }
    } finally {
      if (alive.current) setBooking(false)
    }
  }

  const stay = inn === null ? undefined : activeRoom(inn.lodging, inn.rooms, now)
  const lodging = stay === undefined ? null : (inn?.lodging ?? null)
  const shown = inn?.rooms.find((room) => room.id === shownId)
  const notice =
    inn === null || shown === undefined
      ? null
      : replaceNotice(inn.lodging, inn.rooms, shown.id, now)
  const affordable = shown !== undefined && canAfford(character.money, shown.price, 1)
  const sheetOpen = selectedId !== null && shown !== undefined

  return (
    <div className="inn" style={{ "--pt": `${innCounterTop(stage.height)}px` } as CSSProperties}>
      <div className="inn__lobby" aria-hidden="true" />
      <img className="inn__innkeeper" src={innkeeper} alt="老闆娘 瑪莎" />
      <button
        type="button"
        className="inn__back"
        onClick={() => {
          setLeaving(true)
          onBack()
        }}
      >
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24">
          <path d="M14.5 5.5c-2.2 2.3-4.4 4.4-6.5 6.4 2 2.1 4.2 4.3 6.6 6.6" />
        </svg>
        市集
      </button>
      <p className="inn__bubble" data-testid="innkeeper-line" aria-live="polite">
        <span key={`${event?.kind ?? "greet"}-${stay !== undefined}`}>
          {innkeeperLine(event, stay !== undefined)}
        </span>
      </p>

      <section className="inn__counter" aria-label="旅館">
        <header className="inn__header">
          <span className="inn__name">
            <b>暖爐亭</b>
            <small>老闆娘・瑪莎</small>
          </span>
          <span className="inn__purse" aria-label="持有金錢">
            <img src={purse} alt="" />
            <b>{formatNumber(character.money)}</b>
          </span>
        </header>
        <i className="inn__rule" aria-hidden="true" />

        {inn === null && loadFailed && (
          <div className="inn__retry">
            <Seal
              className="inn__retry-seal"
              label="再試一次"
              onClick={() => {
                setEvent(null)
                setLoadFailed(false)
                setAttempt((count) => count + 1)
              }}
            />
          </div>
        )}

        {inn !== null && (
          <>
            <section
              className={`inn-now${stay === undefined ? " inn-now--none" : ""}`}
              aria-label="目前入住"
            >
              {stay === undefined || lodging === null ? (
                "目前沒有住宿加成"
              ) : (
                <>
                  <RoomArt room={stay} className="inn-now__art" />
                  <RoomBadge room={stay} className="inn-now__badge" />
                  <div className="inn-now__text">
                    <span className="inn-now__title">
                      <b>{stay.name}</b>
                      <em>{`剩 ${stayRemainingClock(lodging, now)}`}</em>
                    </span>
                    <span className="inn-now__effect">{roomEffect(stay)}</span>
                    <span className="inn-now__bar">
                      <i style={{ width: `${Math.round(stayProgress(lodging, now) * 100)}%` }} />
                    </span>
                  </div>
                </>
              )}
            </section>

            <div className="inn-tabs" role="tablist" aria-label="旅館分頁">
              {(
                [
                  ["rooms", "客房"],
                  ["auto", "自動住宿"],
                ] as const
              ).map(([id, label]) => (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  className="inn-tabs__tab"
                  aria-selected={tab === id}
                  onClick={() => {
                    setEvent(null)
                    setSelectedId(null)
                    setTab(id)
                  }}
                >
                  {label}
                </button>
              ))}
            </div>

            <div
              className="inn__body"
              role="tabpanel"
              aria-label={tab === "rooms" ? "客房" : "自動住宿"}
            >
              {tab === "rooms" && (
                <ul className="inn-rooms" aria-label="客房">
                  {inn.rooms.map((room) => {
                    const current = stay?.id === room.id
                    return (
                      <li key={room.id}>
                        <button
                          type="button"
                          className={`inn-room${current ? " inn-room--current" : ""}`}
                          aria-label={`${room.name}，${roomEffect(room)}，${formatNumber(room.price)} 金錢${current ? "，入住中" : ""}`}
                          onClick={() => select(room.id)}
                        >
                          <RoomArt room={room} className="inn-room__art" />
                          <RoomBadge room={room} className="inn-room__badge" />
                          {current && <span className="inn-room__current">入住中</span>}
                          <span className="inn-room__text">
                            <b>{room.name}</b>
                            <span className="inn-room__effect">{roomEffect(room)}</span>
                            <span className="inn-room__foot">
                              <span>{stayLength(room.seconds)}</span>
                              <span className="inn-room__price">
                                <img src={purse} alt="" />
                                {formatNumber(room.price)}
                              </span>
                            </span>
                          </span>
                        </button>
                      </li>
                    )
                  })}
                </ul>
              )}
              {/* 一直掛著、切分頁只隱藏：切走時還在送的存檔結果才接得到 */}
              <div hidden={tab !== "auto"}>
                <InnAutoRest
                  api={api}
                  character={character}
                  restFee={inn.restFee}
                  onCharacterChange={onCharacterChange}
                  onStep={() => setEvent(null)}
                  onSaveFailed={() => setEvent({ kind: "thresholdFailed" })}
                  leaving={leaving}
                />
              </div>
            </div>
          </>
        )}
      </section>

      {sheetOpen && (
        <div className="inn-sheet__mask" aria-hidden="true" onClick={() => setSelectedId(null)} />
      )}
      <section
        className={`inn-sheet${sheetOpen ? " inn-sheet--open" : ""}`}
        role="dialog"
        aria-label="入住"
        aria-hidden={!sheetOpen}
        inert={!sheetOpen}
      >
        {shown !== undefined && (
          <>
            <button
              type="button"
              className="inn-sheet__close"
              aria-label="關閉"
              onClick={() => setSelectedId(null)}
            />
            <div className="inn-sheet__head">
              <RoomBadge room={shown} className="inn-sheet__badge" />
              <span className="inn-sheet__name">
                <b>{shown.name}</b>
                <span>{roomEffect(shown)}</span>
              </span>
            </div>
            {notice !== null && (
              <p
                className={`inn-sheet__notice${notice.replacing ? " inn-sheet__notice--warn" : ""}`}
              >
                {notice.text}
              </p>
            )}
            <div className="inn-sheet__buy">
              <span className="inn-sheet__cost" data-testid="inn-cost" data-short={!affordable}>
                花費
                <img src={purse} alt="" />
                <b>{formatNumber(shown.price)}</b>
              </span>
              <Seal
                className="inn-sheet__seal"
                label="入住"
                disabled={booking || !affordable}
                onClick={() => void book(shown)}
              />
            </div>
          </>
        )}
      </section>
    </div>
  )
}
