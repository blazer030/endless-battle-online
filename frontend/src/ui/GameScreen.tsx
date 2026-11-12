import { useState } from "react"
import type { ApiClient } from "../api/client"
import type { CharacterVo } from "../game/character"
import type { TownFacility } from "../game/town"
import { AdventureScreen, type Tab } from "./AdventureScreen"
import type { SubPage } from "./BagScreen"
import { StageFrame } from "./stage/StageFrame"
import { TownScreen } from "./town/TownScreen"

// 城鎮的設施先接舊外殼：停在對應分頁（背包分頁再停在對應子頁）
const facilityViews: Record<TownFacility, { tab: Tab; bagPage: SubPage }> = {
  character: { tab: "character", bagPage: "bag" },
  bag: { tab: "bag", bagPage: "bag" },
  companions: { tab: "companions", bagPage: "bag" },
  adventure: { tab: "adventure", bagPage: "bag" },
  inn: { tab: "bag", bagPage: "inn" },
  shop: { tab: "bag", bagPage: "shop" },
  forge: { tab: "bag", bagPage: "forge" },
  honour: { tab: "bag", bagPage: "honour" },
  missions: { tab: "missions", bagPage: "bag" },
}

type View = { name: "town" } | { name: "shell"; tab: Tab; bagPage: SubPage }

type Props = { api: ApiClient; initialCharacter: CharacterVo; onSignOut: () => void }

// 進遊戲後先在城鎮（不連線）；點設施打開舊外殼開始掛機，回城鎮時舊外殼卸載、輪詢停止
export const GameScreen = ({ api, initialCharacter, onSignOut }: Props) => {
  const [character, setCharacter] = useState(initialCharacter)
  const [view, setView] = useState<View>({ name: "town" })

  if (view.name === "town")
    return (
      <StageFrame>
        <TownScreen
          api={api}
          character={character}
          onOpen={(facility) => setView({ name: "shell", ...facilityViews[facility] })}
        />
      </StageFrame>
    )

  return (
    <AdventureScreen
      api={api}
      initialCharacter={character}
      initialTab={view.tab}
      initialBagPage={view.bagPage}
      onSignOut={onSignOut}
      onReturnToTown={(latest) => {
        setCharacter(latest)
        setView({ name: "town" })
      }}
    />
  )
}
