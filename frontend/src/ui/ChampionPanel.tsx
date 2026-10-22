import { useCallback, useEffect, useId, useState } from "react"
import { ApiError, type ApiClient } from "../api/client"
import type { LogEntry } from "../game/battleLog"
import { championErrorMessage, championStats, championTitle, duelLog } from "../game/champion"
import type { CharacterView, ChampionView } from "../game/types"
import "./ChampionPanel.css"

export type ChampionApi = Pick<ApiClient, "getChampion" | "challengeChampion">

type Props = {
  api: ChampionApi
  onCharacterChanged: (character: CharacterView) => void
  onClose: () => void
}

const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? championErrorMessage(error.reasons, error.detail) : fallbackError

export function ChampionPanel({ api, onCharacterChanged, onClose }: Props) {
  const titleId = useId()
  const [champion, setChampion] = useState<ChampionView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<LogEntry[] | null>(null)
  // 對話框每次打開都重新掛載，鎖定倒數以打開時為準
  const [now] = useState(() => new Date())

  const load = useCallback(
    () =>
      api.getChampion().then(
        (loaded) => {
          setChampion(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  const challenge = async (opponent: string) => {
    setBusy(true)
    setError(null)
    setResult(null)
    try {
      const outcome = await api.challengeChampion()
      setResult(duelLog(outcome, opponent))
      setChampion(outcome.champion)
      onCharacterChanged(outcome.character)
    } catch (caught) {
      setError(describeError(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="champion" role="dialog" aria-labelledby={titleId}>
      <header className="champion__head">
        <h2 id={titleId} className="champion__title">
          冠軍
        </h2>
        <button type="button" className="champion__close" onClick={onClose}>
          關閉
        </button>
      </header>
      <div className="champion__body">
        {loadError ? (
          <>
            <p className="error" role="alert">
              無法載入冠軍資料。
            </p>
            <div className="actions">
              <button type="button" onClick={() => void load()}>
                重試
              </button>
            </div>
          </>
        ) : champion === null ? (
          <p className="champion__loading">載入中…</p>
        ) : (
          <SeatCard
            champion={champion}
            now={now}
            busy={busy}
            error={error}
            onChallenge={() => void challenge(champion.name)}
          />
        )}
        {result !== null && (
          <ul className="champion__log" role="log" aria-label="挑戰結果">
            {result.map((line, index) => (
              <li key={index} className={`champion__line champion__line--${line.kind}`}>
                {line.text}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}

function SeatCard({
  champion,
  now,
  busy,
  error,
  onChallenge,
}: {
  champion: ChampionView
  now: Date
  busy: boolean
  error: string | null
  onChallenge: () => void
}) {
  const nameId = useId()
  const { title, subtitle, lock } = championTitle(champion, now)
  const blocked = champion.reasons.length > 0
  return (
    <article className="champion__seat" aria-labelledby={nameId}>
      <h3 id={nameId} className="champion__name">
        {title}
      </h3>
      {subtitle !== null && <p className="champion__subtitle">{subtitle}</p>}
      <ul className="champion__stats">
        {championStats(champion).map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>
      {lock !== null && <p className="champion__lock">{lock}</p>}
      {blocked && (
        <p className="champion__reasons">{championErrorMessage(champion.reasons, "無法挑戰")}</p>
      )}
      <button
        type="button"
        className="champion__challenge"
        disabled={busy || blocked}
        onClick={onChallenge}
      >
        挑戰
      </button>
      {error !== null && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </article>
  )
}
