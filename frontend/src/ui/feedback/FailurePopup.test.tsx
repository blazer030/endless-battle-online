import { act, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { FailurePopup } from "./FailurePopup"

const advance = (milliseconds: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 action-feedback「Shared failure popup」：紅蠟封、一句紅字、確定
test("ShowsTheText", () => {
  render(<FailurePopup text="連線失敗" onClose={vi.fn()} />)

  const dialog = screen.getByRole("alertdialog")
  expect(dialog).toHaveTextContent("！")
  expect(dialog).toHaveTextContent("連線失敗")
  expect(screen.getByRole("button", { name: "確定" })).toBeInTheDocument()
})

// 按確定：淡出 0.15 秒後才交給呼叫端關掉
test("ConfirmClosesAfterFadingOut", async () => {
  const onClose = vi.fn()
  render(<FailurePopup text="金錢不足" onClose={onClose} />)

  fireEvent.click(screen.getByRole("button", { name: "確定" }))
  await advance(149)
  expect(onClose).not.toHaveBeenCalled()
  await advance(1)

  expect(onClose).toHaveBeenCalledTimes(1)
})

// 規格「Closing the popup」：點外面也關
test("ClosesOnOutsidePress", async () => {
  const onClose = vi.fn()
  render(<FailurePopup text="金錢不足" onClose={onClose} />)

  fireEvent.click(document.querySelector(".popup-scrim")!)
  await advance(150)

  expect(onClose).toHaveBeenCalledTimes(1)
})
