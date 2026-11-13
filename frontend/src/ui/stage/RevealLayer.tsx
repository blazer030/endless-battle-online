import type { CSSProperties, ReactNode } from "react"
import { useStage } from "./stageContext"
import "./RevealLayer.css"

export type RevealOrigin = (stageHeight: number) => { x: number; y: number }

type Props = { origin: RevealOrigin; closing: boolean; children: ReactNode }

// 從城鎮上的紙籤或名牌圓形展開的一層（旅館、同伴共用）；收回時縮回同一點
export const RevealLayer = ({ origin, closing, children }: Props) => {
  const point = origin(useStage().height)
  return (
    <div
      className={`reveal-layer${closing ? " reveal-layer--closing" : ""}`}
      style={{ "--ox": `${point.x}px`, "--oy": `${point.y}px` } as CSSProperties}
    >
      {children}
    </div>
  )
}
