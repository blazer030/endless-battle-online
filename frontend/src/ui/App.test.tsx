import { render, screen } from "@testing-library/react"
import { expect, test } from "vitest"
import App from "./App"

test("App_ShowsTitle", () => {
  render(<App />)

  expect(screen.getByRole("heading", { name: "墨渡" })).toBeInTheDocument()
})
