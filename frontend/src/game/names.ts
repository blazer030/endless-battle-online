// 後端以 rune 計名稱長度；UTF-16 的 length 會把 emoji 算成 2
export const nameLength = (name: string) => [...name.trim()].length

// 與後端的上限一致；下限交給伺服器回報
export const CharacterNameMaxLength = 12
export const PetNameMaxLength = 12
export const MercenaryNameMaxLength = 8

export const NameLengthError = "名稱長度不符"

export const isNameTooLong = (name: string, maxLength: number) => nameLength(name) > maxLength

// 輸入超過上限時只截掉這次新插入、超出的部分，原本的字都留著（同瀏覽器原生 maxLength，但以字碼計）
export const fitName = (previous: string, next: string, maxLength: number) => {
  if (nameLength(next) <= maxLength) return next
  const before = [...previous]
  const after = [...next]
  let start = 0
  while (start < before.length && start < after.length && before[start] === after[start]) start += 1
  let end = 0
  while (
    end < before.length - start &&
    end < after.length - start &&
    before[before.length - 1 - end] === after[after.length - 1 - end]
  )
    end += 1
  const head = after.slice(0, start).join("")
  const tail = after.slice(after.length - end).join("")
  const inserted = after.slice(start, after.length - end)
  for (let keep = inserted.length; keep > 0; keep -= 1) {
    const candidate = head + inserted.slice(0, keep).join("") + tail
    if (nameLength(candidate) <= maxLength) return candidate
  }
  return nameLength(head + tail) <= maxLength ? head + tail : previous
}
