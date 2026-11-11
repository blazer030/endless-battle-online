// 畫面上顯示名稱用的公開資料（/api/public/jobs、/api/public/settings）；載入失敗時用空表，畫面改顯示 id
import type { CreationOption } from "./creationOptions"

export type NamedJob = { readonly id: string; readonly name: string }

export type PublicNames = {
  readonly jobs: readonly NamedJob[]
  readonly sexes: readonly CreationOption[]
  readonly elements: readonly CreationOption[]
  readonly births: readonly CreationOption[]
}

export const emptyPublicNames: PublicNames = { jobs: [], sexes: [], elements: [], births: [] }

export const nameOf = <Id extends string | number>(
  options: readonly { readonly id: Id; readonly name: string }[],
  id: Id,
) => options.find((option) => option.id === id)?.name ?? String(id)
