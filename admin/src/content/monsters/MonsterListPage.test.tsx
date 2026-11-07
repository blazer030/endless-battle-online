import { render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../../App"
import { chooseOption } from "../../test/antd"
import { mockFetch } from "../../test/fetchMock"
import type { MonsterDto } from "./MonsterConverter"
import { monsterDto, placeRecords } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const renderMonsters = (monsters: unknown[]) => {
  mockFetch({
    "GET /admin/content/monsters": monsters,
    "GET /admin/content/places": placeRecords,
    "GET /admin/draft": { exists: false, valid: true, error: null },
  })
  render(
    <MemoryRouter initialEntries={["/monsters"]}>
      <App />
    </MemoryRouter>,
  )
}

const monsters = [
  monsterDto({ id: "monster-1", name: "史萊姆", placeId: 1 }),
  monsterDto({ id: "monster-2", name: "水母", placeId: 1 }),
  monsterDto({ id: "monster-3", name: "沙漠史萊姆", placeId: 2 }),
  monsterDto({ id: "monster-4", name: "全域怪", placeId: 0 }),
]

describe("MonsterListPage", () => {
  it("shows the place name instead of its id", async () => {
    renderMonsters(monsters)

    const row = await screen.findByRole("row", { name: /monster-3/ })

    expect(within(row).getByText("灼熱荒漠")).toBeInTheDocument()
    expect(
      within(await screen.findByRole("row", { name: /monster-4/ })).getByText("全域"),
    ).toBeInTheDocument()
  })

  it("combines the place filter with the search box", async () => {
    renderMonsters(monsters)
    await screen.findByRole("row", { name: /monster-3/ })

    await chooseOption(screen.getByRole("combobox", { name: "地區" }), "中央平原")
    await userEvent.type(screen.getByPlaceholderText("搜尋 id 或名稱"), "史萊姆")

    expect(screen.getByRole("row", { name: /monster-1/ })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /monster-2/ })).not.toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /monster-3/ })).not.toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /monster-4/ })).not.toBeInTheDocument()
  })

  it("matches the id without caring about case", async () => {
    renderMonsters(monsters)
    await screen.findByRole("row", { name: /monster-3/ })

    await userEvent.type(screen.getByPlaceholderText("搜尋 id 或名稱"), "MONSTER-2")

    expect(screen.getByRole("row", { name: /monster-2/ })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /monster-1/ })).not.toBeInTheDocument()
  })

  it("shows the path of a missing field and no rows", async () => {
    const withoutLevel: Partial<MonsterDto> = monsterDto({ id: "monster-2" })
    delete withoutLevel.level
    renderMonsters([monsterDto(), withoutLevel])

    expect(await screen.findByText(/monsters\[1\]\.level/)).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /monster-1/ })).not.toBeInTheDocument()
  })

  it("shows a monster normally when the response has an extra field", async () => {
    renderMonsters([{ ...monsterDto(), rarity: 3 }])

    expect(await screen.findByRole("row", { name: /monster-1/ })).toBeInTheDocument()
  })

  it("pages the list at 50 rows", async () => {
    renderMonsters(
      Array.from({ length: 51 }, (_, index) =>
        monsterDto({ id: `page-${index + 1}`, name: `怪${index + 1}` }),
      ),
    )

    expect(await screen.findByRole("row", { name: /page-50/ })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /page-51/ })).not.toBeInTheDocument()
  })
})
