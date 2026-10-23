import { useCallback, useEffect, useId, useState, type ReactNode } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  actionLabel,
  completionLines,
  missionErrorMessage,
  objectiveLines,
  progressRatio,
  requirementLines,
  resetHoursText,
  rewardLines,
} from "../game/missions"
import type {
  CharacterView,
  ContentNames,
  MissionBoard,
  MissionCompletion,
  MissionView,
} from "../game/types"
import "./BagScreen.css"
import "./MissionsScreen.css"
import { TabPanel, Tabs } from "./bagWidgets"
import { usePanelRefresh } from "./usePanelRefresh"

export type MissionsApi = Pick<
  ApiClient,
  "getMissions" | "acceptMission" | "abandonMission" | "completeMission"
>

type Props = {
  api: MissionsApi
  content: ContentNames
  onCharacterChanged: (character: CharacterView) => void
  // 戰報有任務進度時遞增，開著的任務頁跟著重新載入
  refreshSignal?: number
}

type SubPage = "available" | "active" | "completed"

const subPages: { id: SubPage; label: string }[] = [
  { id: "available", label: "可接" },
  { id: "active", label: "進行中" },
  { id: "completed", label: "已完成" },
]

const fallbackError = "發生錯誤，請稍後再試"

const describeError = (error: unknown) =>
  error instanceof ApiError ? missionErrorMessage(error.reasons, error.detail) : fallbackError

export function MissionsScreen({ api, content, onCharacterChanged, refreshSignal = 0 }: Props) {
  const tabsId = useId()
  const [page, setPage] = useState<SubPage>("available")
  const [board, setBoard] = useState<MissionBoard | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<{ missionId: string; message: string } | null>(null)
  const [completion, setCompletion] = useState<MissionCompletion | null>(null)
  const [confirmingId, setConfirmingId] = useState<string | null>(null)
  // 分頁每次打開都重新掛載，重置倒數以打開時為準
  const [now] = useState(() => new Date())

  const load = useCallback(
    () =>
      api.getMissions().then(
        (loaded) => {
          setBoard(loaded)
          setLoadError(false)
        },
        () => setLoadError(true),
      ),
    [api],
  )

  useEffect(() => {
    void load()
  }, [load])

  // 背景重載失敗就保留目前的任務板
  usePanelRefresh(refreshSignal, () => {
    api.getMissions().then(setBoard, () => {})
  })

  // 同一時間只送一個請求；畫面一律換成回應裡的任務板
  const run = async (missionId: string, action: () => Promise<MissionBoard>) => {
    setBusy(true)
    setError(null)
    setCompletion(null)
    try {
      setBoard(await action())
      setConfirmingId(null)
    } catch (caught) {
      setError({ missionId, message: describeError(caught) })
    } finally {
      setBusy(false)
    }
  }

  const complete = (missionId: string) =>
    run(missionId, async () => {
      const result = await api.completeMission(missionId)
      setCompletion(result)
      onCharacterChanged(result.character)
      return result.board
    })

  const errorFor = (mission: MissionView) =>
    error?.missionId === mission.id ? (
      <p className="error" role="alert">
        {error.message}
      </p>
    ) : null

  const renderPage = (current: MissionBoard) => {
    // 可接的卡片列出條件；進行中的卡片改顯示進度條
    const card = (mission: MissionView, accepted: boolean, actions: ReactNode) => (
      <MissionCard
        key={mission.id}
        mission={mission}
        reset={mission.daily ? resetHoursText(current.nextDailyReset, now) : null}
        requirements={accepted ? [] : requirementLines(mission, content)}
        progress={accepted}
      >
        {actions}
        {errorFor(mission)}
      </MissionCard>
    )

    switch (page) {
      case "available": {
        const full = current.active.length >= current.activeLimit
        const accept = (mission: MissionView) =>
          card(
            mission,
            false,
            <div className="missions__buttons">
              <button
                type="button"
                className="bag__action"
                disabled={busy || full}
                onClick={() => void run(mission.id, () => api.acceptMission(mission.id))}
              >
                接取
              </button>
            </div>,
          )
        const daily = current.available.filter((mission) => mission.daily)
        const regular = current.available.filter((mission) => !mission.daily)
        return (
          <>
            {full && (
              <p className="missions__notice">{`進行中的任務已達 ${current.activeLimit} 個`}</p>
            )}
            {current.available.length === 0 ? (
              <p className="bag__empty">目前無任務</p>
            ) : (
              <>
                {daily.length > 0 && (
                  <MissionGroup title="每日任務">{daily.map(accept)}</MissionGroup>
                )}
                {regular.length > 0 && (
                  <MissionGroup title="一般任務">{regular.map(accept)}</MissionGroup>
                )}
              </>
            )}
          </>
        )
      }
      case "active":
        return current.active.length === 0 ? (
          <p className="bag__empty">沒有進行中的任務</p>
        ) : (
          current.active.map((mission) =>
            card(
              mission,
              true,
              <div className="missions__buttons">
                <button
                  type="button"
                  className="bag__action"
                  disabled={busy || !mission.ready}
                  onClick={() => void complete(mission.id)}
                >
                  {actionLabel(mission)}
                </button>
                {confirmingId === mission.id ? (
                  <>
                    <button
                      type="button"
                      className="bag__action missions__danger"
                      disabled={busy}
                      onClick={() => void run(mission.id, () => api.abandonMission(mission.id))}
                    >
                      確定放棄
                    </button>
                    <button
                      type="button"
                      className="bag__action"
                      disabled={busy}
                      onClick={() => setConfirmingId(null)}
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
                      setConfirmingId(mission.id)
                      setError(null)
                    }}
                  >
                    放棄
                  </button>
                )}
              </div>,
            ),
          )
        )
      case "completed":
        return current.completed.length === 0 ? (
          <p className="bag__empty">還沒有完成的任務</p>
        ) : (
          <ul className="missions__done">
            {current.completed.map((mission) => (
              <CompletedMission key={mission.id} mission={mission} />
            ))}
          </ul>
        )
    }
  }

  return (
    <div className="missions">
      <Tabs
        id={tabsId}
        label="任務分頁"
        options={subPages}
        selected={page}
        onSelect={(next) => {
          setPage(next)
          setError(null)
          setConfirmingId(null)
        }}
        variant="pages"
      />
      <TabPanel tabsId={tabsId} selected={page}>
        {completion !== null && (
          <section className="missions__result" role="status">
            {completionLines(completion).map((line, index) => (
              <p key={index} className={index === 0 ? "missions__speech" : undefined}>
                {line}
              </p>
            ))}
          </section>
        )}
        {loadError ? (
          <div className="bag__card">
            <p className="error" role="alert">
              無法載入任務。
            </p>
            <div className="actions">
              <button type="button" onClick={() => void load()}>
                重試
              </button>
            </div>
          </div>
        ) : board === null ? (
          <p className="bag__empty">載入中…</p>
        ) : (
          renderPage(board)
        )}
      </TabPanel>
    </div>
  )
}

