import type { CSSProperties, ReactNode } from "react"
import { innRevealOrigin } from "../../game/inn"
import { useStage } from "../stage/stageContext"
import "./InnReveal.css"

type Props = { closing: boolean; children: ReactNode }

// 旅館從市集的旅館名牌圓形展開；收回時縮回同一點
export const InnReveal = ({ closing, children }: Props) => {
  const origin = innRevealOrigin(useStage().height)
  return (
    <div
      className={`inn-reveal${closing ? " inn-reveal--closing" : ""}`}
      style={{ "--ox": `${origin.x}px`, "--oy": `${origin.y}px` } as CSSProperties}
    >
      {children}
    </div>
  )
}
