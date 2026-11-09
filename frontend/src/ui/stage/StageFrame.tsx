import { useEffect, useState, type CSSProperties, type ReactNode } from "react"
import { fitStage } from "./fitStage"
import "./StageFrame.css"

// 行動瀏覽器的網址列、鍵盤會改變可見範圍，有 visualViewport 時以它為準
const measureWindow = () => {
  const viewport = window.visualViewport
  return viewport
    ? { width: viewport.width, height: viewport.height }
    : { width: window.innerWidth, height: window.innerHeight }
}

// 新畫面的外框：舞台寬 390、高 683～722，依視窗等比縮放置中；舞台外是共用背景，舞台內超出的部分裁掉
export const StageFrame = ({ children }: { children: ReactNode }) => {
  const [size, setSize] = useState(measureWindow)

  useEffect(() => {
    const update = () => setSize(measureWindow())
    window.addEventListener("resize", update)
    window.visualViewport?.addEventListener("resize", update)
    return () => {
      window.removeEventListener("resize", update)
      window.visualViewport?.removeEventListener("resize", update)
    }
  }, [])

  const fit = fitStage(size.width, size.height)
  const style = {
    "--stage-h": `${fit.stageHeight}px`,
    transform: `translate(${fit.offsetX}px, ${fit.offsetY}px) scale(${fit.scale})`,
  } as CSSProperties

  return (
    <div className="stage-frame">
      <div className="stage" style={style}>
        {children}
      </div>
    </div>
  )
}
