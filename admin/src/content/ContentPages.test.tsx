import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import { mockFetch } from "../test/fetchMock"

const sprite = {
  id: "test-sprite",
  name: "測試雪精",
  level: 1,
  placeId: 1,
  subPlaces: [1, 2],
  drops: [{ itemId: "small-supply", topr: 3 }],
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe("ContentEdit", () => {
  it("sends the whole record with only the edited field changed", async () => {
    const calls = mockFetch({
      "GET /admin/content/monsters/test-sprite": sprite,
      "PUT /admin/content/monsters/test-sprite": { exists: true, valid: true, error: null },
    })
    render(
      <MemoryRouter initialEntries={["/monsters/edit/test-sprite"]}>
        <App />
      </MemoryRouter>,
    )
    const level = await screen.findByLabelText("level")

    await userEvent.clear(level)
    await userEvent.type(level, "9")
    await userEvent.click(screen.getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(calls.some((call) => call.method === "PUT")).toBe(true))
    expect(calls.find((call) => call.method === "PUT")?.body).toEqual({ ...sprite, level: 9 })
  })

  it("shows the new level in the list after saving", async () => {
    let saved = sprite
    mockFetch({
      "GET /admin/content/monsters/test-sprite": () => saved,
      "PUT /admin/content/monsters/test-sprite": (body: typeof sprite) => {
        saved = body
        return { exists: true, valid: true, error: null }
      },
      "GET /admin/content/monsters": () => [saved],
    })
    render(
      <MemoryRouter initialEntries={["/monsters", "/monsters/edit/test-sprite"]} initialIndex={1}>
        <App />
      </MemoryRouter>,
    )
    const level = await screen.findByLabelText("level")

    await userEvent.clear(level)
    await userEvent.type(level, "9")
    await userEvent.click(screen.getByRole("button", { name: "儲存" }))
    await waitFor(() => expect(saved.level).toBe(9))
    await userEvent.click(screen.getByRole("link", { name: /怪物/ }))

    const row = await screen.findByRole("row", { name: /test-sprite/ })
    expect(row).toHaveTextContent("9")
  })
})
