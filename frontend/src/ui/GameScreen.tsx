import { useEffect, useRef, useState } from "react"
import type { ApiClient } from "../api/client"
import type { CharacterVo } from "../game/character"
import type { TownFacility, TownSegment } from "../game/town"
import { RevealMilliseconds } from "../motion/timings"
import { AdventureScreen, type Tab } from "./AdventureScreen"
import type { SubPage } from "./BagScreen"
import { StageFrame } from "./stage/StageFrame"
import { RevealLayer } from "./stage/RevealLayer"
import { innRevealOrigin } from "../game/inn"
import { InnScreen } from "./inn/InnScreen"
import { TownScreen } from "./town/TownScreen"

// 旅館是新畫面；其他設施先接舊外殼：停在對應分頁（背包分頁再停在對應子頁）
const facilityViews: Record<Exclude<TownFacility, "inn">, { tab: Tab; bagPage: SubPage }> = {
  character: { tab: "character", bagPage: "bag" },
  bag: { tab: "bag", bagPage: "bag" },
  companions: { tab: "companions", bagPage: "bag" },
  adventure: { tab: "adventure", bagPage: "bag" },
  shop: { tab: "bag", bagPage: "shop" },
  forge: { tab: "bag", bagPage: "forge" },
  honour: { tab: "bag", bagPage: "honour" },
  missions: { tab: "missions", bagPage: "bag" },
}

// 旅館展開與收回時城鎮留在底下；完全展開後才卸載城鎮，收回時以市集段重新顯示
type InnPhase = "opening" | "open" | "closing"
type View =
  | { name: "town"; segment: TownSegment }
  | { name: "inn"; phase: InnPhase }
  | { name: "shell"; tab: Tab; bagPage: SubPage; shell: number }

type Props = { api: ApiClient; initialCharacter: CharacterVo; onSignOut: () => void }

// 進遊戲後先在城鎮（不連線）；點設施打開舊外殼開始掛機，回城鎮時舊外殼卸載、輪詢停止
// 卸載前送出的請求回來時仍會交出角色，城鎮跟著更新
export const GameScreen = ({ api, initialCharacter, onSignOut }: Props) => {
  const [character, setCharacter] = useState(initialCharacter)
  const [view, setView] = useState<View>({ name: "town", segment: 0 })
  // 每打開一次舊外殼加一；回城鎮後才回來的回應只收最後打開的那次，較早的不蓋掉較新的角色
  const latestShell = useRef(0)

  const open = (facility: TownFacility) => {
    if (facility === "inn") {
      setView({ name: "inn", phase: "opening" })
      return
    }
    latestShell.current += 1
    setView({ name: "shell", ...facilityViews[facility], shell: latestShell.current })
  }

  const innPhase = view.name === "inn" ? view.phase : null
  useEffect(() => {
    if (innPhase !== "opening" && innPhase !== "closing") return
    const timer = setTimeout(
      () =>
        setView(
          innPhase === "opening" ? { name: "inn", phase: "open" } : { name: "town", segment: 1 },
        ),
      RevealMilliseconds,
    )
    return () => clearTimeout(timer)
  }, [innPhase])

  if (view.name === "town" || view.name === "inn") {
    const showTown = view.name === "town" || view.phase !== "open"
    return (
      <StageFrame>
        {showTown && (
          <div inert={view.name === "inn"}>
            <TownScreen
              api={api}
              character={character}
              onOpen={open}
              initialSegment={view.name === "town" ? view.segment : 1}
            />
          </div>
        )}
        {view.name === "inn" && (
          <RevealLayer origin={innRevealOrigin} closing={view.phase === "closing"}>
            <InnScreen
              api={api}
              character={character}
              onCharacterChange={setCharacter}
              onBack={() => setView({ name: "inn", phase: "closing" })}
            />
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
