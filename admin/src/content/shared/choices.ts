import { matchesSearch } from "./search"

export type Choice<TValue> = { value: TValue; label: string; searchText: string }

// 名稱重複的選項（道具常見）要帶 id 才分得出來；搜尋同時比對 id 與名稱
export function namedChoices<TId extends string | number>(
  records: readonly { readonly id: TId; readonly name: string }[],
): Choice<TId>[] {
  const counts = new Map<string, number>()
  for (const record of records) {
    counts.set(record.name, (counts.get(record.name) ?? 0) + 1)
  }
  return records.map((record) => ({
    value: record.id,
    label: (counts.get(record.name) ?? 0) > 1 ? `${record.name}（${record.id}）` : record.name,
    searchText: `${record.id} ${record.name}`,
  }))
}

export const filterChoice = (input: string, option?: { searchText?: string; label?: string }) =>
  matchesSearch({ id: "", name: option?.searchText ?? option?.label ?? "" }, input)
