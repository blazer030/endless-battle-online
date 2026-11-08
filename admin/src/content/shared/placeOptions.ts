import { useContentList } from "./repository"
import { placeRepository } from "../world/worldRepository"
import type { PlaceVo } from "../world/WorldVo"

export const usePlaceOptions = () => useContentList(placeRepository)

export const globalPlaceId = 0

export const globalPlaceName = "全域"

export function placeNameOf(places: readonly PlaceVo[], placeId: number) {
  if (placeId === globalPlaceId) {
    return globalPlaceName
  }
  return places.find((place) => place.id === placeId)?.name ?? String(placeId)
}
