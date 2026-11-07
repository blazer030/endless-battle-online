import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { expect } from "vitest"
import { App } from "../../App"
import { mockFetch, type FetchCall } from "../../test/fetchMock"
import { jobs } from "../jobs/testData"
import type { SkillDto } from "./SkillConverter"

const savedStatus = { exists: true, valid: true, error: null }

export function renderSkillPage(list: unknown): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/skills": list,
    "GET /admin/content/jobs": jobs,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "PUT /admin/content/skills/skill-1": savedStatus,
  })
  render(
    <MemoryRouter initialEntries={["/skills"]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

export async function openSkillEdit(id = "skill-1") {
  const row = await screen.findByRole("row", { name: new RegExp(`${id}\\b`) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

export const lastSkillPut = (calls: FetchCall[]) =>
  calls.filter((call) => call.method === "PUT").at(-1)?.body as SkillDto

export async function saveSkill(dialog: HTMLElement, calls: FetchCall[]) {
  await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))
  await waitFor(() => expect(lastSkillPut(calls)).toBeDefined())
}