function MissionGroup({ title, children }: { title: string; children: ReactNode }) {
  const headingId = useId()
  return (
    <section className="missions__group" aria-labelledby={headingId}>
      <h3 id={headingId} className="bag__panel-title">
        {title}
      </h3>
      {children}
    </section>
  )
}

function MissionCard({
  mission,
  reset,
  requirements,
  progress,
  children,
}: {
  mission: MissionView
  reset: string | null
  requirements: string[]
  progress: boolean
  children: ReactNode
}) {
  const titleId = useId()
  const objectives = objectiveLines(mission)
  const rewards = rewardLines(mission.rewards)
  const ratio = progressRatio(mission)
  return (
    <article className="missions__card" aria-labelledby={titleId}>
      <h4 id={titleId} className="missions__title">
        {mission.title}
      </h4>
      <p className="missions__giver">{`委託者：${mission.giver}`}</p>
      <p className="missions__description">{mission.description}</p>
      {requirements.length > 0 && (
        <ul className="missions__requirements" aria-label="條件">
          {requirements.map((line, index) => (
            <li key={index}>{line}</li>
          ))}
        </ul>
      )}
      {objectives.length > 0 && (
        <div className="missions__block">
          <h5 className="missions__label">委託內容</h5>
          <ul className="missions__lines">
            {objectives.map((line, index) => (
              <li key={index}>{line}</li>
            ))}
          </ul>
          {progress && (
            <div
              role="progressbar"
              aria-label="任務進度"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={Math.round(ratio * 100)}
              className="missions__progress"
            >
              <div className="missions__progress-fill" style={{ width: `${ratio * 100}%` }} />
            </div>
          )}
        </div>
      )}
      {rewards.length > 0 && (
        <div className="missions__block">
          <h5 className="missions__label">委託報酬</h5>
          <ul className="missions__lines">
            {rewards.map((line, index) => (
              <li key={index}>{line}</li>
            ))}
          </ul>
        </div>
      )}
      {reset !== null && <p className="missions__reset">{reset}</p>}
      {children}
    </article>
  )
}

function CompletedMission({ mission }: { mission: MissionView }) {
  const titleId = useId()
  return (
    <li>
      <article className="missions__done-item" aria-labelledby={titleId}>
        <span id={titleId} className="missions__title">
          {mission.title}
        </span>
        <span className="missions__giver">{`委託者：${mission.giver}`}</span>
        <span className="missions__level">{`Lv ${mission.level}`}</span>
      </article>
    </li>
  )
}
