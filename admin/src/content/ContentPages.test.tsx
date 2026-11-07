import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import { mockFetch } from "../test/fetchMock"

const sword = {
  id: "test-sword",
  name: "測試劍",
  price: 1,
  shopLevel: 1,
  category: "Weapon",
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe("ContentEdit", () => {
  it("sends the whole record with only the edited field changed", async () => {
    const calls = mockFetch({
      "GET /admin/content/items/test-sword": sword,
      "PUT /admin/content/items/test-sword": { exists: true, valid: true, error: null },
    })
    render(
      <MemoryRouter initialEntries={["/items/edit/test-sword"]}>
        <App />
      </MemoryRouter>,
    )
    const price = await screen.findByLabelText("price")

    await userEvent.clear(price)
    await userEvent.type(price, "9")
    await userEvent.click(screen.getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(calls.some((call) => call.method === "PUT")).toBe(true))
    expect(calls.find((call) => call.method === "PUT")?.body).toEqual({ ...sword, price: 9 })
  })

  it("shows the new price in the list after saving", async () => {
    let saved = sword
    mockFetch({
      "GET /admin/content/items/test-sword": () => saved,
      "PUT /admin/content/items/test-sword": (body: typeof sword) => {
        saved = body
        return { exists: true, valid: true, error: null }
      },
      "GET /admin/content/items": () => [saved],
    })
    render(
      <MemoryRouter initialEntries={["/items", "/items/edit/test-sword"]} initialIndex={1}>
        <App />
      </MemoryRouter>,
    )
    const price = await screen.findByLabelText("price")

    await userEvent.clear(price)
    await userEvent.type(price, "9")
    await userEvent.click(screen.getByRole("button", { name: "儲存" }))
    await waitFor(() => expect(saved.price).toBe(9))
    await userEvent.click(screen.getByRole("link", { name: /道具/ }))

    const row = await screen.findByRole("row", { name: /test-sword/ })
    expect(row).toHaveTextContent("9")
  })
})
