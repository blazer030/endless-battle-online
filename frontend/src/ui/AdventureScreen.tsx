import { lazy, Suspense, useCallback, useEffect, useRef, useState } from "react"
import type { ApiClient } from "../api/client"
import { describePlaybackItem, MaxLogLines } from "../game/battleLog"
import type { LogEntry, LogLine } from "../game/battleLog"
import { BattlePlayback } from "../game/battlePlayback"
import type { PlaybackItem } from "../game/battlePlayback"
import { refreshSignalsFor } from "../game/refreshSignals"
import { applyToStage, initialStageState, stageEffectsFor } from "../game/stageModel"
import { emptyPublicNames, type PublicNames } from "../game/publicNames"
import type { AdvanceVo } from "../game/battleEvents"
import type { CharacterVo } from "../game/character"
import { locationTitle, passStatus } from "../game/world"
import { LevelUpHintMilliseconds } from "../motion/timings"
import { StageHeight } from "../render/stageLayout"
import type { StageHandle } from "../render/StageRenderer"
import { BagScreen } from "./BagScreen"
import { BattleLog } from "./BattleLog"
import { CharacterScreen } from "./CharacterScreen"
import { CompanionsScreen } from "./CompanionsScreen"
import { formatNumber } from "../game/format"
import { MissionsScreen } from "./MissionsScreen"
import { SettingsPanel } from "./SettingsPanel"
import { SkillsScreen } from "./SkillsScreen"
import { StageErrorBoundary } from "./StageErrorBoundary"
import { StatBar } from "./StatBar"
import { useAdventureLoop } from "./useAdventureLoop"
import { WorldPanel } from "./WorldPanel"
import "./AdventureScreen.css"

// Pixi 與 GSAP 拆成獨立 chunk，舞台載入前先用同高的空白占位
const PixiStage = lazy(() =>
  import("../render/PixiStage").then((module) => ({ default: module.PixiStage })),
)

type Tab = "adventure" | "character" | "skills" | "bag" | "missions" | "companions"

const tabs: { id: Tab; label: string }[] = [
  { id: "adventure", label: "冒險" },
  { id: "character", label: "角色" },
  { id: "skills", label: "技能" },
  { id: "bag", label: "背包" },
  { id: "missions", label: "任務" },
  { id: "companions", label: "同伴" },
]

type AreaPass = {
  expiresAt: Date | null
  at: number
  placeId: CharacterVo["placeId"]
  subPlace: CharacterVo["subPlace"]
}

// onSignOut：玩家在角色頁按登出
type Props = { api: ApiClient; initialCharacter: CharacterVo; onSignOut: () => void }

