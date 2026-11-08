import { fileURLToPath } from "node:url"
import { runCli } from "./cli.ts"
import { createR2Storage } from "./r2Storage.ts"

const repositoryRoot = fileURLToPath(new URL("../../..", import.meta.url))

process.exitCode = await runCli(
  process.argv.slice(2),
  { log: (line) => console.log(line), error: (line) => console.error(line) },
  {
    repositoryRoot,
    workingDirectory: process.env.INIT_CWD ?? process.cwd(),
    createStorage: createR2Storage,
  },
)
