import { createContext, useContext } from "react"
import { StageMaximumHeight } from "./fitStage"

export type StageMetrics = {
  // 舞台本身（縮放前）的高度
  height: number
  // 舞台 px → 視窗 px 的倍率
  scale: number
}

export const StageContext = createContext<StageMetrics>({ height: StageMaximumHeight, scale: 1 })

export const useStage = () => useContext(StageContext)
