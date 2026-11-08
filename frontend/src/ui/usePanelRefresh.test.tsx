import { act, renderHook } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { PanelRefreshMilliseconds } from "../motion/timings"
import { usePanelRefresh } from "./usePanelRefresh"

const advance = (milliseconds: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })

const renderRefresh = () => {
  const reload = vi.fn()
  const view = renderHook(({ signal }) => usePanelRefresh(signal, reload), {
    initialProps: { signal: 0 },
  })
  return { reload, ...view }
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

test("usePanelRefresh_OpeningLoadCountsAsTheFirstReload", async () => {
  const { reload, rerender } = renderRefresh()
  await advance(1_000)

  rerender({ signal: 1 })
  await advance(PanelRefreshMilliseconds - 1_001)
  expect(reload).not.toHaveBeenCalled()

  await advance(1)
  expect(reload).toHaveBeenCalledTimes(1)
})

test("usePanelRefresh_SignalsWithinTheIntervalMergeIntoOneReload", async () => {
  const { reload, rerender } = renderRefresh()
  await advance(PanelRefreshMilliseconds)

  rerender({ signal: 1 })
  await advance(0)
  rerender({ signal: 2 })
  await advance(1_000)
  rerender({ signal: 3 })
  await advance(PanelRefreshMilliseconds)

  expect(reload).toHaveBeenCalledTimes(2)
})

test("usePanelRefresh_UnmountCancelsAPendingReload", async () => {
  const { reload, rerender, unmount } = renderRefresh()
  rerender({ signal: 1 })

  unmount()
  await advance(PanelRefreshMilliseconds * 2)

  expect(reload).not.toHaveBeenCalled()
})
