import { request } from "../../api/dataProvider"
import { parseArray } from "./parse"

export type ContentConverter<TDto, TVo> = {
  toVo: (dto: TDto, path?: string) => TVo
  toDto: (vo: TVo) => TDto
}

export type ContentRepository<TVo> = {
  queryKey: readonly string[]
  list: () => Promise<TVo[]>
  update: (id: string, vo: TVo) => Promise<void>
  create: (vo: TVo) => Promise<void>
}

// 內容 API 的存取；回傳與接收的都是 VO，API 的形狀只存在 Converter
export function createContentRepository<TDto, TVo>(
  kind: string,
  converter: ContentConverter<TDto, TVo>,
): ContentRepository<TVo> {
  const url = `/admin/content/${kind}`
  const send = (target: string, method: string, vo: TVo) =>
    request<void>(target, { method, body: JSON.stringify(converter.toDto(vo)) })

  return {
    queryKey: ["admin", "content", kind],
    list: async () =>
      parseArray(await request<unknown>(url), kind, (item, path) =>
        converter.toVo(item as TDto, path),
      ),
    update: async (id, vo) => {
      await send(`${url}/${encodeURIComponent(id)}`, "PUT", vo)
    },
    create: async (vo) => {
      await send(url, "POST", vo)
    },
  }
}
