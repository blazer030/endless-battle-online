import { render, screen } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import { StageErrorBoundary } from "./StageErrorBoundary"

const Broken = () => {
  throw new Error("chunk failed")
}

test("StageErrorBoundary_RendersBlankStage_WhenChildThrows", () => {
  const consoleError = vi.spyOn(console, "error").mockImplementation(() => {})

  render(
    <StageErrorBoundary>
      <Broken />
    </StageErrorBoundary>,
  )

  expect(screen.getByTestId("stage-fallback")).toBeInTheDocument()
  expect(consoleError).toHaveBeenCalled()
  consoleError.mockRestore()
})
