import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test } from "vitest"
import type { LogLine } from "../game/battleLog"
import { BattleLog } from "./BattleLog"

test("BattleLog_FiltersByTab", async () => {
  const lines: LogLine[] = [
    { id: 1, text: "墨靈現身。", kind: "normal" },
    { id: 2, text: "獲得修為 5、靈石 1。", kind: "drop" },
    { id: 3, text: "你一劍穿心，會心 99！", kind: "trigger" },
  ]
  render(<BattleLog lines={lines} />)
  expect(screen.getAllByRole("listitem")).toHaveLength(3)

  await userEvent.click(screen.getByRole("tab", { name: "掉落" }))

  expect(screen.getByRole("tab", { name: "掉落" })).toHaveAttribute("aria-selected", "true")
  const items = screen.getAllByRole("listitem")
  expect(items).toHaveLength(1)
  expect(items[0]).toHaveTextContent("獲得修為 5、靈石 1。")
})
