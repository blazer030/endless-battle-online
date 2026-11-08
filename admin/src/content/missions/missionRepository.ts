import { createContentRepository } from "../shared/repository"
import { MissionConverter } from "./MissionConverter"

export const missionRepository = createContentRepository("missions", MissionConverter)
