import { fileURLToPath } from "node:url"

export const repositoryRoot = fileURLToPath(new URL("../../../..", import.meta.url))

export const contentDirectory = fileURLToPath(
  new URL("../../../../backend/content", import.meta.url),
)
