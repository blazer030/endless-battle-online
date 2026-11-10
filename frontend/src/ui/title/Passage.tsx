import { InkDots } from "./InkDots"
import { useTimeOfDay } from "./useTimeOfDay"
import "./Passage.css"

type Props = {
  // 省略時還在讀取：背景上只有墨點
  title?: string
  subtitle?: string
  // 背景一開始就在（已登入打開網頁）：不整層淡入，只淡入文字
  immediate?: boolean
}

// 進城過場：「歡迎回來」或「踏上旅途」＋角色名，整個舞台淡進城鎮
export const Passage = ({ title, subtitle, immediate = false }: Props) => {
  const time = useTimeOfDay()
  return (
    <div
      className={`passage passage--${time}${immediate ? " passage--immediate" : ""}`}
      aria-live="polite"
    >
      {title === undefined ? (
        <InkDots className="passage-wait" label="讀取中" />
      ) : (
        <>
          <b>{title}</b>
          <small>{subtitle}</small>
        </>
      )}
    </div>
  )
}
