import { lazy, Suspense, useCallback, useEffect, useRef, useState } from "react"
import type { ApiClient } from "../api/client"
import { createMonsterNameResolver, describePlaybackItem, MaxLogLines } from "../game/battleLog"
import type { LogLine } from "../game/battleLog"
import { BattlePlayback } from "../game/battlePlayback"
import type { PlaybackItem } from "../game/battlePlayback"
import { applyToStage, initialStageState, stageEffectsFor } from "../game/stageModel"
import type { CharacterView, ContentNames } from "../game/types"
import { StageHeight } from "../render/stageLayout"
import type { StageHandle } from "../render/StageRenderer"
import { BattleLog } from "./BattleLog"
import { CharacterScreen } from "./CharacterScreen"
import { formatNumber } from "./formatNumber"
import { StageErrorBoundary } from "./StageErrorBoundary"
import { StatBar } from "./StatBar"
import { useAdventureLoop } from "./useAdventureLoop"
import "./AdventureScreen.css"

// Pixi 與 GSAP 拆成獨立 chunk，舞台載入前先用同高的空白占位
const PixiStage = lazy(() =>
  import("../render/PixiStage").then((module) => ({ default: module.PixiStage })),
)

const emptyContent: ContentNames = {
  monsters: [],
  stages: [],
  jobs: [],
  elements: [],
  births: [],
  sexes: [],
}

type Tab = "adventure" | "character"

const tabs: { id: Tab; label: string }[] = [
  { id: "adventure", label: "冒險" },
  { id: "character", label: "角色" },
]

const unavailableTabs = ["技能", "背包"]

type Props = { api: ApiClient; initialCharacter: CharacterView }

export function AdventureScreen({ api, initialCharacter }: Props) {
  const [content, setContent] = useState<ContentNames | null>(null)
  const [playback] = useState(() => new BattlePlayback(initialCharacter))
  const [character, setCharacter] = useState(initialCharacter)
  const [lines, setLines] = useState<LogLine[]>([])
  const [tab, setTab] = useState<Tab>("adventure")
  const stageHandle = useRef<StageHandle>(null)
  // 舞台狀態只是播放進度，不需要觸發重繪
  const stageState = useRef(initialStageState)
  const currentMonsterName = useRef<string | null>(null)
  const nextLineId = useRef(0)

  // 舞台 chunk 載入期間 handle 還不存在，接上時補播目前這隻妖怪與血量
  const attachStage = useCallback((handle: StageHandle | null) => {
    stageHandle.current = handle
    const state = stageState.current
    if (handle === null || state.monsterId === null || currentMonsterName.current === null) return
    handle.showEncounter(state.monsterId, currentMonsterName.current, state)
    handle.updateHealth(state)
  }, [])

  useEffect(() => {
    let isCurrent = true
    api.getContent().then(
      (loaded) => {
        if (isCurrent) setContent(loaded)
      },
      () => {
        // 名稱表載入失敗時以 id 顯示，不擋住歷練
        if (isCurrent) setContent(emptyContent)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api])

  const stageName =
    content?.stages.find((stage) => stage.id === character.currentStageId)?.name ??
    character.currentStageId

  const handleItems = (items: PlaybackItem[]) => {
    const monsterName = createMonsterNameResolver(content ?? emptyContent)
    const added: LogLine[] = []
    for (const item of items) {
      if (item.kind === "event") {
        const { event } = item
        stageState.current = applyToStage(stageState.current, event)
        if (event.type === "encounter") {
          currentMonsterName.current = monsterName(event.monsterId)
          stageHandle.current?.showEncounter(
            event.monsterId,
            currentMonsterName.current,
            stageState.current,
          )
        } else if (event.type === "attack") {
          stageHandle.current?.updateHealth(stageState.current)
        }
        stageHandle.current?.play(stageEffectsFor(event))
      }
      const line = describePlaybackItem(item, monsterName, stageState.current.monsterId)
      if (line !== null) added.push({ id: nextLineId.current++, ...line })
    }
    setLines((previous) => [...previous, ...added].slice(-MaxLogLines))
    setCharacter(playback.displayedCharacter)
  }

  return (
    <div className="adventure">
      <header className="adventure__top">
        <span className="adventure__identity">
          <span className="adventure__name">{character.name}</span>
          <span className="adventure__level">{`Lv ${character.level}`}</span>
        </span>
        <span className="adventure__money">{`金錢 ${formatNumber(character.money)}`}</span>
        <span className="adventure__exp">
          <span className="adventure__exp-label">EXP</span>
          <StatBar label="EXP" value={character.exp} max={character.nextExp} tone="exp" />
          <span className="adventure__exp-value">{`${formatNumber(character.exp)} / ${formatNumber(character.nextExp)}`}</span>
        </span>
      </header>
      <main className="adventure__main">
        {/* 切到角色分頁時只隱藏不卸載：輪詢、舞台與戰報都繼續跑 */}
        <div className="adventure__panel" hidden={tab !== "adventure"}>
          <h1 className="adventure__stage-name">{stageName}</h1>
          <StageErrorBoundary>
            <Suspense fallback={<div style={{ height: StageHeight }} />}>
              <PixiStage ref={attachStage} />
            </Suspense>
          </StageErrorBoundary>
          {/* 名稱表就緒才開始輪詢，戰報與舞台才拿得到妖怪名字 */}
          {content !== null && (
            <AdventureLoop api={api} playback={playback} onItems={handleItems} />
          )}
          <BattleLog lines={lines} />
        </div>
        <div className="adventure__sheet" hidden={tab !== "character"}>
          <CharacterScreen character={character} content={content ?? emptyContent} />
        </div>
      </main>
      <nav className="adventure__nav">
        {tabs.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            className="adventure__nav-item"
            aria-current={tab === id ? "page" : undefined}
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
        {unavailableTabs.map((label) => (
          <button
            key={label}
            type="button"
            className="adventure__nav-item"
            disabled
            title="尚未開放"
          >
            {label}
          </button>
        ))}
      </nav>
    </div>
  )
}

type LoopProps = {
  api: ApiClient
  playback: BattlePlayback
  onItems: (items: PlaybackItem[]) => void
}

function AdventureLoop({ api, playback, onItems }: LoopProps) {
  const { error } = useAdventureLoop({ api, playback, onItems })
  return error === null ? null : (
    <p className="adventure__error" role="status">
      {error}
    </p>
  )
}
