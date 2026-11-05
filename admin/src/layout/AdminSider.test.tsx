import { render, screen } from "@testing-library/react"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import { mockFetch } from "../test/fetchMock"

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

// 桌機寬度：Ant Design 的斷點（min-width）全部成立，sidebar 才不會收進抽屜
const renderApp = () => {
  const mobile = window.matchMedia("")
  vi.spyOn(window, "matchMedia").mockImplementation((query) => ({
    ...mobile,
    media: query,
    matches: true,
  }))
  mockFetch({ "GET /admin/content/items": [] })
  return render(
    <MemoryRouter>
      <App />
    </MemoryRouter>,
  )
}

describe("AdminSider", () => {
  it("Sidebar is fixed", async () => {
    renderApp()

    await screen.findByRole("banner")
    const sider = screen.getByRole("link", { name: /怪物/ }).closest(".ant-layout-sider")
    expect(sider).toHaveStyle({ position: "fixed" })
  })
})
