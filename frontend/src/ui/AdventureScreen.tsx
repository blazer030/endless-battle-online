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
import { useAdventureLoop } from "./useAdventureLoop"
import "./AdventureScreen.css"

// Pixi 與 GSAP 拆成獨立 chunk，舞台載入前先用同高的空白占位
const PixiStage = lazy(() =>
  import("../render/PixiStage").then((module) => ({ default: module.PixiStage })),
)

const emptyContent: ContentNames = { monsters: [], stages: [] }

const navigation = ["本命", "經脈", "行囊"]

const format = (value: number) => value.toLocaleString("zh-TW")

type Props = { api: ApiClient; initialCharacter: CharacterView }

export function AdventureScreen({ api, initialCharacter }: Props) {
  const [content, setContent] = useState<ContentNames | null>(null)
  const [playback] = useState(() => new BattlePlayback(initialCharacter))
  const [character, setCharacter] = useState(initialCharacter)
  const [lines, setLines] = useState<LogLine[]>([])
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
        <span className="adventure__name">{character.name}</span>
        <span className="adventure__totals">
          <span>{`修為 ${format(character.experience)}`}</span>
          <span>{`靈石 ${format(character.spiritStones)}`}</span>
        </span>
      </header>
      <h1 className="adventure__stage-name">{stageName}</h1>
      <Suspense fallback={<div style={{ height: StageHeight }} />}>
        <PixiStage ref={attachStage} />
      </Suspense>
      {/* 名稱表就緒才開始輪詢，戰報與舞台才拿得到妖怪名字 */}
      {content !== null && <AdventureLoop api={api} playback={playback} onItems={handleItems} />}
      <BattleLog lines={lines} />
      <nav className="adventure__nav">
        <button type="button" className="adventure__nav-item" aria-current="page">
          歷練
        </button>
        {navigation.map((label) => (
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
