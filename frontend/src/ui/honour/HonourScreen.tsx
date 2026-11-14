import { useEffect, useState, type CSSProperties } from "react"
import type { ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { clerkGreeting, medalCase, rackHeight, type CounterState } from "../../game/honour"
import type { HonourShopVo } from "../../game/honour"
import { ContentImage } from "../ContentImage"
import { BackButton } from "../companions/BackButton"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import { HonourRack } from "./HonourRack"
import clerk from "../../assets/title/clerk.webp"
import "../inn/InnScreen.css"
import "./HonourScreen.css"

export type HonourScreenApi = Pick<ApiClient, "getHonourShop" | "exchangeHonour" | "getMyCharacter">

type Props = {
  api: HonourScreenApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  // 櫃檯按「公會前」：回城鎮公會前
  onBack: () => void
}

type Shop = HonourShopVo | "failed" | null

// 勳章所（設計板 HonourFit）：櫃檯的莉瑟與勳章盒；點一枚勳章，陳列板降下來掛著它換得到的吊牌
export const HonourScreen = ({ api, onBack }: Props) => {
  const stage = useStage()
  const [shop, setShop] = useState<Shop>(null)
  const [attempt, setAttempt] = useState(0)
  // null＝櫃檯；有值＝陳列板正在看這種勳章
  const [medalId, setMedalId] = useState<string | null>(null)
  const [visit, setVisit] = useState<CounterState["visit"]>("first")
  // 打開的吊牌（羊皮紙詳情）
  const [, setSheetId] = useState<string | null>(null)

  useEffect(() => {
    let isCurrent = true
    api.getHonourShop().then(
      (loaded) => {
        if (isCurrent) setShop(loaded)
      },
      () => {
        if (isCurrent) setShop("failed")
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, attempt])

  const loaded = shop !== null && shop !== "failed" ? shop : null
  const layout = medalCase(stage.height)
  const greeting = clerkGreeting({
    load: shop === null ? "loading" : shop === "failed" ? "failed" : "ready",
    medals: loaded?.medals.reduce((total, medal) => total + medal.owned, 0) ?? 0,
    visit,
  })
  const atCounter = medalId === null
  const medal = loaded?.medals.find((entry) => entry.itemId === medalId)

  const backToCounter = () => {
    setMedalId(null)
    setVisit((current) => (current === "exchanged" ? current : "browsed"))
  }

  return (
    <div
      className={`honour${atCounter ? " honour--counter" : ""}`}
      style={
        {
          "--h": `${stage.height}px`,
          "--case-top": `${layout.top}px`,
          "--case-width": `${layout.width}px`,
          "--case-height": `${layout.height}px`,
          "--rack-height": `${rackHeight(stage.height)}px`,
        } as CSSProperties
      }
    >
      <img className="honour__clerk" src={clerk} alt="" aria-hidden={!atCounter} />
      {atCounter && (
        <p
          className={`honour__bubble${greeting.failed ? " honour__bubble--failed" : ""}`}
          role="status"
          data-testid="clerk-line"
        >
          {greeting.text}
        </p>
      )}
      {atCounter && shop === "failed" && (
        <Seal
          className="honour__retry"
          label="再試一次"
          onClick={() => {
            setShop(null)
            setAttempt((count) => count + 1)
          }}
        />
      )}

      {loaded !== null && medal !== undefined && (
        <HonourRack shop={loaded} medal={medal} onPick={setSheetId} />
      )}

      <div className="honour__counter" aria-hidden="true" />
      <div className="honour__plank" aria-hidden="true" />
      <div className="honour-case" role="group" aria-label="持有勳章">
        {(loaded?.medals ?? placeholderMedals).map((entry, index) => (
          <button
            key={entry.itemId}
            type="button"
            className={`honour-case__medal${entry.itemId === medalId ? " honour-case__medal--on" : ""}${loaded !== null && entry.owned === 0 ? " honour-case__medal--zero" : ""}${loaded === null ? " honour-case__medal--wait" : ""}`}
            style={{ left: `${medalSlots[index] ?? 0}%` }}
            aria-label={`${entry.name} ${loaded === null ? "" : entry.owned}`.trim()}
            aria-pressed={entry.itemId === medalId}
            disabled={loaded === null}
            onClick={() => setMedalId(entry.itemId)}
          >
            {loaded !== null && <ContentImage category="items" assetKey={entry.itemId} alt="" />}
            <span className="honour-case__count">{loaded === null ? "" : entry.owned}</span>
          </button>
        ))}
      </div>
      {loaded?.hiredMercenaryName != null && (
        <div className="honour__hired">
          僱用中<b>{loaded.hiredMercenaryName}</b>
        </div>
      )}

      {atCounter ? (
        <BackButton label="公會前" onClick={onBack} />
      ) : (
        <BackButton label="櫃檯" onClick={backToCounter} />
      )}
    </div>
  )
}

// 勳章盒五格的左緣（盒寬的百分比，照設計稿）
const medalSlots = [5.9, 24.3, 42, 59.7, 77.6]

// 還沒讀到時勳章盒照樣畫五格（不寫數字、不能點）
const placeholderMedals = medalSlots.map((_, index) => ({
  itemId: `pending-${index}`,
  name: "勳章",
  owned: 0,
}))
