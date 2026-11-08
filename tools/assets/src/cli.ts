export type Output = { log: (line: string) => void; error: (line: string) => void }

const Usage = [
  "用法：pnpm cli <指令>",
  "  import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]",
  "  missing [<類別>]",
  "  design <分類> <名稱> <來源圖> [--max-width <n>]",
]

export const runCli = async (args: readonly string[], output: Output): Promise<number> => {
  const [command] = args
  output.error(`不認得的指令：${command ?? "（沒有指令）"}`)
  for (const line of Usage) output.error(line)
  return 1
}
