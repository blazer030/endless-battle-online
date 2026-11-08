import { render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../../App"
import { chooseOption, visibleOptions } from "../../test/antd"
import { mockFetch, type FetchCall } from "../../test/fetchMock"
import { ContentParseError } from "../shared/parse"
import { JobConverter, type JobDto } from "./JobConverter"
import { jobDto, jobs } from "./testData"

afterEach(() => {
  vi.unstubAllGlobals()
})

const savedStatus = { exists: true, valid: true, error: null }

function renderJobs(routes: Record<string, unknown> = {}): FetchCall[] {
  const calls = mockFetch({
    "GET /admin/content/jobs": jobs,
    "GET /admin/draft": { exists: false, valid: true, error: null },
    "PUT /admin/content/jobs/job-6": savedStatus,
    "PUT /admin/content/jobs/job-10": savedStatus,
    ...routes,
  })
  render(
    <MemoryRouter initialEntries={["/jobs"]}>
      <App />
    </MemoryRouter>,
  )
  return calls
}

async function openEdit(id: string) {
  const row = await screen.findByRole("row", { name: new RegExp(`${id}\\b`) })
  await userEvent.click(within(row).getByRole("button", { name: "編輯" }))
  return screen.findByRole("dialog")
}

const lastPut = (calls: FetchCall[]) =>
  calls.filter((call) => call.method === "PUT").at(-1)?.body as JobDto

describe("JobConverter", () => {
  it("reports the path of a missing growth value", () => {
    const dto = jobDto({ growth: { ...jobDto().growth, life: { minimum: 1 } as never } })

    expect(() => JobConverter.toVo(dto, "jobs[0]")).toThrow(ContentParseError)
    expect(() => JobConverter.toVo(dto, "jobs[0]")).toThrow("jobs[0].growth.life.maximum")
  })

  it("keeps every field through toVo and toDto", () => {
    const dto = jobDto({
      parentJobId: "job-6",
      statRequirements: [{ stat: "Agility", minimum: 5 }],
    })

    expect(JobConverter.toDto(JobConverter.toVo(dto))).toEqual(dto)
  })
})

describe("JobListPage", () => {
  it("shows the parent job by name and filters by tier", async () => {
    renderJobs()
    const berserker = await screen.findByRole("row", { name: /job-10/ })
    expect(within(berserker).getByText("戰士")).toBeInTheDocument()

    await chooseOption(screen.getByRole("combobox", { name: "階級" }), "2 階")

    expect(screen.getByRole("row", { name: /job-10/ })).toBeInTheDocument()
    expect(screen.queryByRole("row", { name: /job-7/ })).not.toBeInTheDocument()
  })

  it("does not offer the job itself as its parent", async () => {
    renderJobs()
    const dialog = await openEdit("job-10")

    await userEvent.click(within(dialog).getByRole("combobox", { name: "上級職業" }))

    await waitFor(() => expect(visibleOptions()).toContain("戰士"))
    expect(visibleOptions()).toContain("無")
    expect(visibleOptions()).toContain("術士")
    expect(visibleOptions()).not.toContain("狂戰士")
  })

  it("sends the edited growth and leaves the other stats unchanged", async () => {
    const calls = renderJobs()
    const dialog = await openEdit("job-6")
    const minimum = within(dialog).getByLabelText("力量最小")
    const maximum = within(dialog).getByLabelText("力量最大")

    await userEvent.clear(minimum)
    await userEvent.type(minimum, "2")
    await userEvent.clear(maximum)
    await userEvent.type(maximum, "4")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastPut(calls)).toBeDefined())
    expect(lastPut(calls)).toEqual({
      ...jobDto(),
      growth: { ...jobDto().growth, strength: { minimum: 2, maximum: 4 } },
    })
  })

  it("chooses a parent job and adds a stat requirement", async () => {
    const calls = renderJobs()
    const dialog = await openEdit("job-10")

    await chooseOption(within(dialog).getByRole("combobox", { name: "上級職業" }), "術士")
    await userEvent.click(within(dialog).getByRole("button", { name: "新增能力需求" }))
    await chooseOption(within(dialog).getByRole("combobox", { name: "能力 1" }), "敏捷")
    await userEvent.type(within(dialog).getByLabelText("能力需求 1"), "5")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastPut(calls)?.parentJobId).toBe("job-7"))
    expect(lastPut(calls).statRequirements).toEqual([{ stat: "Agility", minimum: 5 }])
  })

  it("sends null when the parent job is cleared", async () => {
    const calls = renderJobs()
    const dialog = await openEdit("job-10")

    await chooseOption(within(dialog).getByRole("combobox", { name: "上級職業" }), "無")
    await userEvent.click(within(dialog).getByRole("button", { name: "儲存" }))

    await waitFor(() => expect(lastPut(calls)).toBeDefined())
    expect(lastPut(calls).parentJobId).toBeNull()
  })
})
