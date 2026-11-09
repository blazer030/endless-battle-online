import { useTimeOfDay } from "./useTimeOfDay"
import "./Passage.css"

type Props = { title: string; subtitle: string }

// 進城過場：「歡迎回來＋帳號」或「踏上旅途＋角色名」，整個舞台淡進城鎮
export const Passage = ({ title, subtitle }: Props) => {
  const time = useTimeOfDay()
  return (
    <div className={`passage passage--${time}`} aria-live="polite">
      <b>{title}</b>
      <small>{subtitle}</small>
    </div>
  )
}
