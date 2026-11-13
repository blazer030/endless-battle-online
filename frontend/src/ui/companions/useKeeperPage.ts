import { useCallback, useEffect, useRef, useState } from "react"
import { ApiError } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import type { KeeperEvent } from "../../game/companions"
import type { CompanionsApi } from "./CompanionsScreen"

type Options<T> = {
  api: Pick<CompanionsApi, "getMyCharacter">
  // 進頁與重試時讀的資料；呼叫端用 useCallback 固定
  load: () => Promise<T>
  onCharacterChange: (character: CharacterVo) => void
  // 讀不到時說的事件；呼叫端傳模組常數
  loadFailed: KeeperEvent
  // 伺服器擋下時的原因代碼（與目前的頁面資料）→ 管理人的事件；連線錯誤與沒有原因代碼時用 failed
  rejected: (reason: string, view: T | null) => KeeperEvent
  failed: KeeperEvent
}

// 獸欄與傭兵所共用：讀資料、重試、同一時間只送一個請求、結果交給管理人說；錢不夠時重讀角色
export const useKeeperPage = <T>({
  api,
  load,
  onCharacterChange,
  loadFailed,
  rejected,
  failed,
}: Options<T>) => {
  const [view, setView] = useState<T | null>(null)
  const [loadFailedNow, setLoadFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [event, setEvent] = useState<KeeperEvent | null>(null)
  const [busy, setBusy] = useState(false)
  const alive = useRef(true)

  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
    }
  }, [])

  useEffect(() => {
    let isCurrent = true
    load().then(
      (loaded) => {
        if (!isCurrent) return
        setView(loaded)
        setLoadFailed(false)
        setEvent(null)
      },
      () => {
        if (!isCurrent) return
        setLoadFailed(true)
        setEvent(loadFailed)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [load, loadFailed, attempt])

  const reloadCharacter = useCallback(async () => {
    try {
      const latest = await api.getMyCharacter()
      if (alive.current && latest !== null) onCharacterChange(latest)
    } catch {
      // 維持原本的金錢
    }
  }, [api, onCharacterChange])

  const fail = (error: unknown) => {
    if (error instanceof ApiError && error.reasons.length > 0) {
      setEvent(rejected(error.reasons[0]!, view))
      if (error.reasons.includes("notEnoughMoney")) void reloadCharacter()
    } else setEvent(failed)
  }

  // 送一個請求：成功時交給 onSuccess 套用結果並回傳要說的事件（null 不說話）；回傳是否成功
  const perform = async <R>(
    action: () => Promise<R>,
    onSuccess: (result: R) => KeeperEvent | null,
  ) => {
    setBusy(true)
    setEvent(null)
    try {
      const result = await action()
      if (!alive.current) return true
      setEvent(onSuccess(result))
      return true
    } catch (error) {
      if (alive.current) fail(error)
      return false
    } finally {
      if (alive.current) setBusy(false)
    }
  }

  // 回應就是新的頁面資料時：換上並說 success
  const run = (action: () => Promise<T>, success: KeeperEvent) =>
    perform(action, (result) => {
      setView(result)
      return success
    })

  const retry = () => {
    setEvent(null)
    setLoadFailed(false)
    setAttempt((count) => count + 1)
  }

  return {
    view,
    setView,
    loadFailed: loadFailedNow,
    event,
    setEvent,
    busy,
    alive,
    perform,
    run,
    retry,
  }
}
