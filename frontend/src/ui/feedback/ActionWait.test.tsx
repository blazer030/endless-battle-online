import { act, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ActionWait } from "./ActionWait"
import { useActionFeedback } from "./useActionFeedback"

// 送出後過 delay 毫秒才回應；回應後記下 run 交回的結果
const Harness = ({ delay, onSettled }: { delay: number; onSettled: (value: string) => void }) => {
  const feedback = useActionFeedback()
  const send = () =>
    void feedback
      .run(() => new Promise<string>((resolve) => setTimeout(() => resolve("回來了"), delay)))
      .then((result) => onSettled(result.ok ? result.value : "失敗"))
  return (
    <>
      <button type="button" onClick={send}>
        升級
      </button>
      <ActionWait feedback={feedback} />
    </>
  )
}

const advance = (milliseconds: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })

const dots = () => document.querySelector(".action-wait__plate")

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 action-feedback「A quick response shows no ink dots」
test("QuickResponseShowsNoInkDots", async () => {
  const onSettled = vi.fn()
  render(<Harness delay={200} onSettled={onSettled} />)

  fireEvent.click(screen.getByRole("button", { name: "升級" }))
  expect(document.querySelector(".action-wait")).not.toBeNull()
  await advance(200)

  expect(dots()).toBeNull()
  expect(document.querySelector(".action-wait")).toBeNull()
  expect(onSettled).toHaveBeenCalledWith("回來了")
})

// 規格「A slow response shows the ink dots」
test("SlowResponseShowsInkDots", async () => {
  const onSettled = vi.fn()
  render(<Harness delay={1500} onSettled={onSettled} />)

  fireEvent.click(screen.getByRole("button", { name: "升級" }))
  await advance(449)
  expect(dots()).toBeNull()
  await advance(1)
  expect(dots()).not.toBeNull()
  await advance(1050)
  expect(onSettled).not.toHaveBeenCalled()
  await advance(150)

  expect(document.querySelector(".action-wait")).toBeNull()
  expect(onSettled).toHaveBeenCalledWith("回來了")
})

// 規格「The ink dots stay at least the minimum time」：0.45 秒出現、0.5 秒回應 → 停到 0.75 秒才開始淡出
test("InkDotsStayTheMinimumTime", async () => {
  const onSettled = vi.fn()
  render(<Harness delay={500} onSettled={onSettled} />)

  fireEvent.click(screen.getByRole("button", { name: "升級" }))
  await advance(500)
  expect(dots()).not.toBeNull()
  expect(document.querySelector(".action-wait--leaving")).toBeNull()
  await advance(249)
  expect(document.querySelector(".action-wait--leaving")).toBeNull()
  await advance(1)
  expect(document.querySelector(".action-wait--leaving")).not.toBeNull()
  await advance(150)

  expect(document.querySelector(".action-wait")).toBeNull()
  expect(onSettled).toHaveBeenCalledTimes(1)
})

// 規格「Nothing can be pressed while waiting」：擋點擊層在、再按也不會再送
test("BlocksPressesWhileWaiting", async () => {
  const send = vi.fn(
    () => new Promise<string>((resolve) => setTimeout(() => resolve("回來了"), 1000)),
  )
  const Twice = () => {
    const feedback = useActionFeedback()
    return (
      <>
        <button type="button" onClick={() => void feedback.run(send)}>
          升級
        </button>
        <ActionWait feedback={feedback} />
      </>
    )
  }
  render(<Twice />)

  fireEvent.click(screen.getByRole("button", { name: "升級" }))
  fireEvent.click(screen.getByRole("button", { name: "升級" }))

  expect(send).toHaveBeenCalledTimes(1)
  expect(document.querySelector(".action-wait")).not.toBeNull()
  await advance(1200)
})

// 送出失敗時 run 交回 ok: false 與錯誤，等待層一樣收掉
test("FailureSettlesWithTheError", async () => {
  const error = new Error("斷線")
  let settled: unknown = null
  const Failing = () => {
    const feedback = useActionFeedback()
    return (
      <>
        <button
          type="button"
          onClick={() =>
            void feedback
              .run(() => Promise.reject(error))
              .then((result) => {
                settled = result
              })
          }
        >
          升級
        </button>
        <ActionWait feedback={feedback} />
      </>
    )
  }
  render(<Failing />)

  fireEvent.click(screen.getByRole("button", { name: "升級" }))
  await advance(0)

  expect(settled).toEqual({ ok: false, error })
  expect(document.querySelector(".action-wait")).toBeNull()
})
