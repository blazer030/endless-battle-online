import { render, screen } from "@testing-library/react"
import { user } from "../test/user"
import { expect, test } from "vitest"
import type { LogLine } from "../game/battleLog"
import { BattleLog } from "./BattleLog"

test("BattleLog_FiltersByTab", async () => {
  const lines: LogLine[] = [
    { id: 1, text: "遭遇 史萊姆！", kind: "normal" },
    { id: 2, text: "獲得經驗 5、金錢 1、熟練度 1。", kind: "drop" },
    { id: 3, text: "暴擊！你攻擊史萊姆，造成 99 點傷害！", kind: "trigger" },
  ]
  render(<BattleLog lines={lines} />)
  expect(screen.getAllByRole("listitem")).toHaveLength(3)

  await user.click(screen.getByRole("tab", { name: "掉落" }))

  expect(screen.getByRole("tab", { name: "掉落" })).toHaveAttribute("aria-selected", "true")
  const items = screen.getAllByRole("listitem")
  expect(items).toHaveLength(1)
  expect(items[0]).toHaveTextContent("獲得經驗 5、金錢 1、熟練度 1。")
})
