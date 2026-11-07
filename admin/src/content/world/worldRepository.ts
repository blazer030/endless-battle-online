import { createContentRepository } from "../shared/repository"
import { AreaConverter, PlaceConverter } from "./WorldConverter"

export const areaRepository = createContentRepository("areas", AreaConverter)

export const placeRepository = createContentRepository("places", PlaceConverter)
