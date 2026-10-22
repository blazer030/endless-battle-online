import { useState } from "react"
import type { CharacterView } from "../game/types"
import "./BagScreen.css"
import "./CompanionsScreen.css"
import { Tabs } from "./bagWidgets"
import { MercenaryPage, type MercenaryApi } from "./MercenaryPage"
import { PetsPage, type PetsApi } from "./PetsPage"

export type CompanionsApi = PetsApi & MercenaryApi

type Props = {
  api: CompanionsApi
  onCharacterChanged: (character: CharacterView) => void
}

type SubPage = "pets" | "mercenary"

const subPages: { id: SubPage; label: string }[] = [
  { id: "pets", label: "寵物" },
  { id: "mercenary", label: "傭兵" },
]

export function CompanionsScreen({ api, onCharacterChanged }: Props) {
  const [page, setPage] = useState<SubPage>("pets")
  return (
    <div className="companions">
      <Tabs
        label="同伴分頁"
        options={subPages}
        selected={page}
        onSelect={setPage}
        variant="pages"
      />
      {/* 子頁每次切換都重新掛載並載入 */}
      {page === "pets" ? (
        <PetsPage api={api} onCharacterChanged={onCharacterChanged} />
      ) : (
        <MercenaryPage api={api} onCharacterChanged={onCharacterChanged} />
      )}
    </div>
  )
}
