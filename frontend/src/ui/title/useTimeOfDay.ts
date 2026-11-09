import { useEffect, useState } from "react"
import { timeOfDay, type TimeOfDay } from "../../game/timeOfDay"
import { TimeOfDayRefreshMilliseconds } from "../../motion/timings"

const now = () => timeOfDay(new Date().getHours())

// 目前的時段；畫面開著時每分鐘重算一次
export const useTimeOfDay = (): TimeOfDay => {
  const [current, setCurrent] = useState(now)
  useEffect(() => {
    const timer = setInterval(() => setCurrent(now()), TimeOfDayRefreshMilliseconds)
    return () => clearInterval(timer)
  }, [])
  return current
}
