import type { CSSProperties } from "react"
import type { CharacterVo } from "../../game/character"
import { companionsCounterTop } from "../../game/companions"
import { useStage } from "../stage/stageContext"
import { BackButton } from "./BackButton"
import type { CompanionsApi } from "./CompanionsScreen"
import { CounterHeader } from "./KeeperSay"

type Props = {
  api: CompanionsApi
  character: CharacterVo
  onCharacterChange: (character: CharacterVo) => void
  onBack: () => void
}

// 獸欄：三格柵門＋木台上的寵物資料與果實槽
export const StablePage = ({ character, onBack }: Props) => {
  const stage = useStage()
  return (
    <section
      className="companions-page companions-page--stable"
      aria-label="獸欄"
      style={{ "--pt": `${companionsCounterTop(stage.height)}px` } as CSSProperties}
    >
      <div className="companions-scene companions-scene--stable" aria-hidden="true" />
      <BackButton label="同伴" onClick={onBack} />
      <section className="inn__counter" aria-label="寵物">
        <CounterHeader
          title={
            <>
              <b>獸欄</b>
              <small>管理員・米菈</small>
            </>
          }
          line={null}
          money={character.money}
        />
      </section>
    </section>
  )
}