export function AdventureScreen({ api, initialCharacter, onSignOut }: Props) {
  const [names, setNames] = useState<PublicNames>(emptyPublicNames)
  const [playback] = useState(() => new BattlePlayback(initialCharacter))
  const [character, setCharacter] = useState(initialCharacter)
  const [lines, setLines] = useState<LogLine[]>([])
  const [tab, setTab] = useState<Tab>("adventure")
  const [worldOpen, setWorldOpen] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  // 升級事件播出時在頂欄短暫顯示提示，值是剛升上的等級
  const [levelUpHint, setLevelUpHint] = useState<number | null>(null)
  // 最近一次推進回應的禁區通行證與它所屬的地點；每次回應都換新物件，剩餘分鐘跟著輪詢重算
  const [areaPass, setAreaPass] = useState<AreaPass>({
    expiresAt: null,
    at: 0,
    placeId: initialCharacter.placeId,
    subPlace: initialCharacter.subPlace,
  })
  // 戰報讓開著的分頁過時就遞增，分頁自己節流重載
  const [refreshSignals, setRefreshSignals] = useState({ bag: 0, missions: 0, skills: 0 })
  const stageHandle = useRef<StageHandle>(null)
  // 舞台狀態只是播放進度，不需要觸發重繪
  const stageState = useRef(initialStageState)
  const currentMonsterName = useRef<string | null>(null)
  const nextLineId = useRef(0)

  // 舞台 chunk 載入期間 handle 還不存在，接上時補播目前這隻怪物與血量
  const attachStage = useCallback((handle: StageHandle | null) => {
    stageHandle.current = handle
    const state = stageState.current
    if (handle === null || state.monsterId === null || currentMonsterName.current === null) return
    handle.showEncounter(state.monsterId, currentMonsterName.current, state)
    handle.updateHealth(state)
  }, [])

  useEffect(() => {
    let isCurrent = true
    // 名稱表載入失敗時以 id 顯示，不擋住冒險
    Promise.all([api.getPublicJobs(), api.getCreationOptions()]).then(
      ([jobs, options]) => {
        if (isCurrent)
          setNames({
            jobs: jobs.map(({ id, name }) => ({ id, name })),
            sexes: options.sexes,
            elements: options.elements,
            births: options.births,
          })
      },
      () => undefined,
    )
    return () => {
      isCurrent = false
    }
  }, [api])

  useEffect(() => {
    if (levelUpHint === null) return
    const timer = setTimeout(() => setLevelUpHint(null), LevelUpHintMilliseconds)
    return () => clearTimeout(timer)
  }, [levelUpHint])

  const appendLines = (entries: LogEntry[]) => {
    const added = entries.map((entry) => ({ id: nextLineId.current++, ...entry }))
    setLines((previous) => [...previous, ...added].slice(-MaxLogLines))
  }

  // 轉職、學技能、移動回傳的最新角色；轉職與移動時在戰報留一行本地訊息
  const handleCharacterChange = (updated: CharacterVo) => {
    const previous = playback.displayedCharacter
    const moved = updated.placeId !== previous.placeId || updated.subPlace !== previous.subPlace
    playback.confirm(updated)
    // 舊地區還沒播的戰鬥不再接在「你前往」後面
    if (moved) playback.discardQueued()
    setCharacter(playback.displayedCharacter)
    const notes: LogEntry[] = []
    if (updated.jobId !== previous.jobId)
      notes.push({ text: `你轉職為${updated.jobName}。`, kind: "muted" })
    if (moved) notes.push({ text: `你前往${locationTitle(updated)}。`, kind: "muted" })
    if (notes.length > 0) appendLines(notes)
  }

  const handleItems = (items: PlaybackItem[]) => {
    const added: LogEntry[] = []
    for (const item of items) {
      if (item.kind === "event") {
        const { event } = item
        const previousState = stageState.current
        stageState.current = applyToStage(previousState, event)
        if (event.type === "encounter") {
          currentMonsterName.current = event.monsterName
          stageHandle.current?.showEncounter(
            event.monsterId,
            currentMonsterName.current,
            stageState.current,
          )
        } else if (stageState.current !== previousState) {
          stageHandle.current?.updateHealth(stageState.current)
        }
        if (event.type === "levelUp") setLevelUpHint(event.newLevel)
        stageHandle.current?.play(stageEffectsFor(event))
      }
      added.push(
        ...describePlaybackItem(
          item,
          stageState.current.monsterId === null ? null : currentMonsterName.current,
          playback.displayedCharacter.name,
        ),
      )
    }
    appendLines(added)
    setCharacter(playback.displayedCharacter)
    const signals = refreshSignalsFor(items)
    if (signals.bag || signals.missions || signals.skills)
      setRefreshSignals((previous) => ({
        bag: previous.bag + Number(signals.bag),
        missions: previous.missions + Number(signals.missions),
        skills: previous.skills + Number(signals.skills),
      }))
  }

  // 移動前送出的輪詢可能在移動後才回來，只顯示目前地點的通行狀態
  const areaPassText =
    areaPass.placeId === character.placeId && areaPass.subPlace === character.subPlace
      ? passStatus(areaPass.expiresAt, new Date(areaPass.at))
      : null

  return (
    <div className="adventure">
      <header className="adventure__top">
        <span className="adventure__identity">
          <span className="adventure__name">{character.name}</span>
          <span className="adventure__job">{character.jobName}</span>
          <span className="adventure__level">{`Lv ${character.level}`}</span>
          {levelUpHint !== null && (
            <span key={levelUpHint} className="adventure__level-up" role="status">
              升級！
            </span>
          )}
        </span>
        <span className="adventure__money">{`金錢 ${formatNumber(character.money)}`}</span>
        <span className="adventure__exp">
          <span className="adventure__exp-label">EXP</span>
          <StatBar
            label="EXP"
            value={character.experience}
            max={character.nextLevelExperience}
            tone="exp"
          />
          <span className="adventure__exp-value">{`${formatNumber(character.experience)} / ${formatNumber(character.nextLevelExperience)}`}</span>
        </span>
      </header>
      {/* 放在分頁外，連線錯誤在每個分頁都看得到 */}
      <AdventureLoop
        api={api}
        playback={playback}
        onItems={handleItems}
        onResponse={(response: AdvanceVo) =>
          setAreaPass({
            expiresAt: response.areaPassExpiresAt,
            at: Date.now(),
            placeId: response.character.placeId,
            subPlace: response.character.subPlace,
          })
        }
      />
      <main className="adventure__main">
        {/* 切到其他分頁時只隱藏不卸載：輪詢、舞台與戰報都繼續跑 */}
        <div className="adventure__panel" hidden={tab !== "adventure"}>
          <h1 className="adventure__stage-name">
            <button
              type="button"
              className="adventure__location"
              aria-haspopup="dialog"
              aria-expanded={worldOpen}
              onClick={() => setWorldOpen(true)}
            >
              {`${locationTitle(character)} ▾`}
            </button>
            <button
              type="button"
              className="adventure__settings"
              aria-haspopup="dialog"
              aria-expanded={settingsOpen}
              onClick={() => setSettingsOpen(true)}
            >
              設定
            </button>
          </h1>
          {areaPassText !== null && <p className="adventure__pass">{areaPassText}</p>}
          <StageErrorBoundary>
            <Suspense fallback={<div style={{ height: StageHeight }} />}>
              <PixiStage ref={attachStage} />
            </Suspense>
          </StageErrorBoundary>
          <BattleLog lines={lines} />
          {/* 每次打開才載入，推薦標籤跟著目前等級 */}
          {worldOpen && (
            <WorldPanel
              api={api}
              character={character}
              onCharacterChange={handleCharacterChange}
              onClose={() => setWorldOpen(false)}
            />
          )}
          {settingsOpen && (
            <SettingsPanel
              api={api}
              character={character}
              onCharacterChanged={handleCharacterChange}
              onClose={() => setSettingsOpen(false)}
            />
          )}
        </div>
        {/* 角色分頁每次打開才載入密藥效果，剩餘時間是最新的 */}
        {tab === "character" && (
          <div className="adventure__sheet">
            <CharacterScreen api={api} character={character} names={names} onSignOut={onSignOut} />
          </div>
        )}
        {/* 技能分頁每次打開才載入，轉職條件與熟練度都是最新的 */}
        {tab === "skills" && (
          <div className="adventure__sheet">
            <SkillsScreen
              api={api}
              character={character}
              onCharacterChange={handleCharacterChange}
              refreshSignal={refreshSignals.skills}
            />
          </div>
        )}
        {/* 背包分頁每次打開才載入，背包、商店與倉庫都是最新的 */}
        {tab === "bag" && (
          <div className="adventure__sheet">
            <BagScreen
              api={api}
              character={character}
              onCharacterChanged={handleCharacterChange}
              jobNames={names.jobs}
              refreshSignal={refreshSignals.bag}
            />
          </div>
        )}
        {/* 任務分頁每次打開才載入，擊殺進度與交付數量都是最新的 */}
        {tab === "missions" && (
          <div className="adventure__sheet">
            <MissionsScreen
              api={api}
              names={names}
              onCharacterChanged={handleCharacterChange}
              refreshSignal={refreshSignals.missions}
            />
          </div>
        )}
        {/* 同伴分頁每次打開才載入，寵物的飽腹與傭兵的次數都是最新的 */}
        {tab === "companions" && (
          <div className="adventure__sheet">
            <CompanionsScreen api={api} onCharacterChanged={handleCharacterChange} />
          </div>
        )}
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
      </nav>
    </div>
  )
}

type LoopProps = {
  api: ApiClient
  playback: BattlePlayback
  onItems: (items: PlaybackItem[]) => void
  onResponse: (response: AdvanceVo) => void
}

function AdventureLoop({ api, playback, onItems, onResponse }: LoopProps) {
  const { error } = useAdventureLoop({ api, playback, onItems, onResponse })
  return error === null ? null : (
    <p className="adventure__error" role="status">
      {error}
    </p>
  )
}
