import { act, render, screen } from "@testing-library/react"
import { afterEach, expect, test, vi } from "vitest"
import { TitleBackdrop } from "./TitleBackdrop"

afterEach(() => vi.useRealTimers())

const skyOf = (container: HTMLElement) => container.querySelector(".title-sky")!

test("TitleBackdrop_ShowsSignAndDuskSkyInTheEvening", () => {
  vi.useFakeTimers({ toFake: ["Date", "setInterval", "clearInterval"] })
  vi.setSystemTime(new Date(2026, 10, 9, 17, 30))

  const { container } = render(<TitleBackdrop />)

  expect(
    screen.getByRole("heading", { name: "無盡戰記 Endless Battle Online" }),
  ).toBeInTheDocument()
  expect(skyOf(container)).toHaveClass("title-sky--dusk")
})

test("TitleBackdrop_ChecksTheTimeAgainEveryMinute", () => {
  vi.useFakeTimers({ toFake: ["Date", "setInterval", "clearInterval"] })
  vi.setSystemTime(new Date(2026, 10, 9, 16, 59, 30))
  const { container } = render(<TitleBackdrop />)
  expect(skyOf(container)).toHaveClass("title-sky--day")

  act(() => vi.advanceTimersByTime(60_000))

  expect(skyOf(container)).toHaveClass("title-sky--dusk")
})
