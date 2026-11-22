import { useCallback, useRef, useState } from "react"
import {
  ActionWaitDelaySeconds,
  ActionWaitFadeOutSeconds,
  ActionWaitMinimumMilliseconds,
} from "../../motion/timings"

// idle：沒在等；blocking：只擋點擊；dots：墨點出現；leaving：墨點淡出中（仍擋點擊）
export type ActionWaitPhase = "idle" | "blocking" | "dots" | "leaving"

export type ActionResult<T> = { ok: true; value: T } | { ok: false; error: unknown }

export type ActionFeedback = {
  readonly phase: ActionWaitPhase
  // 送出一個動作：等待層收掉之後才交回結果，畫面這時再換狀態、跳彈窗
  readonly run: <T>(send: () => Promise<T>) => Promise<ActionResult<T>>
}

const wait = (milliseconds: number) =>
  new Promise<void>((resolve) => setTimeout(resolve, milliseconds))

// 玩家動作的等待：一按就擋住點擊；過 ActionWaitDelaySeconds 還沒回應才出墨點，出現後至少停 ActionWaitMinimumMilliseconds
export const useActionFeedback = (): ActionFeedback => {
  const [phase, setPhase] = useState<ActionWaitPhase>("idle")
  const busy = useRef(false)

  const run = useCallback(async <T>(send: () => Promise<T>): Promise<ActionResult<T>> => {
    // 等待層蓋住時其實按不到；同一刻連按的第二下不送，也永遠不交回結果
    if (busy.current) return new Promise<ActionResult<T>>(() => {})
    busy.current = true
    setPhase("blocking")
    let shownAt: number | null = null
    const dotsTimer = setTimeout(() => {
      shownAt = Date.now()
      setPhase("dots")
    }, ActionWaitDelaySeconds * 1000)

    let result: ActionResult<T>
    try {
      result = { ok: true, value: await send() }
    } catch (error) {
      result = { ok: false, error }
    }
    clearTimeout(dotsTimer)

    if (shownAt !== null) {
      const remaining = shownAt + ActionWaitMinimumMilliseconds - Date.now()
      if (remaining > 0) await wait(remaining)
      setPhase("leaving")
      await wait(ActionWaitFadeOutSeconds * 1000)
    }
    busy.current = false
    setPhase("idle")
    return result
  }, [])

  return { phase, run }
}
