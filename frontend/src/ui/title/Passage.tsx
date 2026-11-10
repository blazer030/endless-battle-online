import { InkDots } from "./InkDots"
import { useTimeOfDay } from "./useTimeOfDay"
import "./Passage.css"

type Props = {
  // 省略時還在讀取：背景上只有墨點
  title?: string
  subtitle?: string
  // 背景一開始就在（已登入打開網頁）：不整層淡入，只淡入文字
  immediate?: boolean
  // 打開網頁沒角色或讀不到：疊在下一個畫面上淡出，不攔點擊、不宣告讀取中
  leaving?: boolean
}

const modifierOf = ({ immediate = false, leaving = false }: Props) => {
  if (leaving) return " passage--leaving"
  return immediate ? " passage--immediate" : ""
}

// 進城過場：「歡迎回來」或「踏上旅途」＋角色名，整個舞台淡進城鎮
export const Passage = (props: Props) => {
  const { title, subtitle, leaving = false } = props
  const time = useTimeOfDay()
  return (
    <div
      className={`passage passage--${time}${modifierOf(props)}`}
      {...(leaving ? { "aria-hidden": true } : { "aria-live": "polite" })}
    >
      {leaving && <InkDots className="passage-wait" />}
      {!leaving && title === undefined && <InkDots className="passage-wait" label="讀取中" />}
      {!leaving && title !== undefined && (
        <>
          <b>{title}</b>
          <small>{subtitle}</small>
        </>
      )}
    </div>
  )
}
