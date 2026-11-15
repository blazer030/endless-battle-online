import { useEffect, useRef, useState, type CSSProperties } from "react"
import type { ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import {
  completedRegular,
  dailyNotes,
  missionsLayout,
  openingPage,
  resetHours,
  type BoardPage,
  type Mission,
  type MissionBoardVo,
} from "../../game/missions"
import { useStage } from "../stage/stageContext"
import { BackButton } from "../companions/BackButton"
import { MissionBoard } from "./MissionBoard"
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
export const MissionsScreen = ({ api, character, onBack }: Props) => {
  const stage = useStage()
  const [board, setBoard] = useState<Board>(null)
  const [attempt, setAttempt] = useState(0)
  const [page, setPage] = useState<BoardPage>("daily")
  const [sheet, setSheet] = useState<Sheet>(null)
  // 第一次讀到任務板時選頁；玩家自己換過頁就不再改
  const pageChosen = useRef(false)

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
  const sheetMission =
    loaded !== null && sheet?.kind === "mission" ? findMission(loaded, sheet.id) : undefined

  const closeSheet = () => {
    setSheet(sheet?.kind === "mission" && sheet.fromPile ? { kind: "pile" } : null)
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
      <BackButton label="公會前" onClick={onBack} />
      {loaded !== null && page === "daily" && dailyNotes(loaded).length > 0 && (
        <div className="missions__reset">
          <b>{`${resetHours(loaded.nextDailyReset, new Date())} 小時後重置`}</b>
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
        onPick={(id) => setSheet({ kind: "mission", id, fromPile: false })}
        onPile={() => setSheet({ kind: "pile" })}
        onRetry={() => {
          setBoard(null)
          setAttempt((count) => count + 1)
        }}
      />
      <div className="missions__counter" aria-hidden="true" />
      <div className="missions__plank" aria-hidden="true" />
      <div className="missions__tag">
        進行中<b data-testid="hand-count">{loaded === null ? "" : loaded.active.length}</b> / 3
      </div>
      {loaded !== null && sheet?.kind === "pile" && (
        <CompletedList
          missions={completedRegular(loaded)}
          onPick={(id) => setSheet({ kind: "mission", id, fromPile: true })}
          onClose={() => setSheet(null)}
        />
      )}
      {sheetMission !== undefined && (
        <MissionSheet
          mission={sheetMission}
          completed={loaded?.completed.includes(sheetMission) ?? false}
          onClose={closeSheet}
        />
      )}
    </div>
  )
}
