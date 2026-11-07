import { createContentRepository } from "../shared/repository"
import { SkillConverter } from "./SkillConverter"

export const skillRepository = createContentRepository("skills", SkillConverter)
