import { useCallback, useEffect, useId, useState } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  activationText,
  canFeed,
  feedOptionLabel,
  petCountText,
  petErrorMessage,
  petStats,
  petTitle,
} from "../game/pets"
import { isNameTooLong, NameLengthError, PetNameMaxLength } from "../game/names"
import type { CharacterView, PetFood, PetsView, PetView } from "../game/types"

export type PetsApi = Pick<
  ApiClient,
  "getPets" | "carryPet" | "storePet" | "releasePet" | "renamePet" | "feedPet"
>

type Props = {
  api: PetsApi
  onCharacterChanged: (character: CharacterView) => void
}

// setTimeout 的上限（32 位元有號整數）
const MaxTimerMilliseconds = 2_147_483_647
const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? petErrorMessage(error.reasons, error.detail) : fallbackError

export function PetsPage({ api, onCharacterChanged }: Props) {
  const [view, setView] = useState<PetsView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<{ petId: number; message: string } | null>(null)
  const [now, setNow] = useState(() => new Date())

  const load = useCallback(
    () =>
      api.getPets().then(
        (loaded) => {
          setView(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  // 餵食冷卻結束時重繪，讓「餵食」按鈕恢復可按
  useEffect(() => {
    if (view === null) return
    const waits = view.pets
      .map((pet) => new Date(pet.nextFeedAt).getTime() - now.getTime())
      .filter((wait) => wait > 0)
    if (waits.length === 0) return
    const timer = setTimeout(() => setNow(new Date()), Math.min(...waits, MaxTimerMilliseconds))
    return () => clearTimeout(timer)
  }, [view, now])

  // 同一時間只送一個請求；畫面一律換成回應裡的寵物清單
  const run = async (petId: number, action: () => Promise<PetsView>) => {
    setBusy(true)
    setError(null)
    try {
      setView(await action())
      setNow(new Date())
      return true
    } catch (caught) {
      setError({ petId, message: describeError(caught) })
      return false
    } finally {
      setBusy(false)
    }
  }

  const feed = (petId: number, foodId: string) =>
    run(petId, async () => {
      const result = await api.feedPet(petId, foodId)
      onCharacterChanged(result.character)
      return result.pets
    })

  if (loadError)
    return (
      <section className="bag__card">
        <p className="error" role="alert">
          無法載入寵物。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load()}>
            重試
          </button>
        </div>
      </section>
    )
  if (view === null) return <p className="bag__empty">載入中…</p>

  return (
    <>
      <section className="bag__card companions__intro">
        <h2 className="companions__title">{petCountText(view)}</h2>
        <p>餵食初級須花費20元，中級須花費50元</p>
        <p>選擇攜帶時，寵物會根據親密值及主人魅力值，隨機出現支援戰鬥或抵擋敵人攻擊</p>
      </section>
      {view.pets.length === 0 ? (
        <p className="bag__empty">還沒有寵物。在補給欄裝上捕獸道具，打贏時有機會捕捉。</p>
      ) : (
        view.pets.map((pet) => (
          <PetCard
            key={pet.id}
            pet={pet}
            foods={view.foods}
            now={now}
            busy={busy}
            error={error?.petId === pet.id ? error.message : null}
            onCarry={() => void run(pet.id, () => api.carryPet(pet.id))}
            onStore={() => void run(pet.id, () => api.storePet(pet.id))}
            onRename={(name) => run(pet.id, () => api.renamePet(pet.id, name))}
            onRelease={() => run(pet.id, () => api.releasePet(pet.id))}
            onFeed={(foodId) => void feed(pet.id, foodId)}
            onDismissError={() => setError(null)}
          />
        ))
      )}
    </>
  )
}

type CardProps = {
  pet: PetView
  foods: PetFood[]
  now: Date
  busy: boolean
  error: string | null
  onCarry: () => void
  onStore: () => void
  onRename: (name: string) => Promise<boolean>
  onRelease: () => Promise<boolean>
  onFeed: (foodId: string) => void
  onDismissError: () => void
}

function PetCard({
  pet,
  foods,
  now,
  busy,
  error,
  onCarry,
  onStore,
  onRename,
  onRelease,
  onFeed,
  onDismissError,
}: CardProps) {
  const titleId = useId()
  const nameErrorId = useId()
  const [name, setName] = useState<string | null>(null)
  const nameTooLong = isNameTooLong(name ?? pet.name, PetNameMaxLength)
  const [foodId, setFoodId] = useState(foods[0]?.id ?? "")
  const [confirming, setConfirming] = useState(false)

  return (
    <article className="companions__card" aria-labelledby={titleId}>
      <header className="companions__card-head">
        <h3 id={titleId} className="companions__name">
          {petTitle(pet)}
        </h3>
        <span className={pet.carried ? "companions__badge" : "companions__status"}>
          {pet.carried ? "攜帶中" : "獸欄"}
        </span>
      </header>
      <ul className="companions__stats" aria-label="能力">
        {petStats(pet).map(({ label, value }) => (
          <li key={label}>
            <span className="companions__stat-label">{label}</span>
            <span className="companions__stat-value">{value}</span>
          </li>
        ))}
      </ul>
      <p className="companions__note">{activationText(pet)}</p>
      <div className="companions__row">
        <label className="companions__field">
          名字
          <input
            type="text"
            value={name ?? pet.name}
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
            void onRename(name ?? pet.name).then((done) => {
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
      <div className="companions__row">
        <label className="companions__field">
          餵食項目
          <select value={foodId} onChange={(event) => setFoodId(event.target.value)}>
            {foods.map((food) => (
              <option key={food.id} value={food.id}>
                {feedOptionLabel(food)}
              </option>
            ))}
          </select>
        </label>
        <button
          type="button"
          className="bag__action"
          disabled={busy || !canFeed(pet, now)}
          onClick={() => onFeed(foodId)}
        >
          餵食
        </button>
      </div>
      <div className="companions__buttons">
        {pet.carried ? (
          <button type="button" className="bag__action" disabled={busy} onClick={onStore}>
            放回獸欄
          </button>
        ) : (
          <button type="button" className="bag__action" disabled={busy} onClick={onCarry}>
            攜帶
          </button>
        )}
        {confirming ? (
          <>
            <button
              type="button"
              className="bag__action companions__danger"
              disabled={busy}
              onClick={() =>
                void onRelease().then((done) => {
                  if (!done) setConfirming(false)
                })
              }
            >
              確定放生
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
            放生
          </button>
        )}
      </div>
      {error !== null && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </article>
  )
}
