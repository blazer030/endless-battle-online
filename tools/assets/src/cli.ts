import { mkdir, readFile, writeFile } from "node:fs/promises"
import { dirname, extname, join, resolve } from "node:path"
import { loadCredentials, type Credentials } from "./credentials.ts"
import { convertDesignAsset } from "./design.ts"
import { missingReport } from "./missing.ts"
import { renderPreview } from "./preview.ts"
import { AssetError, Specs, parseCategory, processImage, validateKey } from "./process.ts"
import { publishAsset } from "./publish.ts"
import type { Storage } from "./storage.ts"

export type Output = { log: (line: string) => void; error: (line: string) => void }

export type CliEnvironment = {
  repositoryRoot: string
  // 使用者下指令時所在的資料夾（pnpm 會把 cwd 換成套件資料夾），來源路徑以它為準
  workingDirectory: string
  createStorage: (credentials: Credentials) => Storage
}

const Usage = [
  "用法：pnpm cli <指令>",
  "  import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]",
  "  missing [<類別>]",
  "  design <分類> <名稱> <來源圖> [--max-width <n>]",
]

export const manifestPathOf = (root: string) =>
  join(root, "frontend/src/assets/content-manifest.json")

type Parsed = { positional: string[]; flags: Set<string>; options: Map<string, string> }

const parseArgs = (args: readonly string[], valued: readonly string[]): Parsed => {
  const parsed: Parsed = { positional: [], flags: new Set(), options: new Map() }
  for (let index = 0; index < args.length; index++) {
    const arg = args[index] ?? ""
    if (valued.includes(arg)) {
      const value = args[index + 1]
      if (value === undefined) throw new AssetError(`${arg} 後面要接數值`)
      parsed.options.set(arg, value)
      index++
    } else if (arg.startsWith("--")) parsed.flags.add(arg)
    else parsed.positional.push(arg)
  }
  return parsed
}

export const parseNonNegativeInteger = (
  option: string,
  text: string | undefined,
): number | undefined => {
  if (text === undefined) return undefined
  if (!/^\d+$/.test(text)) throw new AssetError(`${option} 要是 0 以上的整數：${text}`)
  return Number(text)
}

export const readSource = async (environment: CliEnvironment, path: string) => {
  const full = resolve(environment.workingDirectory, path)
  try {
    return { bytes: await readFile(full), extension: extname(full).slice(1) }
  } catch {
    throw new AssetError(`讀不到來源圖：${path}`)
  }
}

const runImport = async (
  args: readonly string[],
  output: Output,
  environment: CliEnvironment,
): Promise<number> => {
  const parsed = parseArgs(args, ["--head-y"])
  const unknownFlag = [...parsed.flags].find((flag) => flag !== "--dry-run")
  if (unknownFlag !== undefined) throw new AssetError(`不認得的選項：${unknownFlag}`)
  const [categoryText, key, sourcePath, ...extra] = parsed.positional
  if (
    categoryText === undefined ||
    key === undefined ||
    sourcePath === undefined ||
    extra.length > 0
  )
    throw new AssetError("用法：import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]")
  const category = parseCategory(categoryText)
  validateKey(key)
  const headY = parseNonNegativeInteger("--head-y", parsed.options.get("--head-y"))
  if (headY !== undefined && category !== "portraits")
    throw new AssetError("--head-y 只能用在 portraits")
  const dryRun = parsed.flags.has("--dry-run")

  const source = await readSource(environment, sourcePath)
  const processed = await processImage(category, source.bytes, headY === undefined ? {} : { headY })
  const spec = Specs[category]
  output.log(
    `輸出 ${spec.width}×${spec.height}、品質 ${processed.quality}、${processed.webp.length.toLocaleString()} bytes`,
  )
  for (const warning of processed.warnings) output.log(`注意：${warning}`)

  const previewPath = join(environment.repositoryRoot, ".asset-preview", category, `${key}.png`)
  await mkdir(dirname(previewPath), { recursive: true })
  await writeFile(previewPath, await renderPreview(category, processed.webp))
  output.log(`預覽圖：${previewPath}`)

  if (dryRun) {
    output.log("試轉完成，沒有上傳、清單沒有變更")
    return 0
  }
  const credentials = await loadCredentials(join(environment.repositoryRoot, ".env.r2"))
  const entry = await publishAsset({
    category,
    key,
    webp: processed.webp,
    source: source.bytes,
    sourceExtension: source.extension,
    ...(headY === undefined ? {} : { headY }),
    storage: environment.createStorage(credentials),
    manifestPath: manifestPathOf(environment.repositoryRoot),
  })
  output.log(`已上傳並寫進清單：${entry.file}`)
  return 0
}

const runMissing = async (
  args: readonly string[],
  output: Output,
  environment: CliEnvironment,
): Promise<number> => {
  if (args.length > 1) throw new AssetError("用法：missing [<類別>]")
  const categories = args[0] === undefined ? undefined : [parseCategory(args[0])]
  const lines = await missingReport(
    join(environment.repositoryRoot, "backend/content"),
    manifestPathOf(environment.repositoryRoot),
    categories,
  )
  for (const line of lines) output.log(line)
  return 0
}

const runDesign = async (
  args: readonly string[],
  output: Output,
  environment: CliEnvironment,
): Promise<number> => {
  const parsed = parseArgs(args, ["--max-width"])
  if (parsed.flags.size > 0) throw new AssetError(`不認得的選項：${[...parsed.flags].join("、")}`)
  const [group, name, sourcePath, ...extra] = parsed.positional
  if (group === undefined || name === undefined || sourcePath === undefined || extra.length > 0)
    throw new AssetError("用法：design <分類> <名稱> <來源圖> [--max-width <n>]")
  validateKey(group)
  validateKey(name)
  const maxWidth = parseNonNegativeInteger("--max-width", parsed.options.get("--max-width"))
  const source = await readSource(environment, sourcePath)
  const webp = await convertDesignAsset(source.bytes, maxWidth)
  const target = join(environment.repositoryRoot, "frontend/src/assets", group, `${name}.webp`)
  await mkdir(dirname(target), { recursive: true })
  await writeFile(target, webp)
  output.log(`已寫出 ${target}（${webp.length.toLocaleString()} bytes）`)
  return 0
}

export const runCli = async (
  args: readonly string[],
  output: Output,
  environment: CliEnvironment,
): Promise<number> => {
  const [command, ...rest] = args
  try {
    switch (command) {
      case "import":
        return await runImport(rest, output, environment)
      case "missing":
        return await runMissing(rest, output, environment)
      case "design":
        return await runDesign(rest, output, environment)
      default:
        output.error(`不認得的指令：${command ?? "（沒有指令）"}`)
        for (const line of Usage) output.error(line)
        return 1
    }
  } catch (error) {
    if (error instanceof AssetError) output.error(error.message)
    else output.error(`失敗：${error instanceof Error ? error.message : String(error)}`)
    return 1
  }
}
