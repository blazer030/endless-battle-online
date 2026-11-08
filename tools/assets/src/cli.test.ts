import { describe, expect, it } from "vitest"
import { runCli } from "./cli.ts"

const capture = () => {
  const lines: string[] = []
  return {
    lines,
    output: { log: (line: string) => lines.push(line), error: (line: string) => lines.push(line) },
  }
}

describe("runCli", () => {
  it("未知指令以非零 exit code 結束並印出可用指令", async () => {
    const { lines, output } = capture()

    const exitCode = await runCli(["unknown"], output)

    expect(exitCode).not.toBe(0)
    const text = lines.join("\n")
    for (const command of ["import", "missing", "design"]) expect(text).toContain(command)
  })
})
