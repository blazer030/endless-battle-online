import { useQuery } from "@tanstack/react-query"
import { request } from "../../api/request"
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

// 列表與選單共用：同一種內容只讀一次，都經過 Converter 檢查
export const useContentList = <TVo>(repository: ContentRepository<TVo>) =>
  useQuery({ queryKey: repository.queryKey, queryFn: repository.list, retry: false })
