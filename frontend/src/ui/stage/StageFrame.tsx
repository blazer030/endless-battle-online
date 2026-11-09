import { useEffect, useState, type CSSProperties, type ReactNode } from "react"
import { fitStage } from "./fitStage"
import "./StageFrame.css"

// 用版面視窗（innerWidth／innerHeight）：網址列伸縮、旋轉會變；雙指放大、鍵盤彈出不會變，舞台才不會跟著縮
const measureWindow = () => ({ width: window.innerWidth, height: window.innerHeight })

// 新畫面的外框：舞台寬 390、高 683～722，依視窗等比縮放置中；舞台外是共用背景，舞台內超出的部分裁掉
export const StageFrame = ({ children }: { children: ReactNode }) => {
  const [size, setSize] = useState(measureWindow)

  useEffect(() => {
    const update = () => setSize(measureWindow())
    window.addEventListener("resize", update)
    return () => window.removeEventListener("resize", update)
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
