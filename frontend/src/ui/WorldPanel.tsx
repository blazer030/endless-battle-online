import { useCallback, useEffect, useId, useState } from "react"
import { ApiError, type ApiClient } from "../api/client"
import type { WorldAreaVo, WorldPlace } from "../game/world"
import type { CharacterVo } from "../game/character"
import { moveReasonText, passActive, placeAccess, recommendationLabel } from "../game/world"
import "./WorldPanel.css"

export type WorldApi = Pick<ApiClient, "getWorld" | "moveTo">

type Props = {
  api: WorldApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  onClose: () => void
}

const fallbackError = "發生錯誤，請稍後再試"
const conflictError = "角色資料已被其他請求更新，請再試一次"

const describeMoveError = (error: unknown): string[] => {
  if (!(error instanceof ApiError)) return [fallbackError]
  if (error.status === 409) return [conflictError]
  return error.reasons.length === 0 ? [error.detail] : error.reasons.map(moveReasonText)
}

export function WorldPanel({ api, character, onCharacterChange, onClose }: Props) {
  const titleId = useId()
  const [areas, setAreas] = useState<WorldAreaVo[] | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [expandedPlaceId, setExpandedPlaceId] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)
  const [moveErrors, setMoveErrors] = useState<string[]>([])

  const show = useCallback((loaded: WorldAreaVo[]) => {
    setAreas(loaded)
    setLoadError(false)
  }, [])

  useEffect(() => {
    let isCurrent = true
    api.getWorld().then(
      (loaded) => {
        if (isCurrent) show(loaded)
      },
      () => {
        if (isCurrent) setLoadError(true)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, show])

  const reload = () => {
    setLoadError(false)
    api.getWorld().then(show, () => setLoadError(true))
  }

  const moveTo = async (placeId: number, subPlace: number | null) => {
    setBusy(true)
    setMoveErrors([])
    try {
      onCharacterChange(await api.moveTo(placeId, subPlace))
      onClose()
    } catch (error) {
      setMoveErrors(describeMoveError(error))
      setBusy(false)
    }
  }

  return (
    <section className="world" role="dialog" aria-labelledby={titleId}>
      <header className="world__head">
        <h2 id={titleId} className="world__title">
          選擇地區
        </h2>
        <button type="button" className="world__close" onClick={onClose}>
          關閉
        </button>
      </header>
      <div className="world__body">
        {loadError ? (
          <>
            <p className="error" role="alert">
              無法載入地區資料。
            </p>
            <div className="actions">
              <button type="button" onClick={reload}>
                重試
              </button>
            </div>
          </>
        ) : areas === null ? (
          <p className="world__loading">載入中…</p>
        ) : (
          areas.map((area) => (
            <AreaSection
              key={area.id}
              area={area}
              character={character}
              expandedPlaceId={expandedPlaceId}
              busy={busy}
              onToggle={(placeId) =>
                setExpandedPlaceId((current) => (current === placeId ? null : placeId))
              }
              onMove={(placeId, subPlace) => void moveTo(placeId, subPlace)}
            />
          ))
        )}
      </div>
      {moveErrors.length > 0 && (
        <ul className="world__errors" role="alert">
          {moveErrors.map((text) => (
            <li key={text}>{text}</li>
          ))}
        </ul>
      )}
    </section>
  )
}

type AreaProps = {
  area: WorldAreaVo
  character: CharacterVo
  expandedPlaceId: number | null
  busy: boolean
  onToggle: (placeId: number) => void
  onMove: (placeId: number, subPlace: number | null) => void
}

function AreaSection({ area, character, expandedPlaceId, busy, onToggle, onMove }: AreaProps) {
  const areaId = useId()
  return (
    <section className="world__area" aria-labelledby={areaId}>
      <h3 id={areaId} className="world__area-name">
        {area.name}
      </h3>
      <ul className="world__places">
        {area.places.map((place) => (
          <PlaceRow
            key={place.id}
            place={place}
            character={character}
            expanded={expandedPlaceId === place.id}
            busy={busy}
            onToggle={() => onToggle(place.id)}
            onMove={(subPlace) => onMove(place.id, subPlace)}
          />
        ))}
      </ul>
    </section>
  )
}

type PlaceProps = {
  place: WorldPlace
  character: CharacterVo
  expanded: boolean
  busy: boolean
  onToggle: () => void
  onMove: (subPlace: number | null) => void
}

function PlaceRow({ place, character, expanded, busy, onToggle, onMove }: PlaceProps) {
  const subPlacesId = useId()
  const label = recommendationLabel(place.recommendation)
  const now = new Date()
  const access = placeAccess(place, now)
  // 禁區通行證到期後，目前所在的子地區也能用鑰匙再次進入
  const reentry = place.restricted && !passActive(place.passExpiresAt, now)
  const isHere = (subPlace: number | null) =>
    character.placeId === place.id && character.subPlace === subPlace
  return (
    <li className="world__place">
      <button
        type="button"
        className="world__place-button"
        aria-expanded={expanded}
        aria-controls={expanded ? subPlacesId : undefined}
        disabled={!access.open}
        onClick={onToggle}
      >
        <span className="world__place-name">{place.name}</span>
        <span className="world__place-level">{`Lv ${place.recommended.minimum}–${place.recommended.maximum}`}</span>
        {label !== null && (
          <span className="world__tag" data-tone={label.tone}>
            {label.text}
          </span>
        )}
        {place.current && <span className="world__badge">目前</span>}
        {access.label !== null && <span className="world__locked">{access.label}</span>}
      </button>
      {expanded &&
        (place.subPlaces.length === 0 ? (
          <div id={subPlacesId} className="world__sub-places">
            <SubPlaceButton
              name="前往"
              here={isHere(null)}
              reentry={reentry}
              busy={busy}
              onClick={() => onMove(null)}
            />
          </div>
        ) : (
          <div
            id={subPlacesId}
            className="world__sub-places"
            role="group"
            aria-label={`${place.name}的子地區`}
          >
            {place.subPlaces.map((subPlace) => (
              <SubPlaceButton
                key={subPlace.index}
                name={subPlace.name}
                here={isHere(subPlace.index)}
                reentry={reentry}
                busy={busy}
                onClick={() => onMove(subPlace.index)}
              />
            ))}
          </div>
        ))}
    </li>
  )
}

function SubPlaceButton({
  name,
  here,
  reentry,
  busy,
  onClick,
}: {
  name: string
  here: boolean
  reentry: boolean
  busy: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      className="world__sub-place"
      aria-current={here ? "location" : undefined}
      disabled={(here && !reentry) || busy}
      onClick={onClick}
    >
      {name}
    </button>
  )
}
