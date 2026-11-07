import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../../App"
import { chooseOption } from "../../test/antd"
import { mockFetch, respond, type FetchCall } from "../../test/fetchMock"
import { ContentParseError } from "../shared/parse"
import { PlaceConverter, type AreaDto, type PlaceDto } from "./WorldConverter"
import { areas, placeDto, places } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const savedStatus = { exists: true, valid: true, error: null }

function renderPage(path: string): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/areas": areas,
    "GET /admin/content/places": places,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "PUT /admin/content/areas/central": savedStatus,
    "PUT /admin/content/places/1": savedStatus,
    "POST /admin/content/places": respond(201, savedStatus),
  })
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

async function openEdit(id: string) {
  const row = await screen.findByRole("row", { name: new RegExp(`^${id} `) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

const lastBody = <T,>(calls: FetchCall[], method: string) =>
  calls.filter((call) => call.method === method).at(-1)?.body as T

describe("PlaceConverter", () => {
  it("keeps every field through toVo and toDto", () => {
    const dto = placeDto({ restricted: true })

    expect(PlaceConverter.toDto(PlaceConverter.toVo(dto))).toEqual(dto)
  })

  it("reports the path of a missing recommended level", () => {
    const dto = placeDto({ recommendedLevel: { minimum: 1 } as never })

    expect(() => PlaceConverter.toVo(dto, "places[0]")).toThrow(ContentParseError)
    expect(() => PlaceConverter.toVo(dto, "places[0]")).toThrow(
      "places[0].recommendedLevel.maximum",
    )
  })
})

describe("AreaListPage", () => {
  it("shows place names and sends the two chosen places", async () => {
    const calls = renderPage("/areas")
    const row = await screen.findByRole("row", { name: /^south / })
    expect(within(row).getByText("灼熱荒漠、古代禁區")).toBeInTheDocument()

    const dialog = await openEdit("central")
    await chooseOption(within(dialog).getByRole("combobox", { name: "地區" }), "灼熱荒漠")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastBody<AreaDto>(calls, "PUT")).toBeDefined())
    expect(lastBody<AreaDto>(calls, "PUT").placeIds).toEqual([1, 2])
  })
})

describe("PlaceListPage", () => {
  it("shows only restricted places with the restriction filter", async () => {
    renderPage("/places")
    await screen.findByRole("row", { name: /^1 / })

    await chooseOption(screen.getByRole("combobox", { name: "是否禁區" }), "禁區")

    expect(screen.getByRole("row", { name: /^3 / })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /^1 / })).not.toBeInTheDocument()
  })

  it("filters by area and shows the area name", async () => {
    renderPage("/places")
    const row = await screen.findByRole("row", { name: /^2 / })
    expect(within(row).getByText("南方大陸")).toBeInTheDocument()

    await chooseOption(screen.getByRole("combobox", { name: "大陸" }), "中央大陸")

    expect(screen.getByRole("row", { name: /^1 / })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /^2 / })).not.toBeInTheDocument()
  })

  it("edits sub-place names and the recommended level", async () => {
    const calls = renderPage("/places")
    const dialog = await openEdit("1")

    await userEvent.click(within(dialog).getByRole("button", { name: "新增子地區" }))
    await userEvent.type(within(dialog).getByLabelText("子地區 3"), "白沙海灣")
    const maximum = within(dialog).getByLabelText("推薦等級最大")
    await userEvent.clear(maximum)
    await userEvent.type(maximum, "12")
    await userEvent.click(within(dialog).getByRole("checkbox", { name: "禁區" }))
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastBody<PlaceDto>(calls, "PUT")).toBeDefined())
    expect(lastBody<PlaceDto>(calls, "PUT")).toEqual({
      ...placeDto(),
      subPlaceNames: ["香茅綠地", "綠風草地", "白沙海灣"],
      recommendedLevel: { minimum: 1, maximum: 12 },
      restricted: true,
    })
  })

  it("creates a place with a numeric id from a copy", async () => {
    const calls = renderPage("/places")
    await screen.findByRole("row", { name: /^1 / })
    await userEvent.click(screen.getByRole("button", { name: "新增" }))
    const dialog = await screen.findByRole("dialog")

    await chooseOption(within(dialog).getByRole("combobox", { name: "複製自" }), "2 灼熱荒漠")
    expect(within(dialog).getByLabelText("id")).toHaveValue("")
    await userEvent.type(within(dialog).getByLabelText("id"), "30")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastBody<PlaceDto>(calls, "POST")).toBeDefined())
    expect(lastBody<PlaceDto>(calls, "POST")).toEqual({ ...places[1], id: 30 })
  })
})
