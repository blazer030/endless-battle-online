// 原因代碼全部認得時以「、」串起對照文字；有任何認不得的代碼或沒有代碼時改用後端的 detail
export const translateReasons = (
  texts: Readonly<Record<string, string>>,
  reasons: string[],
  fallback: string,
): string =>
  reasons.length === 0 || !reasons.every((code) => Object.hasOwn(texts, code))
    ? fallback
    : reasons.map((code) => texts[code]).join("、")
