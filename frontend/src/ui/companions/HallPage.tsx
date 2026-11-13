import { useCallback, useEffect, useRef, useState, type CSSProperties } from "react"
import { ApiError } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import {
  canHire,
  companionsCounterTop,
  deployGroups,
  keeperLine,
  shortPrice,
  type DeployGroup,
  type KeeperEvent,
} from "../../game/companions"
import { formatNumber } from "../../game/format"
import type {
  MercenaryCatalogueEntry,
  MercenaryContract,
  MercenaryOverviewVo,
} from "../../game/mercenaries"
import { ContentImage } from "../ContentImage"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import { BackButton } from "./BackButton"
import type { CompanionsApi } from "./CompanionsScreen"
import { CounterHeader, KeeperSay } from "./KeeperSay"
import { RenameField } from "./RenameField"
import purse from "../../assets/town/purse.webp"

type Props = {
  api: CompanionsApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  onBack: () => void
}

const MercenaryNameMaxLength = 8
// 名冊便條輪流傾斜
const NoteTilts = [-3, 2, -1.5, 2.5, -2]
// 勳章所兌換要用的兩種勳章（大陸、王國）
const HonourMedals = ["item-1304", "item-1305"]

type Sheet = { kind: "hire"; entryId: string } | { kind: "place"; pick: number | null }

const gradeOf = (entry: MercenaryCatalogueEntry) => entry.name.replace(/級傭兵$/, "")

const noteLabel = (entry: MercenaryCatalogueEntry, hired: boolean) =>
  `${entry.name}${entry.price === null ? "，勳章所兌換" : `，${formatNumber(entry.price)} 金錢`}${hired ? "，僱用中" : ""}`

