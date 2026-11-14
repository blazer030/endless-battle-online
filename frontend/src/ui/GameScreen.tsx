import { useEffect, useRef, useState, type CSSProperties, type ReactNode } from "react"
import type { ApiClient } from "../api/client"
import type { CharacterVo } from "../game/character"
import type { TownFacility, TownSegment } from "../game/town"
import { RevealMilliseconds } from "../motion/timings"
import { AdventureScreen, type Tab } from "./AdventureScreen"
import type { SubPage } from "./BagScreen"
import { StageFrame } from "./stage/StageFrame"
import { preloadImages } from "./stage/preloadImages"
import { companionsImages, honourImages, innImages, townImages } from "./stage/screenImages"
import { timeOfDay } from "../game/timeOfDay"
import { RevealLayer, type RevealOrigin } from "./stage/RevealLayer"
import "./stage/RevealLayer.css"
import "./GameScreen.css"
import { innRevealOrigin } from "../game/inn"
import { companionsRevealOrigin } from "../game/companions"
import { honourRevealOrigin } from "../game/honour"
import { InnScreen } from "./inn/InnScreen"
import { CompanionsScreen } from "./companions/CompanionsScreen"
import { HonourScreen } from "./honour/HonourScreen"
import { TownScreen } from "./town/TownScreen"

// 新畫面從城鎮展開：旅館從市集的名牌、同伴從城門口的紙籤、勳章所從公會前的紙籤；收回後城鎮停在原本的段落
type Revealed = "inn" | "companions" | "honour"
const revealed: Record<Revealed, { origin: RevealOrigin; segment: TownSegment }> = {
  inn: { origin: innRevealOrigin, segment: 1 },
  companions: { origin: companionsRevealOrigin, segment: 0 },
  honour: { origin: honourRevealOrigin, segment: 2 },
}

const isRevealed = (facility: TownFacility): facility is Revealed => facility in revealed

// 其他設施先接舊外殼：停在對應分頁（背包分頁再停在對應子頁）
const facilityViews: Record<Exclude<TownFacility, Revealed>, { tab: Tab; bagPage: SubPage }> = {
  character: { tab: "character", bagPage: "bag" },
  bag: { tab: "bag", bagPage: "bag" },
  adventure: { tab: "adventure", bagPage: "bag" },
  shop: { tab: "bag", bagPage: "shop" },
  forge: { tab: "bag", bagPage: "forge" },
  missions: { tab: "missions", bagPage: "bag" },
}

// 展開與收回時城鎮留在底下；完全展開後才卸載城鎮，收回時以原本的段落重新顯示
type RevealPhase = "opening" | "open" | "closing"
type View =
  | { name: "town"; segment: TownSegment }
  | { name: "revealed"; facility: Revealed; phase: RevealPhase }
  | { name: "shell"; tab: Tab; bagPage: SubPage; shell: number }

type Props = {
  api: ApiClient
  initialCharacter: CharacterVo
  onSignOut: () => void
  // 進城的過場（歡迎回來、踏上旅途）：墊在城鎮底下，城鎮從舞台正中間圓形展開蓋過它
  arrival?: ReactNode
}

// 進城時城鎮的圓心在舞台正中間
const arrivalReveal = { "--ox": "50%", "--oy": "50%" } as CSSProperties

// 進遊戲後先在城鎮（不連線）；點設施打開舊外殼開始掛機，回城鎮時舊外殼卸載、輪詢停止
// 卸載前送出的請求回來時仍會交出角色，城鎮跟著更新
export const GameScreen = ({ api, initialCharacter, onSignOut, arrival }: Props) => {
  const [character, setCharacter] = useState(initialCharacter)
  const [arriving, setArriving] = useState(arrival !== undefined)
  useEffect(() => {
    if (!arriving) return
    const timer = setTimeout(() => setArriving(false), RevealMilliseconds)
    return () => clearTimeout(timer)
  }, [arriving])
  const [view, setView] = useState<View>({ name: "town", segment: 0 })
  // 在城鎮時先把旅館、同伴與勳章所的圖載好，第一次打開時不會從空的慢慢冒出來；
  // 等城鎮的圖（過場時已開始載，同一張只載一次）載完才開始，慢網路時不跟城鎮搶頻寬
  useEffect(() => {
    let isCurrent = true
    void preloadImages(townImages(timeOfDay(new Date().getHours()))).then(() => {
      if (isCurrent) void preloadImages([...innImages(), ...companionsImages(), ...honourImages()])
    })
    return () => {
      isCurrent = false
    }
  }, [])
  // 每打開一次舊外殼加一；回城鎮後才回來的回應只收最後打開的那次，較早的不蓋掉較新的角色
  const latestShell = useRef(0)

  const open = (facility: TownFacility) => {
    if (isRevealed(facility)) {
      setView({ name: "revealed", facility, phase: "opening" })
      return
    }
    latestShell.current += 1
    setView({ name: "shell", ...facilityViews[facility], shell: latestShell.current })
  }

  const revealedFacility = view.name === "revealed" ? view.facility : null
  const revealPhase = view.name === "revealed" ? view.phase : null
  useEffect(() => {
    if (revealedFacility === null || (revealPhase !== "opening" && revealPhase !== "closing"))
      return
    const timer = setTimeout(
      () =>
        setView(
          revealPhase === "opening"
            ? { name: "revealed", facility: revealedFacility, phase: "open" }
            : { name: "town", segment: revealed[revealedFacility].segment },
        ),
      RevealMilliseconds,
    )
    return () => clearTimeout(timer)
  }, [revealedFacility, revealPhase])

  if (view.name === "town" || view.name === "revealed") {
    const showTown = view.name === "town" || view.phase !== "open"
    const close = () => {
      if (view.name === "revealed") setView({ ...view, phase: "closing" })
    }
    return (
      <StageFrame>
        {arriving && <div className="game-arrival">{arrival}</div>}
        {showTown && (
          // 進城展開時套圓形展開的樣式；展開完只拿掉樣式，城鎮不重新掛載
          <div
            inert={view.name === "revealed"}
            className={arriving ? "reveal-layer" : undefined}
            style={arriving ? arrivalReveal : undefined}
          >
            <TownScreen
              api={api}
              character={character}
              onOpen={open}
              initialSegment={view.name === "town" ? view.segment : revealed[view.facility].segment}
            />
          </div>
        )}
        {view.name === "revealed" && (
          <RevealLayer origin={revealed[view.facility].origin} closing={view.phase === "closing"}>
            {view.facility === "inn" ? (
              <InnScreen
                api={api}
                character={character}
                onCharacterChange={setCharacter}
                onBack={close}
              />
            ) : view.facility === "companions" ? (
              <CompanionsScreen
                api={api}
                character={character}
                onCharacterChange={setCharacter}
                onBack={close}
              />
            ) : (
              <HonourScreen
                api={api}
                character={character}
                onCharacterChange={setCharacter}
                onBack={close}
              />
            )}
          </RevealLayer>
        )}
      </StageFrame>
    )
  }

  const { shell } = view
  return (
    <AdventureScreen
      api={api}
      initialCharacter={character}
      initialTab={view.tab}
      initialBagPage={view.bagPage}
      onSignOut={onSignOut}
      onReturnToTown={(latest) => {
        setCharacter(latest)
        setView({ name: "town", segment: 0 })
      }}
      onConfirmedCharacter={(confirmed) => {
        if (shell === latestShell.current) setCharacter(confirmed)
      }}
    />
  )
}
