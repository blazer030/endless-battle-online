import { useEffect, useRef, useState, type CSSProperties } from "react"
import { ApiError, type ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import {
  actionLabel,
  completedRegular,
  dailyNotes,
  liseLine,
  MaxActiveMissions,
  missionsLayout,
  openingPage,
  resetHours,
  type BoardPage,
  type LiseLine,
  type Mission,
  type MissionBoardVo,
  type MissionCompletionVo,
} from "../../game/missions"
import { useStage } from "../stage/stageContext"
import { BackButton } from "../companions/BackButton"
import { MissionBoard } from "./MissionBoard"
import { MissionHand } from "./MissionHand"
import { CompletedList, MissionSheet } from "./MissionSheet"
import "../companions/CompanionsScreen.css"
import "../inn/InnScreen.css"
import "../title/Seal.css"
import "./MissionsScreen.css"

export type MissionsScreenApi = Pick<
  ApiClient,
  "getMissions" | "acceptMission" | "abandonMission" | "completeMission"
>

type Props = {
  api: MissionsScreenApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  // 「公會前」：回城鎮公會前
  onBack: () => void
}

type Board = MissionBoardVo | "failed" | null

// 拉近的東西：一張委託單（從完成清單打開的，關掉回清單）或完成的委託清單
type Sheet = { kind: "mission"; id: string; fromPile: boolean } | { kind: "pile" } | null

const findMission = (board: MissionBoardVo, id: string): Mission | undefined =>
  [...board.available, ...board.active, ...board.completed].find((mission) => mission.id === id)

// 任務板（設計板 MissionsFit）：公會牆上的佈告欄（每日／一般兩頁）＋櫃檯上手邊的三張；點一張拉近成委託單
export const MissionsScreen = ({ api, character, onCharacterChange, onBack }: Props) => {
  const stage = useStage()
  const [board, setBoard] = useState<Board>(null)
  const [attempt, setAttempt] = useState(0)
  const [page, setPage] = useState<BoardPage>("daily")
  const [sheet, setSheet] = useState<Sheet>(null)
  // 第一次讀到任務板時選頁；玩家自己換過頁就不再改
  const pageChosen = useRef(false)
  // 送出中、莉瑟在委託單裡說的話、放棄的再確認、剛完成的那一筆
  const [busy, setBusy] = useState(false)
  const [say, setSay] = useState<LiseLine | null>(null)
  const [asking, setAsking] = useState(false)
  const [result, setResult] = useState<{ id: string; value: MissionCompletionVo } | null>(null)
  const alive = useRef(true)
  // 每次送出或重讀加一；回來時編號已經不是最新的就丟掉，晚到的舊任務板不會蓋掉之後的結果
  const latestRequest = useRef(0)

  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
    }
  }, [])

  useEffect(() => {
    let isCurrent = true
    api.getMissions().then(
      (loaded) => {
        if (!isCurrent) return
        setBoard(loaded)
        if (!pageChosen.current) {
          pageChosen.current = true
          setPage(openingPage(loaded))
        }
      },
      () => {
        if (isCurrent) setBoard("failed")
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt])

  const loaded = board !== null && board !== "failed" ? board : null
  const layout = missionsLayout(stage.height)
  const handLimit = loaded?.activeLimit ?? MaxActiveMissions
  const sheetMission =
    loaded !== null && sheet?.kind === "mission" ? findMission(loaded, sheet.id) : undefined

  const resetText =
    loaded === null ? "" : `${resetHours(loaded.nextDailyReset, new Date())} 小時後重置`

  // 換一張委託單、關掉、清單：上一張的傳話、再確認、完成結果都收掉
  const showSheet = (next: Sheet) => {
    setSay(null)
    setAsking(false)
    setResult(null)
    setSheet(next)
  }

  const closeSheet = () => {
    showSheet(sheet?.kind === "mission" && sheet.fromPile ? { kind: "pile" } : null)
  }

  const pick = (id: string) => {
    // 送出中不換委託單，回來的結果才會說在送出的那一張裡
    if (!busy) showSheet({ kind: "mission", id, fromPile: false })
  }

  // 同一時間只送一個請求；成功換成回傳的任務板、委託單留著；失敗時莉瑟說原因並重讀任務板
  const send = async (
    mission: Mission,
    request: () => Promise<MissionBoardVo>,
    success: LiseLine | null,
  ) => {
    setBusy(true)
    setSay(null)
    setAsking(false)
    latestRequest.current += 1
    try {
      const next = await request()
      if (!alive.current) return
      setBoard(next)
      setSay(success)
    } catch (error) {
      if (!alive.current) return
      setSay(
        liseLine(
          error instanceof ApiError && error.reasons.length > 0
            ? { kind: "refused", reasons: error.reasons, mission, activeLimit: handLimit }
            : { kind: "failed" },
        ),
      )
      const reload = ++latestRequest.current
      api.getMissions().then(
        (latest) => {
          if (alive.current && reload === latestRequest.current) setBoard(latest)
        },
        () => undefined,
      )
    } finally {
      if (alive.current) setBusy(false)
    }
  }

  const accept = (mission: Mission) =>
    send(mission, () => api.acceptMission(mission.id), liseLine({ kind: "accepted", mission }))

  const abandon = (mission: Mission) =>
    send(mission, () => api.abandonMission(mission.id), liseLine({ kind: "abandoned" }))

  const complete = (mission: Mission) =>
    send(
      mission,
      async () => {
        const value = await api.completeMission(mission.id)
        if (alive.current) {
          setResult({ id: mission.id, value })
          onCharacterChange(value.character)
        }
        return value.board
      },
      null,
    )

  const sheetView = (current: MissionBoardVo, mission: Mission) =>
    current.completed.includes(mission)
      ? "done"
      : current.active.includes(mission)
        ? "active"
        : "open"

  // 印章：可接的接取（手邊滿了停用）；手邊的交付或完成（還沒達成停用）
  const sealState = (current: MissionBoardVo, mission: Mission) => {
    const open = sheetView(current, mission) === "open"
    const full = current.active.length >= current.activeLimit
    return {
      open,
      seal: open ? "接取" : actionLabel(mission),
      sealDisabled: open ? full : !mission.ready,
      note: open && full ? `手邊已有 ${current.activeLimit} 張` : null,
    }
  }

  return (
    <div
      className="missions"
      style={
        {
          "--h": `${stage.height}px`,
          "--board-height": `${layout.boardHeight}px`,
          "--row-height": `${layout.rowHeight}px`,
          "--hand-top": `${layout.handTop}px`,
          "--hand-height": `${layout.handHeight}px`,
        } as CSSProperties
      }
    >
      <BackButton label="公會前" disabled={busy} onClick={onBack} />
      {loaded !== null && page === "daily" && dailyNotes(loaded).length > 0 && (
        <div className="missions__reset">
          <b>{resetText}</b>
        </div>
      )}
      <MissionBoard
        board={board}
        page={page}
        level={character.level}
        onPage={(next) => {
          pageChosen.current = true
          setPage(next)
        }}
        onPick={pick}
        onPile={() => {
          if (!busy) showSheet({ kind: "pile" })
        }}
        onRetry={() => {
          setBoard(null)
          setAttempt((count) => count + 1)
        }}
      />
      <div className="missions__counter" aria-hidden="true" />
      <div className="missions__plank" aria-hidden="true" />
      <div className="missions__tag">
        進行中<b data-testid="hand-count">{loaded === null ? "" : loaded.active.length}</b> /{" "}
        {handLimit}
      </div>
      <MissionHand active={loaded?.active ?? null} limit={handLimit} onPick={pick} />
      {loaded !== null && sheet?.kind === "pile" && (
        <CompletedList
          missions={completedRegular(loaded)}
          onPick={(id) => showSheet({ kind: "mission", id, fromPile: true })}
          onClose={() => showSheet(null)}
        />
      )}
      {loaded !== null && sheetMission !== undefined && (
        <MissionSheet
          mission={sheetMission}
          view={sheetView(loaded, sheetMission)}
          resetText={resetText}
          result={result?.id === sheetMission.id ? result.value : null}
          say={say}
          actions={{
            ...sealState(loaded, sheetMission),
            busy,
            asking,
            onSeal: () => {
              void (sealState(loaded, sheetMission).open
                ? accept(sheetMission)
                : complete(sheetMission))
            },
            onAsk: () => {
              setSay(null)
              setAsking(true)
            },
            onCancelAsk: () => setAsking(false),
            onAbandon: () => {
              void abandon(sheetMission)
            },
          }}
          onClose={closeSheet}
        />
      )}
    </div>
  )
}
