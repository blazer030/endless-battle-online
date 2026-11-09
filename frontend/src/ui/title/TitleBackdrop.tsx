import { useTimeOfDay } from "./useTimeOfDay"
import "./TitleBackdrop.css"

// 標題的背景：依時段換的城鎮遠景、吊著的招牌、前面的木桌
export const TitleBackdrop = ({ departing = false }: { departing?: boolean }) => {
  const time = useTimeOfDay()
  return (
    <>
      <div
        className={`title-sky title-sky--${time}${departing ? " title-sky--departing" : ""}`}
        aria-hidden="true"
      />
      <h1 className="title-sign" aria-label="無盡戰記 Endless Battle Online">
        <b>無盡戰記</b>
        <small>Endless Battle Online</small>
      </h1>
      <div className="title-desk" aria-hidden="true" />
    </>
  )
}
