import { readdir, readFile, writeFile } from "node:fs/promises"
import { join, resolve } from "node:path"
import { ContentFileError } from "./contentFile.ts"
import {
  assertLossless,
  exportedFingerprints,
  fingerprintOf,
  fromTables,
  TableError,
  toTables,
  type ContentFiles,
} from "./tables.ts"
import { readWorkbook, WorkbookError, writeWorkbook } from "./workbook.ts"

export type Output = { log: (line: string) => void; error: (line: string) => void }

export type CliEnvironment = {
  repositoryRoot: string
  // 使用者下指令時所在的資料夾（pnpm 會把 cwd 換成套件資料夾），檔案路徑以它為準
  workingDirectory: string
}

const Usage = [
  "用法：pnpm cli <指令>",
  "  export [<輸出.xlsx>]   遊戲內容匯出成試算表（預設 content.xlsx）",
  "  import <試算表.xlsx> [--force]",
  "                         試算表寫回 backend/content/*.json；匯出後被改過的內容檔要加 --force 才會蓋掉",
]

const contentDirectoryOf = (root: string) => join(root, "backend/content")

const readContent = async (root: string): Promise<ContentFiles> => {
  const directory = contentDirectoryOf(root)
  const names = (await readdir(directory)).filter((name) => name.endsWith(".json")).sort()
  return new Map(
    await Promise.all(
      names.map(async (name) => [name, await readFile(join(directory, name), "utf8")] as const),
    ),
  )
}

const exportWorkbook = async (output: string, environment: CliEnvironment, out: Output) => {
  const files = await readContent(environment.repositoryRoot)
  assertLossless(files)
  const tables = toTables(files)
  await writeFile(output, await writeWorkbook(tables))
  out.log(`已匯出 ${files.size} 個內容檔、${tables.length} 張工作表到 ${output}`)
}

const importWorkbook = async (
  input: string,
  force: boolean,
  environment: CliEnvironment,
  out: Output,
) => {
  const current = await readContent(environment.repositoryRoot)
  const tables = await readWorkbook(await readFile(input))
  const files = fromTables(tables, current)
  const changed = [...files].filter(([name, text]) => current.get(name) !== text)
  const exported = exportedFingerprints(tables)
  const stale = changed
    .map(([name]) => name)
    .filter((name) => {
      const text = current.get(name)
      return text !== undefined && exported.get(name) !== fingerprintOf(text)
    })
  if (stale.length > 0 && !force)
    throw new TableError(
      stale.map((name) => `${name} 在匯出後改過，請重新匯出再改，或加 --force 蓋掉`).join("\n"),
    )
  for (const [name, text] of changed) {
    await writeFile(join(contentDirectoryOf(environment.repositoryRoot), name), text)
    out.log(`已更新 ${name}`)
  }
  out.log(changed.length === 0 ? "內容檔沒有變更" : "記得跑 ./scripts/check.sh 確認內容可以載入")
}

export const runCli = async (
  args: readonly string[],
  out: Output,
  environment: CliEnvironment,
): Promise<number> => {
  const force = args.includes("--force")
  const [command, path] = args.filter((arg) => arg !== "--force")
  const pathOf = (value: string) => resolve(environment.workingDirectory, value)
  try {
    if (command === "export") {
      await exportWorkbook(pathOf(path ?? "content.xlsx"), environment, out)
      return 0
    }
    if (command === "import" && path !== undefined) {
      await importWorkbook(pathOf(path), force, environment, out)
      return 0
    }
    for (const line of Usage) out.error(line)
    return 1
  } catch (error) {
    if (
      error instanceof TableError ||
      error instanceof WorkbookError ||
      error instanceof ContentFileError
    ) {
      out.error(error.message)
      return 1
    }
    throw error
  }
}
