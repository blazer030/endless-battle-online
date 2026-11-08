// 比對 id 與名稱：不分大小寫、部分比對；空白代表不篩選
export function matchesSearch(record: { id: string | number; name: string }, search: string) {
  const text = search.trim().toLowerCase()
  return (
    text === "" ||
    String(record.id).toLowerCase().includes(text) ||
    record.name.toLowerCase().includes(text)
  )
}
