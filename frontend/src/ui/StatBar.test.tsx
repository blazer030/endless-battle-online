import { render, screen } from "@testing-library/react"
import { expect, test } from "vitest"
import { StatBar } from "./StatBar"

test("StatBar_ZeroMax_RendersEmptyFill", () => {
  render(<StatBar label="EXP" value={0} max={0} tone="exp" />)

  const bar = screen.getByRole("progressbar", { name: "EXP" })
  expect(bar).toHaveAttribute("aria-valuenow", "0")
  expect(bar).toHaveAttribute("aria-valuemax", "0")
  expect(bar).toHaveAttribute("aria-valuetext", "0 / 0")
  expect(bar.firstElementChild).toHaveStyle({ width: "0%" })
})

test("StatBar_ClampsValueNowToMax", () => {
  render(<StatBar label="EXP" value={150} max={100} tone="exp" />)

  const bar = screen.getByRole("progressbar", { name: "EXP" })
  expect(bar).toHaveAttribute("aria-valuenow", "100")
  expect(bar).toHaveAttribute("aria-valuemax", "100")
  expect(bar).toHaveAttribute("aria-valuetext", "150 / 100")
  expect(bar.firstElementChild).toHaveStyle({ width: "100%" })
})
