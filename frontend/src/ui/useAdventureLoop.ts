import { useEffect, useRef, useState } from "react"
import { ApiError } from "../api/client"
import type { ApiClient } from "../api/client"
import type { BattlePlayback, PlaybackItem } from "../game/battlePlayback"
import type { AdvanceResponse } from "../game/types"
import { PollIntervalMilliseconds } from "../motion/timings"

const RetryMessage = "連線不穩，稍後自動重試"

type Options = {
  api: ApiClient
  playback: BattlePlayback
  onItems: (items: PlaybackItem[]) => void
  // 每次推進回應都會呼叫，用來取播放佇列以外的資訊（例如禁區通行證）
  onResponse?: (response: AdvanceResponse) => void
  now?: () => number
}

export const useAdventureLoop = ({
  api,
  playback,
  onItems,
  onResponse,
  now = Date.now,
}: Options): { error: string | null } => {
  const [error, setError] = useState<string | null>(null)
  const latest = useRef({ api, playback, onItems, onResponse, now })
  // 放在 ref 而非 effect 內，StrictMode 重掛時不會在請求未回來前再送一次
  const requestInFlight = useRef(false)

  useEffect(() => {
    latest.current = { api, playback, onItems, onResponse, now }
  })

  useEffect(() => {
    const advance = async () => {
      if (requestInFlight.current) return
      requestInFlight.current = true
      const { api, playback, onResponse, now } = latest.current
      try {
        const response = await api.advance()
        playback.enqueue(response, now())
        onResponse?.(response)
        setError(null)
      } catch (reason) {
        // 409 表示另一個請求正在結算，等下一輪即可
        if (!(reason instanceof ApiError && reason.status === 409)) setError(RetryMessage)
      } finally {
        requestInFlight.current = false
      }
    }

    // 分頁在背景時 animation frame 暫停，回來後第一幀會一次交出所有到期項目
    let frame = 0
    const tick = () => {
      const { playback, onItems, now } = latest.current
      const items = playback.takeDue(now())
      if (items.length > 0) onItems(items)
      frame = requestAnimationFrame(tick)
    }

    const onVisibilityChange = () => {
      if (document.visibilityState === "visible") void advance()
    }

    void advance()
    // 背景時不輪詢：伺服器會結算離開期間，回到前景時一次拿回摘要與最後一段事件
    const interval = setInterval(() => {
      if (document.visibilityState !== "hidden") void advance()
    }, PollIntervalMilliseconds)
    frame = requestAnimationFrame(tick)
    document.addEventListener("visibilitychange", onVisibilityChange)
    return () => {
      clearInterval(interval)
      cancelAnimationFrame(frame)
      document.removeEventListener("visibilitychange", onVisibilityChange)
    }
  }, [])

  return { error }
}
