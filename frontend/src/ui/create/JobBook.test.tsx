import { render, screen, within } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import { makeCreationOptions, makePublicJobs } from "../../test/fixtures"
import { user } from "../../test/user"
import { CreateScreen } from "./CreateScreen"
import { JobBook } from "./JobBook"

const firstTier = makePublicJobs().filter((job) => job.tier === 1)

const renderBook = (page: string, sex: number | null) =>
  render(<JobBook open jobs={firstTier} page={page} sex={sex} onPage={vi.fn()} onPick={vi.fn()} />)

test("JobBook_UsesFemalePortraitWhenSexIsFemale", () => {
  renderBook("job-7", 2)

  expect(screen.getByRole("img", { name: "術士" })).toHaveAttribute(
    "data-asset",
    "portraits/job-7-female",
  )
})

test("JobBook_UsesMalePortraitOtherwise", () => {
  renderBook("job-7", null)

  expect(screen.getByRole("img", { name: "術士" })).toHaveAttribute(
    "data-asset",
    "portraits/job-7-male",
  )
})

test("JobBook_ShowsRoleDescriptionAndFastestGrowth", () => {
  renderBook("job-6", 1)

  const page = screen.getByRole("tabpanel", { name: "戰士" })
  expect(page).toHaveTextContent("近身前排，攻守兼備")
  expect(page).toHaveTextContent("持劍披甲站在最前線")
  // 戰士成長：力量、生命、體質最高 +1～3
  const growth = within(screen.getByRole("list", { name: "成長" }))
  expect(growth.getByText("力量").closest("li")).toHaveClass("job-book-growth--top")
  expect(growth.getByText("力量").closest("li")).toHaveTextContent("+1～3")
  expect(growth.getByText("魅力").closest("li")).toHaveTextContent("+1")
  expect(growth.getByText("魅力").closest("li")).not.toHaveClass("job-book-growth--top")
})

test("JobBook_ListsWarriorSkillsWithPrerequisiteAndMaxLevel", () => {
  renderBook("job-6", 1)

  const skills = within(screen.getByRole("list", { name: "技能" })).getAllByRole("listitem")
  expect(skills).toHaveLength(2)
  expect(skills[0]).toHaveTextContent("爆氣")
  expect(skills[0]).toHaveTextContent("入門")
  expect(skills[0]).toHaveTextContent("最高 Lv10")
  expect(skills[0]).toHaveTextContent("Lv1爆氣 Lv1 的效果")
  expect(skills[0]).toHaveTextContent("Lv10爆氣 Lv10 的效果")
  expect(skills[1]).toHaveTextContent("防禦心得")
  expect(skills[1]).toHaveTextContent("前置 爆氣 Lv3")
  expect(skills[1]).toHaveTextContent("最高 Lv5")
  expect(skills[1]).toHaveTextContent("Lv5防禦心得 Lv5 的效果")
})

test("JobBook_PickFillsJobRowAndCloses", async () => {
  render(
    <CreateScreen
      api={{ createCharacter: vi.fn() }}
      jobs={makePublicJobs()}
      options={makeCreationOptions()}
      account="newbie"
      onCreated={vi.fn()}
      onLeave={vi.fn()}
    />,
  )
  await user.click(screen.getByRole("button", { name: /^職業/ }))
  expect(screen.getByRole("tabpanel", { name: "戰士" })).toBeInTheDocument()

  await user.click(screen.getByRole("tab", { name: "盜賊" }))
  expect(screen.getByTestId("lise-line")).toHaveTextContent("盜賊總是比怪物先動手呢")
  await user.click(screen.getByRole("button", { name: "就職" }))

  expect(screen.getByRole("button", { name: /^職業/ })).toHaveAccessibleName("職業 盜賊")
  expect(screen.queryByRole("tabpanel")).not.toBeInTheDocument()
  expect(screen.getByTestId("lise-line")).toHaveTextContent("那就寫上「盜賊」囉。")
})
