import { runCli } from "./cli.ts"

process.exitCode = await runCli(process.argv.slice(2), {
  log: (line) => console.log(line),
  error: (line) => console.error(line),
})
