// 後端以 rune 計名稱長度；UTF-16 的 length 會把 emoji 算成 2
export const nameLength = (name: string) => [...name.trim()].length

// 與後端的上限一致；下限交給伺服器回報
export const CharacterNameMaxLength = 12
export const SpecialAttackNameMaxLength = 8
export const PetNameMaxLength = 12
export const MercenaryNameMaxLength = 8

export const NameLengthError = "名稱長度不符"

export const isNameTooLong = (name: string, maxLength: number) => nameLength(name) > maxLength
