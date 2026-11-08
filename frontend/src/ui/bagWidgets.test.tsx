import { render, screen } from "@testing-library/react"
import { useState } from "react"
import { expect, test } from "vitest"
import { makeItem } from "../test/fixtures"
import { user } from "../test/user"
import { ItemDetail, TabPanel, Tabs } from "./bagWidgets"

const options = [
  { id: "a", label: "甲" },
  { id: "b", label: "乙" },
  { id: "c", label: "丙" },
]

function Harness() {
  const [selected, setSelected] = useState("a")
  return (
    <>
      <Tabs
        id="test-tabs"
        label="測試分頁"
        options={options}
        selected={selected}
        onSelect={setSelected}
      />
      <TabPanel tabsId="test-tabs" selected={selected}>
        <p>{`內容 ${selected}`}</p>
      </TabPanel>
    </>
  )
}

test("Tabs_SelectedTabControlsTheLabelledPanel", () => {
  render(<Harness />)

  const tab = screen.getByRole("tab", { name: "甲" })
  const panel = screen.getByRole("tabpanel", { name: "甲" })
  expect(tab).toHaveAttribute("aria-controls", panel.id)
  expect(panel).toHaveTextContent("內容 a")
  expect(tab).toHaveAttribute("tabindex", "0")
  expect(screen.getByRole("tab", { name: "乙" })).toHaveAttribute("tabindex", "-1")
})

test("Tabs_ArrowKeysHomeAndEndMoveSelectionAndFocus", async () => {
  render(<Harness />)
  await user.click(screen.getByRole("tab", { name: "甲" }))

  await user.keyboard("{ArrowRight}")
  expect(screen.getByRole("tab", { name: "乙" })).toHaveFocus()
  expect(screen.getByRole("tab", { name: "乙" })).toHaveAttribute("aria-selected", "true")
  expect(screen.getByRole("tabpanel", { name: "乙" })).toHaveTextContent("內容 b")

  await user.keyboard("{End}")
  expect(screen.getByRole("tab", { name: "丙" })).toHaveFocus()

  await user.keyboard("{ArrowRight}")
  expect(screen.getByRole("tab", { name: "甲" })).toHaveFocus()

  await user.keyboard("{ArrowLeft}")
  expect(screen.getByRole("tab", { name: "丙" })).toHaveFocus()

  await user.keyboard("{Home}")
  expect(screen.getByRole("tab", { name: "甲" })).toHaveFocus()
  expect(screen.getByRole("tab", { name: "甲" })).toHaveAttribute("aria-selected", "true")
})

test("ItemDetail_WeaponWithSkill_ShowsSkillLine", () => {
  render(<ItemDetail item={makeItem({ weaponSkill: { name: "爆氣", level: 2, rate: 12 } })} />)

  expect(screen.getByText("附帶技能 爆氣 Lv2（發動率 12%）")).toBeInTheDocument()
})

test("ItemDetail_WeaponWithoutSkill_ShowsNoSkillLine", () => {
  render(<ItemDetail item={makeItem()} />)

  expect(screen.queryByText(/附帶技能/)).not.toBeInTheDocument()
})
