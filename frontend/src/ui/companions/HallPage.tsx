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

// 傭兵所：名冊便條＋木台上的契約
export const HallPage = ({ character, onBack }: Props) => {
  const stage = useStage()
  return (
    <section
      className="companions-page companions-page--hall"
      aria-label="傭兵所"
      style={{ "--pt": `${companionsCounterTop(stage.height)}px` } as CSSProperties}
    >
      <div className="companions-scene companions-scene--hall" aria-hidden="true" />
      <BackButton label="同伴" onClick={onBack} />
      <section className="inn__counter" aria-label="契約">
        <CounterHeader
          title={
            <>
              <b>傭兵所</b>
              <small>所長・巴頓</small>
            </>
          }
          line={null}
          money={character.money}
        />
      </section>
    </section>
  )
}
