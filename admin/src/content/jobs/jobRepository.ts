import { createContentRepository } from "../shared/repository"
import { JobConverter } from "./JobConverter"

export const jobRepository = createContentRepository("jobs", JobConverter)
