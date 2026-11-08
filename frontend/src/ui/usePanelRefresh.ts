import { useEffect, useRef } from "react"
import { PanelRefreshMilliseconds } from "../motion/timings"

// signal 每變一次代表戰報讓這一頁過時；打開時的載入也算一次，
// 距上次載入不到 PanelRefreshMilliseconds 時延到間隔滿再載，期間的訊號合併成一次
// 用單調時鐘，裝置時間被往回調也不會拖延重載
export const usePanelRefresh = (signal: number, reload: () => void) => {
  const latestReload = useRef(reload)
  const lastLoad = useRef(0)
  const initialSignal = useRef(signal)

  useEffect(() => {
    latestReload.current = reload
  })

  useEffect(() => {
    lastLoad.current = performance.now()
  }, [])

  useEffect(() => {
    if (signal === initialSignal.current) return
    const run = () => {
      lastLoad.current = performance.now()
      latestReload.current()
    }
    const wait = lastLoad.current + PanelRefreshMilliseconds - performance.now()
    if (wait <= 0) {
      run()
      return
    }
    const timer = setTimeout(run, wait)
    return () => clearTimeout(timer)
  }, [signal])
}
