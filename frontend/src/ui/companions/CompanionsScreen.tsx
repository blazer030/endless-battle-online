import { useEffect, useState, type CSSProperties } from "react"
import type { ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import {
  BartonLoadFailedGreeting,
  bartonGreeting,
  hallChip,
  MiraLoadFailedGreeting,
  miraGreeting,
  stableChip,
} from "../../game/companions"
import type { MercenaryOverviewVo } from "../../game/mercenaries"
import type { PetsVo } from "../../game/pets"
import { CompanionsEntranceFadeSeconds } from "../../motion/timings"
import { useStage } from "../stage/stageContext"
import { Seal } from "../title/Seal"
import { BackButton } from "./BackButton"
import { HallPage } from "./HallPage"
import { StablePage } from "./StablePage"
import mira from "../../assets/companions/mira.webp"
import barton from "../../assets/companions/barton.webp"
import "../inn/InnScreen.css"
import "./CompanionsScreen.css"

export type CompanionsApi = Pick<
  ApiClient,
  | "getPets"
  | "carryPet"
  | "storePet"
  | "releasePet"
  | "renamePet"
  | "feedPet"
  | "getMercenaries"
  | "hireMercenary"
  | "deployMercenary"
  | "restMercenary"
  | "renameMercenary"
  | "dismissMercenary"
  | "getWorld"
  | "getMyCharacter"
>

type Props = {
  api: CompanionsApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  // 入口按「城門」：回城鎮城門口
  onBack: () => void
}

type Page = "entrance" | "stable" | "hall"
type Entrance = { pets: PetsVo; mercenaries: MercenaryOverviewVo } | "failed" | null

// 同伴（設計板 CompanionsFit）：入口兩扇門（米菈的獸欄、巴頓的傭兵所），點門進各頁
export const CompanionsScreen = ({ api, character, onCharacterChange, onBack }: Props) => {
  const stage = useStage()
  const [page, setPage] = useState<Page>("entrance")
  // 從頁面回入口時淡入；第一次進來不淡入（外層已經圓形展開）
  const [returned, setReturned] = useState(false)
  const [entrance, setEntrance] = useState<Entrance>(null)
  const [attempt, setAttempt] = useState(0)

  // 回到入口時重讀，寵物與契約才是最新的
  useEffect(() => {
    if (page !== "entrance") return
    let isCurrent = true
    Promise.all([api.getPets(), api.getMercenaries()]).then(
      ([pets, mercenaries]) => {
        if (isCurrent) setEntrance({ pets, mercenaries })
      },
      () => {
        if (isCurrent) setEntrance("failed")
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, page, attempt])

  const open = (target: Page) => {
    setPage(target)
    setReturned(true)
  }

  const pageProps = { api, character, onCharacterChange, onBack: () => open("entrance") }
  if (page === "stable") return <StablePage {...pageProps} />
  if (page === "hall") return <HallPage {...pageProps} />

  const loaded = entrance !== null && entrance !== "failed" ? entrance : null
  const failed = entrance === "failed"
  const miraLine = failed
    ? MiraLoadFailedGreeting
    : loaded === null
      ? ""
      : miraGreeting(loaded.pets.pets)
  const bartonLine = failed
    ? BartonLoadFailedGreeting
    : loaded === null
      ? ""
      : bartonGreeting(loaded.mercenaries.contract)

  return (
    <div
      className={`companions-entrance${returned ? " companions-entrance--returned" : ""}`}
      style={
        {
          "--h": `${stage.height}px`,
          "--entrance-fade": `${CompanionsEntranceFadeSeconds}s`,
        } as CSSProperties
      }
    >
      <button
        type="button"
        className="companions-door companions-door--stable"
        aria-label="獸欄"
        onClick={() => open("stable")}
      >
        <img className="companions-door__keeper" src={mira} alt="" />
        <p className="companions-door__bubble" data-testid="mira-line">
          {miraLine}
        </p>
        <span className="companions-door__sign">
          <b>獸欄</b>
          {loaded !== null && <small>{stableChip(loaded.pets.pets)}</small>}
        </span>
      </button>
      <button
        type="button"
        className="companions-door companions-door--hall"
        aria-label="傭兵所"
        onClick={() => open("hall")}
      >
        <img className="companions-door__keeper" src={barton} alt="" />
        <p className="companions-door__bubble" data-testid="barton-line">
          {bartonLine}
        </p>
        <span className="companions-door__sign">
          <b>傭兵所</b>
          {loaded !== null && <small>{hallChip(loaded.mercenaries.contract)}</small>}
        </span>
      </button>
      <i className="companions-entrance__beam" aria-hidden="true" />
      {failed && (
        <Seal
          className="companions-entrance__retry"
          label="再試一次"
          onClick={() => {
            setEntrance(null)
            setAttempt((count) => count + 1)
          }}
        />
      )}
      <BackButton label="城門" onClick={onBack} />
    </div>
  )
}
