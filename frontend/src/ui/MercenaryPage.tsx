import { useCallback, useEffect, useId, useState, type ReactNode } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  catalogueRow,
  contractStatus,
  deployablePlaces,
  mercenaryErrorMessage,
} from "../game/mercenaries"
import { isNameTooLong, MercenaryNameMaxLength, NameLengthError } from "../game/names"
import type {
  CharacterView,
  MercenaryCatalogueEntry,
  MercenaryContract,
  MercenaryOverview,
  WorldPlace,
} from "../game/types"

export type MercenaryApi = Pick<
  ApiClient,
  | "getMercenaries"
  | "hireMercenary"
  | "deployMercenary"
  | "restMercenary"
  | "renameMercenary"
  | "dismissMercenary"
  | "getWorld"
>

type Props = {
  api: MercenaryApi
  onCharacterChanged: (character: CharacterView) => void
}

const fallbackError = "發生錯誤，請稍後再試"
const contractKey = "contract"

const describeError = (error: unknown) =>
  error instanceof ApiError ? mercenaryErrorMessage(error.reasons, error.detail) : fallbackError

export function MercenaryPage({ api, onCharacterChanged }: Props) {
  const [overview, setOverview] = useState<MercenaryOverview | null>(null)
  const [places, setPlaces] = useState<WorldPlace[] | null>(null)
  const [placesError, setPlacesError] = useState<string | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  // key 是傭兵 id，或合約卡的 contractKey
  const [error, setError] = useState<{ key: string; message: string } | null>(null)

  const load = useCallback(
    () =>
      api.getMercenaries().then(
        (loaded) => {
          setOverview(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  // 地區載入失敗時合約卡顯示原因與重試，其餘照常
  const loadPlaces = useCallback(
    () =>
      api.getWorld().then(
        (areas) => {
          setPlaces(deployablePlaces(areas))
          setPlacesError(null)
        },
        (caught: unknown) => setPlacesError(describeError(caught)),
      ),
    [api],
  )

  useEffect(() => {
    void loadPlaces()
  }, [loadPlaces])

  // 同一時間只送一個請求；畫面一律換成回應裡的傭兵資料
  const run = async (key: string, action: () => Promise<MercenaryOverview>) => {
    setBusy(true)
    setError(null)
    try {
      setOverview(await action())
      return true
    } catch (caught) {
      setError({ key, message: describeError(caught) })
      return false
    } finally {
      setBusy(false)
    }
  }

  const hire = (mercenaryId: string) =>
    run(mercenaryId, async () => {
      const result = await api.hireMercenary(mercenaryId)
      onCharacterChanged(result.character)
      return result.mercenaries
    })

  const errorFor = (key: string) =>
    error?.key === key ? (
      <p className="error" role="alert">
        {error.message}
      </p>
    ) : null

  if (loadError)
    return (
      <section className="bag__card">
        <p className="error" role="alert">
          無法載入傭兵所。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load()}>
            重試
          </button>
        </div>
      </section>
    )
  if (overview === null) return <p className="bag__empty">載入中…</p>

  return (
    <>
      <section className="bag__card companions__intro">
        <p>
          歡迎來到傭兵所，請選擇欲雇用的傭兵。傭兵的能力與技能和你相同，不同等級的傭兵戰鬥獲得的金錢、寶物與經驗比例不同；死亡消耗為傭兵戰敗時扣減的戰鬥次數。
        </p>
      </section>
      {overview.contract !== null && (
        <ContractCard
          // 換了傭兵時名字欄回到新名字
          key={overview.contract.mercenaryId}
          contract={overview.contract}
          places={places}
          placesError={placesError}
          onRetryPlaces={() => void loadPlaces()}
          busy={busy}
          error={errorFor(contractKey)}
          onRename={(name) => run(contractKey, () => api.renameMercenary(name))}
          onDeploy={(placeId) => void run(contractKey, () => api.deployMercenary(placeId))}
          onRest={() => void run(contractKey, () => api.restMercenary())}
          onDismiss={() => run(contractKey, () => api.dismissMercenary())}
          onDismissError={() => setError(null)}
        />
      )}
      {overview.catalogue.map((entry) => (
        <CatalogueCard
          key={entry.id}
          entry={entry}
          busy={busy || overview.contract !== null}
          error={errorFor(entry.id)}
          onHire={() => void hire(entry.id)}
        />
      ))}
      {overview.contract !== null && <p className="companions__note">只能僱用一位傭兵</p>}
    </>
  )
}

function CatalogueCard({
  entry,
  busy,
  error,
  onHire,
}: {
  entry: MercenaryCatalogueEntry
  busy: boolean
  error: ReactNode
  onHire: () => void
}) {
  const titleId = useId()
  const row = catalogueRow(entry)
  const fields = [
    { label: "價格", value: row.price },
    { label: "戰鬥次數", value: row.fights },
    { label: "金錢", value: row.money },
    { label: "寶物", value: row.drop },
    { label: "經驗", value: row.exp },
    { label: "死亡消耗", value: row.deathCost },
  ]
  return (
    <article className="companions__card" aria-labelledby={titleId}>
      <header className="companions__card-head">
        <h3 id={titleId} className="companions__name">
          {row.name}
        </h3>
        {row.hireable && (
          <button type="button" className="bag__action" disabled={busy} onClick={onHire}>
            僱用
          </button>
        )}
      </header>
      <ul className="companions__stats companions__stats--wide" aria-label="條件">
        {fields.map(({ label, value }) => (
          <li key={label}>
            <span className="companions__stat-label">{label}</span>
            <span className="companions__stat-value">{value}</span>
          </li>
        ))}
      </ul>
      {error}
    </article>
  )
}

type ContractProps = {
  contract: MercenaryContract
  places: WorldPlace[] | null
  placesError: string | null
  onRetryPlaces: () => void
  busy: boolean
  error: ReactNode
  onRename: (name: string) => Promise<boolean>
  onDeploy: (placeId: number) => void
  onRest: () => void
  onDismiss: () => Promise<boolean>
  onDismissError: () => void
}

function ContractCard({
  contract,
  places,
  placesError,
  onRetryPlaces,
  busy,
  error,
  onRename,
  onDeploy,
  onRest,
  onDismiss,
  onDismissError,
}: ContractProps) {
  const headingId = useId()
  const nameErrorId = useId()
  const [name, setName] = useState<string | null>(null)
  const nameTooLong = isNameTooLong(name ?? contract.name, MercenaryNameMaxLength)
  const [placeId, setPlaceId] = useState<number | null>(contract.placeId)
  const [confirming, setConfirming] = useState(false)
  const chosen = places?.find((place) => place.id === placeId)?.id ?? places?.[0]?.id ?? null

  return (
    <section className="companions__card companions__contract" aria-labelledby={headingId}>
      <h3 id={headingId} className="bag__panel-title">
        僱用中的傭兵
      </h3>
      <div className="companions__row">
        <label className="companions__field">
          名字
          <input
            type="text"
            value={name ?? contract.name}
            aria-invalid={nameTooLong || undefined}
            aria-describedby={nameTooLong ? nameErrorId : undefined}
            onChange={(event) => setName(event.target.value)}
          />
        </label>
        <button
          type="button"
          className="bag__action"
          disabled={busy || nameTooLong}
          onClick={() =>
            void onRename(name ?? contract.name).then((done) => {
              if (done) setName(null)
            })
          }
        >
          改名
        </button>
      </div>
      {nameTooLong && (
        <p id={nameErrorId} className="error">
          {NameLengthError}
        </p>
      )}
      <ul className="companions__lines">
        {contractStatus(contract).map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>
      {contract.fighting ? (
        <div className="companions__buttons">
          <button type="button" className="bag__action" disabled={busy} onClick={onRest}>
            停止戰鬥
          </button>
        </div>
      ) : placesError !== null ? (
        <div className="companions__row">
          <p className="error" role="alert">{`無法載入地區：${placesError}`}</p>
          <button type="button" className="bag__action" onClick={onRetryPlaces}>
            重試
          </button>
        </div>
      ) : places === null ? (
        <p className="bag__empty">載入中…</p>
      ) : (
        <div className="companions__row">
          <label className="companions__field">
            冒險地
            <select
              value={chosen ?? ""}
              onChange={(event) => setPlaceId(Number(event.target.value))}
            >
              {places.map((place) => (
                <option key={place.id} value={place.id}>
                  {place.name}
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className="bag__action"
            disabled={busy || chosen === null}
            onClick={() => {
              if (chosen !== null) onDeploy(chosen)
            }}
          >
            開始戰鬥
          </button>
        </div>
      )}
      <div className="companions__buttons">
        {confirming ? (
          <>
            <button
              type="button"
              className="bag__action companions__danger"
              disabled={busy}
              onClick={() =>
                void onDismiss().then((done) => {
                  if (!done) setConfirming(false)
                })
              }
            >
              確定解除
            </button>
            <button
              type="button"
              className="bag__action"
              disabled={busy}
              onClick={() => setConfirming(false)}
            >
              取消
            </button>
          </>
        ) : (
          <button
            type="button"
            className="bag__action"
            disabled={busy}
            onClick={() => {
              setConfirming(true)
              onDismissError()
            }}
          >
            解除僱用
          </button>
        )}
      </div>
      {error}
    </section>
  )
}
