import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { expect } from "vitest"
import { App } from "../App"
import { mockFetch, respond, type FetchCall } from "./fetchMock"
import { skillRecords } from "../content/monsters/testData"
import { places } from "../content/world/testData"
import type { ItemDto } from "../content/items/ItemConverter"
import { items } from "../content/items/testData"

const savedStatus = { exists: true, valid: true, error: null }

export function renderItemPage(
  list: unknown = items,
  routes: Record<string, unknown> = {},
): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/items": list,
    "GET /admin/content/skills": skillRecords,
    "GET /admin/content/places": places,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "POST /admin/content/items": respond(201, savedStatus),
    "PUT /admin/content/items/item-2": savedStatus,
    "PUT /admin/content/items/item-10": savedStatus,
    "PUT /admin/content/items/item-223": savedStatus,
    "PUT /admin/content/items/item-568": savedStatus,
    "PUT /admin/content/items/item-1863": savedStatus,
    ...routes,
  })
  render(
    <MemoryRouter initialEntries={["/items"]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

export async function openItemEdit(id = "item-2") {
  const row = await screen.findByRole("row", { name: new RegExp(`^${id} `) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

export async function openItemCreate() {
  await screen.findByRole("row", { name: /^item-2 / })
  await userEvent.click(screen.getByRole("button", { name: "新增" }))
  return screen.findByRole("dialog")
}

export const lastItemBody = (calls: FetchCall[], method: string) =>
  calls.filter((call) => call.method === method).at(-1)?.body as ItemDto

export async function saveItem(dialog: HTMLElement, calls: FetchCall[], method: string) {
  await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))
  await waitFor(() => expect(lastItemBody(calls, method)).toBeDefined())
}
