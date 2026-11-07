import { createContentRepository } from "../shared/repository"
import { MonsterConverter } from "./MonsterConverter"

export const monsterRepository = createContentRepository("monsters", MonsterConverter)
