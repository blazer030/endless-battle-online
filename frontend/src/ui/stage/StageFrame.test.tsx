import { act, render, screen } from "@testing-library/react"
import { afterEach, expect, test } from "vitest"
import { StageFrame } from "./StageFrame"

const originalWidth = window.innerWidth
const originalHeight = window.innerHeight

const resizeWindow = (width: number, height: number) => {
  Object.defineProperty(window, "innerWidth", { configurable: true, value: width })
  Object.defineProperty(window, "innerHeight", { configurable: true, value: height })
  window.dispatchEvent(new Event("resize"))
}

afterEach(() => resizeWindow(originalWidth, originalHeight))

const stageOf = (container: HTMLElement) => container.querySelector<HTMLElement>(".stage")!

test("StageFrame_FollowsTheWindowSize", () => {
  resizeWindow(390, 683)
  const { container } = render(
    <StageFrame>
      <p>登記證</p>
    </StageFrame>,
  )

  expect(screen.getByText("登記證")).toBeInTheDocument()
  expect(stageOf(container).style.getPropertyValue("--stage-h")).toBe("683px")
  expect(stageOf(container).style.getPropertyValue("--stage-height")).toBe("683")
  expect(stageOf(container).style.transform).toBe("translate(0px, 0px) scale(1)")

  act(() => resizeWindow(412, 880))

  // spec 範例表：412×880 → 倍率 1.0564、高 780、上下各留 28.0
  const [, x, y, scale] = /^translate\((.+)px, (.+)px\) scale\((.+)\)$/.exec(
    stageOf(container).style.transform,
  )!
  expect(stageOf(container).style.getPropertyValue("--stage-h")).toBe("780px")
  expect(Number(scale)).toBeCloseTo(1.0564, 4)
  expect(Number(x)).toBe(0)
  expect(Math.round(Number(y) * 10) / 10).toBe(28.0)
})

// 雙指放大或鍵盤彈出只改變可見範圍（visualViewport），舞台不跟著縮
test("StageFrame_IgnoresZoomAndKeyboard", () => {
  resizeWindow(390, 683)
  const viewport = Object.assign(new EventTarget(), { width: 195, height: 341 })
  Object.defineProperty(window, "visualViewport", { configurable: true, value: viewport })
  try {
    const { container } = render(
      <StageFrame>
        <p>登記證</p>
      </StageFrame>,
    )

    act(() => viewport.dispatchEvent(new Event("resize")))

    expect(stageOf(container).style.getPropertyValue("--stage-h")).toBe("683px")
    expect(stageOf(container).style.transform).toBe("translate(0px, 0px) scale(1)")
  } finally {
    Object.defineProperty(window, "visualViewport", { configurable: true, value: null })
  }
})
