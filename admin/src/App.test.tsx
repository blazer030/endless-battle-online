import { render, screen } from "@testing-library/react"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "./App"
import { itemDto } from "./content/items/testData"
import { mockFetch } from "./test/fetchMock"

afterEach(() => {
  vi.unstubAllGlobals()
})

describe("App", () => {
  it("opens the item list first", async () => {
    mockFetch({ "GET /admin/content/items": [itemDto({ id: "test-sword", name: "測試鐵劍" })] })

    render(
      <MemoryRouter>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByText("測試鐵劍")).toBeInTheDocument()
  })
})