// 傭兵所：牆上名冊便條（F～B）＋木台上的契約；詳情與冒險地是從下方滑出的羊皮紙
export const HallPage = ({ api, character, onCharacterChange, onBack }: Props) => {
  const stage = useStage()
  const [view, setView] = useState<MercenaryOverviewVo | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [event, setEvent] = useState<KeeperEvent | null>(null)
  const [busy, setBusy] = useState(false)
  const [renaming, setRenaming] = useState(false)
  const [dismissing, setDismissing] = useState(false)
  // 關上時保留上一張的內容，滑出去時才不會變空白
  const [sheet, setSheet] = useState<Sheet | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)
  const [groups, setGroups] = useState<DeployGroup[] | null>(null)
  const alive = useRef(true)

  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
    }
  }, [])

  useEffect(() => {
    let isCurrent = true
    api.getMercenaries().then(
      (loaded) => {
        if (!isCurrent) return
        setView(loaded)
        setLoadFailed(false)
        setEvent(null)
      },
      () => {
        if (!isCurrent) return
        setLoadFailed(true)
        setEvent({ kind: "hallLoadFailed" })
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt])

  const catalogue = view?.catalogue ?? []
  const contract = view?.contract ?? null
  const contractEntry =
    contract === null ? undefined : catalogue.find((entry) => entry.id === contract.mercenaryId)

  const reloadCharacter = useCallback(async () => {
    try {
      const latest = await api.getMyCharacter()
      if (alive.current && latest !== null) onCharacterChange(latest)
    } catch {
      // 維持原本的金錢
    }
  }, [api, onCharacterChange])

  const fail = (error: unknown) => {
    if (error instanceof ApiError && error.reasons.length > 0) {
      setEvent({
        kind: "mercenaryRejected",
        reason: error.reasons[0]!,
        contractName: contract?.name,
      })
      if (error.reasons.includes("notEnoughMoney")) void reloadCharacter()
    } else setEvent({ kind: "mercenaryFailed" })
  }

  // 同一時間只送一個請求；成功時換成回應裡的名冊與契約並由巴頓說一句
  const run = async (action: () => Promise<MercenaryOverviewVo>, success: KeeperEvent) => {
    setBusy(true)
    setEvent(null)
    try {
      const overview = await action()
      if (!alive.current) return true
      setView(overview)
      setEvent(success)
      return true
    } catch (error) {
      if (alive.current) fail(error)
      return false
    } finally {
      if (alive.current) setBusy(false)
    }
  }

  const hire = async (entry: MercenaryCatalogueEntry) => {
    setBusy(true)
    setEvent(null)
    try {
      const result = await api.hireMercenary(entry.id)
      if (!alive.current) return
      setView(result.mercenaries)
      onCharacterChange(result.character)
      setSheetOpen(false)
      setEvent({ kind: "hired", name: entry.name })
    } catch (error) {
      if (alive.current) fail(error)
    } finally {
      if (alive.current) setBusy(false)
    }
  }

  const openSheet = (next: Sheet) => {
    setEvent(null)
    setRenaming(false)
    setDismissing(false)
    setSheet(next)
    setSheetOpen(true)
  }

  const closeSheet = () => {
    setEvent(null)
    setSheetOpen(false)
  }

  // 冒險地清單每次打開都重讀；目前的冒險地若在清單裡就先選好
  const openPlaces = async (current: MercenaryContract) => {
    setGroups(null)
    openSheet({ kind: "place", pick: null })
    try {
      const loaded = deployGroups(await api.getWorld())
      if (!alive.current) return
      setGroups(loaded)
      const listed = loaded.some((group) =>
        group.places.some((place) => place.id === current.placeId),
      )
      if (listed) setSheet({ kind: "place", pick: current.placeId })
    } catch {
      if (alive.current) setEvent({ kind: "mercenaryFailed" })
    }
  }

  const deploy = async (current: MercenaryContract, placeId: number) => {
    const place = groups
      ?.flatMap((group) => group.places)
      .find((candidate) => candidate.id === placeId)
    if (
      await run(() => api.deployMercenary(placeId), {
        kind: "deployed",
        name: current.name,
        place: place?.name ?? "",
      })
    )
      setSheetOpen(false)
  }

  const retry = () => {
    setEvent(null)
    setLoadFailed(false)
    setAttempt((count) => count + 1)
  }

  const line = event === null ? null : keeperLine(event)
  const sheetEntry =
    sheet?.kind === "hire" ? catalogue.find((entry) => entry.id === sheet.entryId) : undefined
  const pick = sheet?.kind === "place" ? sheet.pick : null

  return (
    <section
      className="companions-page companions-page--hall"
      aria-label="傭兵所"
      style={
        {
          "--pt": `${companionsCounterTop(stage.height)}px`,
          "--h": `${stage.height}px`,
        } as CSSProperties
      }
    >
      <div className="companions-scene companions-scene--hall">
        <div className="hall-sign" aria-hidden="true">
          傭兵名冊
        </div>
        {view !== null && (
          <ul className="hall-roster" aria-label="傭兵名冊">
            {catalogue.map((entry, index) => {
              const hired = contract?.mercenaryId === entry.id
              return (
                <li key={entry.id}>
                  <button
                    type="button"
                    className="hall-note"
                    style={
                      { "--tilt": `${NoteTilts[index % NoteTilts.length]}deg` } as CSSProperties
                    }
                    aria-label={noteLabel(entry, hired)}
                    onClick={() => openSheet({ kind: "hire", entryId: entry.id })}
                  >
                    <i className="hall-note__pin" aria-hidden="true" />
                    <b className="hall-note__grade">{gradeOf(entry)}</b>
                    <span className="hall-note__title">級傭兵</span>
                    {entry.price === null ? (
                      <span className="hall-note__medals">
                        {HonourMedals.map((medal) => (
                          <ContentImage key={medal} category="items" assetKey={medal} alt="" />
                        ))}
                      </span>
                    ) : (
                      <span className="hall-note__price" data-short={character.money < entry.price}>
                        <img src={purse} alt="" />
                        {shortPrice(entry.price)}
                      </span>
                    )}
                    {hired && <span className="hall-note__stamp">僱用中</span>}
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>
      <BackButton label="同伴" onClick={onBack} />
      <section className="inn__counter" aria-label="櫃檯">
        <CounterHeader
          title={
            <>
              <b>傭兵所</b>
              <small>{view === null ? "" : `契約 ${contract === null ? 0 : 1} / 1`}</small>
            </>
          }
          line={sheetOpen ? null : line}
          money={character.money}
        />
        <i className="inn__rule" aria-hidden="true" />
        {loadFailed ? (
          <div className="companions-retry">
            <Seal className="companions-retry__seal" label="再試一次" onClick={retry} />
          </div>
        ) : (
          view !== null && (
            <div className="companions-body">
              {contract === null ? (
                <div className="inn-now inn-now--none">目前沒有僱用傭兵</div>
              ) : (
                <section className="hall-contract" aria-label="契約">
                  <span
                    className={`hall-contract__stamp${contract.fighting ? "" : " hall-contract__stamp--resting"}`}
                  >
                    {contract.fighting ? "戰鬥中" : "休息中"}
                  </span>
                  <span className="hall-contract__label">傭兵契約</span>
                  <div className="companions-head">
                    {renaming ? (
                      <RenameField
                        label="傭兵名字"
                        initial={contract.name}
                        maxLength={MercenaryNameMaxLength}
                        busy={busy}
                        onCancel={() => setRenaming(false)}
                        onConfirm={async (name) => {
                          if (
                            await run(() => api.renameMercenary(name), {
                              kind: "mercenaryRenamed",
                              name,
                            })
                          )
                            setRenaming(false)
                        }}
                      />
                    ) : (
                      <>
                        <b className="companions-head__name">{contract.name}</b>
                        <button
                          type="button"
                          className="companions-head__pen"
                          aria-label="改名"
                          disabled={busy}
                          onClick={() => {
                            setEvent(null)
                            setDismissing(false)
                            setRenaming(true)
                          }}
                        >
                          <svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24">
                            <path d="M4 20l4.5-1 10-10-3.5-3.5-10 10z" />
                          </svg>
                        </button>
                      </>
                    )}
                  </div>
                  {contractEntry !== undefined && (
                    <p className="companions-sub">{contractEntry.name}</p>
                  )}
                  <div className="hall-contract__remaining">
                    <span>剩餘戰鬥</span>
                    <b>{formatNumber(contract.remaining)}</b>
                    <span>{`/ ${formatNumber(contractEntry?.fights ?? contract.remaining)}`}</span>
                  </div>
                  <span className="hall-contract__bar">
                    <i
                      role="progressbar"
                      aria-label="剩餘次數"
                      style={{
                        width: `${Math.round(
                          Math.min(
                            1,
                            contract.remaining / (contractEntry?.fights ?? contract.remaining),
                          ) * 100,
                        )}%`,
                      }}
                    />
                  </span>
                  <dl className="hall-facts">
                    <dt>冒險地</dt>
                    <dd>
                      {contract.fighting && contract.placeName !== null ? contract.placeName : "—"}
                    </dd>
                  </dl>
                  {contractEntry !== undefined && (
                    <ul className="hall-contract__rates" aria-label="分成">
                      {(
                        [
                          ["金錢", `${contractEntry.moneyPercent}%`],
                          ["寶物", `${contractEntry.dropPercent}%`],
                          ["經驗", `${contractEntry.experiencePercent}%`],
                          ["戰敗", `−${contractEntry.deathCost}`],
                        ] as const
                      ).map(([label, value]) => (
                        <li key={label}>
                          <span>{label}</span>
                          <b>{value}</b>
                        </li>
                      ))}
                    </ul>
                  )}
                  <div className="companions-actions">
                    {dismissing ? (
                      <>
                        <button
                          type="button"
                          className="companions-button"
                          disabled={busy}
                          onClick={() =>
                            void run(() => api.dismissMercenary(), {
                              kind: "dismissed",
                              name: contract.name,
                            }).then(() => setDismissing(false))
                          }
                        >
                          確定解約
                        </button>
                        <button
                          type="button"
                          className="companions-button companions-button--quiet"
                          onClick={() => setDismissing(false)}
                        >
                          取消
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          type="button"
                          className={`companions-button${contract.fighting ? " companions-button--muted" : ""}`}
                          disabled={busy}
                          onClick={() =>
                            void (contract.fighting
                              ? run(() => api.restMercenary(), {
                                  kind: "rested",
                                  name: contract.name,
                                })
                              : openPlaces(contract))
                          }
                        >
                          {contract.fighting ? "召回休息" : "派遣"}
                        </button>
                        <button
                          type="button"
                          className="companions-button companions-button--quiet"
                          disabled={busy}
                          onClick={() => {
                            setEvent(null)
                            setRenaming(false)
                            setDismissing(true)
                          }}
                        >
                          解約
                        </button>
                      </>
                    )}
                  </div>
                </section>
              )}
            </div>
          )
        )}
      </section>

      {sheetOpen && <div className="inn-sheet__mask" aria-hidden="true" onClick={closeSheet} />}
      <section
        className={`inn-sheet hall-sheet${sheetOpen ? " inn-sheet--open" : ""}`}
        role="dialog"
        aria-label={sheet?.kind === "place" ? "冒險地" : (sheetEntry?.name ?? "傭兵")}
        aria-hidden={!sheetOpen}
        inert={!sheetOpen}
      >
        {sheet !== null && (
          <button
            type="button"
            className="inn-sheet__close"
            aria-label="關閉"
            onClick={closeSheet}
          />
        )}
        {sheetEntry !== undefined && (
          <>
            <div className="inn-sheet__head">
              <span className="inn-sheet__name">
                <b>{sheetEntry.name}</b>
                <span>{`可戰鬥 ${formatNumber(sheetEntry.fights)} 次`}</span>
              </span>
            </div>
            <dl className="hall-facts hall-facts--ruled">
              <dt>金錢</dt>
              <dd>{`${sheetEntry.moneyPercent}%`}</dd>
              <dt>寶物</dt>
              <dd>{`${sheetEntry.dropPercent}%`}</dd>
              <dt>經驗</dt>
              <dd>{`${sheetEntry.experiencePercent}%`}</dd>
              <dt>戰敗消耗</dt>
              <dd>{`${formatNumber(sheetEntry.deathCost)} 次`}</dd>
            </dl>
            {(sheetEntry.price === null || contract !== null) && (
              <p className="inn-sheet__notice inn-sheet__notice--warn">
                {sheetEntry.price === null
                  ? "只能在勳章所兌換"
                  : `已經和${contract!.name}簽約了，一次只能僱用一位`}
              </p>
            )}
            {sheetOpen && line !== null && <KeeperSay line={line} />}
            {sheetEntry.price !== null && (
              <div className="inn-sheet__buy">
                <span className="inn-sheet__cost" data-short={character.money < sheetEntry.price}>
                  價格
                  <img src={purse} alt="" />
                  <b>{formatNumber(sheetEntry.price)}</b>
                </span>
                <Seal
                  className="inn-sheet__seal"
                  label="僱用"
                  disabled={busy || !canHire(sheetEntry, contract, character.money)}
                  onClick={() => void hire(sheetEntry)}
                />
              </div>
            )}
          </>
        )}
        {sheet?.kind === "place" && (
          <>
            <div className="inn-sheet__head">
              <span className="inn-sheet__name">
                <b>冒險地</b>
                <span>{contract?.name ?? ""}</span>
              </span>
            </div>
            <div className="hall-places" role="group" aria-label="冒險地">
              {(groups ?? []).map((group) => (
                <div key={group.name} className="hall-places__group">
                  <h5>{group.name}</h5>
                  {group.places.map((place) => (
                    <button
                      key={place.id}
                      type="button"
                      className="hall-place"
                      aria-pressed={pick === place.id}
                      onClick={() => setSheet({ kind: "place", pick: place.id })}
                    >
                      <b>{place.name}</b>
                      <small>{place.level}</small>
                    </button>
                  ))}
                </div>
              ))}
            </div>
            {sheetOpen && line !== null && <KeeperSay line={line} />}
            <div className="inn-sheet__buy">
              <span className="inn-sheet__cost">
                冒險地
                <b className="hall-places__pick">
                  {groups?.flatMap((group) => group.places).find((place) => place.id === pick)
                    ?.name ?? "—"}
                </b>
              </span>
              <Seal
                className="inn-sheet__seal"
                label="出發"
                disabled={busy || pick === null || contract === null}
                onClick={() => {
                  if (pick !== null && contract !== null) void deploy(contract, pick)
                }}
              />
            </div>
          </>
        )}
      </section>
    </section>
  )
}
