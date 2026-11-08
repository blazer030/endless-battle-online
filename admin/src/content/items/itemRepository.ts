import { createContentRepository } from "../shared/repository"
import { ItemConverter } from "./ItemConverter"

export const itemRepository = createContentRepository("items", ItemConverter)
