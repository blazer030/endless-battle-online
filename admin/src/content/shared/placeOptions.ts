import { useQuery } from "@tanstack/react-query"
import { request } from "../../api/dataProvider"
import { parseArray, parseNumber, parseObject, parseString } from "./parse"

export type PlaceOption = {
  readonly id: number
  readonly name: string
  readonly subPlaceNames: readonly string[]
}

// 地區還沒改成有型別的路由，這裡只讀選單用得到的欄位
async function listPlaceOptions(): Promise<PlaceOption[]> {
  return parseArray(await request<unknown>("/admin/content/places"), "places", (item, path) => {
    const place = parseObject(item, path)
    return {
      id: parseNumber(place.id, `${path}.id`),
      name: parseString(place.name, `${path}.name`),
      subPlaceNames: parseArray(place.subPlaces ?? [], `${path}.subPlaces`, parseString),
    }
  })
}

export const usePlaceOptions = () =>
  useQuery({
    queryKey: ["admin", "content", "places", "options"],
    queryFn: listPlaceOptions,
    retry: false,
  })

export const globalPlaceId = 0

export const globalPlaceName = "全域"

export function placeNameOf(places: readonly PlaceOption[], placeId: number) {
  if (placeId === globalPlaceId) {
    return globalPlaceName
  }
  return places.find((place) => place.id === placeId)?.name ?? String(placeId)
}
