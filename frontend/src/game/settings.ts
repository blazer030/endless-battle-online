import { translateReasons } from "./reasons"
import type { AdventureSettingsCommand, AttackKind, CharacterView } from "./types"

export type AttackChoice = AttackKind | "auto"

// 表單欄位一律是字串，送出前才轉成整數
export type SettingsForm = {
  attack: AttackChoice
  potionHpPercent: string
  potionSpPercent: string
  restHpPercent: string
  restSpPercent: string
  escapeHpPercent: string
  specialAttackName: string
}

export type ParsedSettings =
  { ok: true; command: AdventureSettingsCommand } | { ok: false; error: string }

// WOG js/wog_fight.js 的選項文字
export const attackChoices: { value: AttackChoice; label: string }[] = [
  { value: "auto", label: "自動" },
  { value: "Physical", label: "物理攻擊" },
  { value: "Magic", label: "魔法攻擊" },
]

export const InvalidPercentMessage = "門檻必須是 -1～100 的整數（逃跑 0～100）"

const reasonTexts: Record<string, string> = {
  invalidPercent: InvalidPercentMessage,
  nameInvalid: "必殺技名稱必須是 1～8 個字",
}

export const settingsErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)

export const settingsForm = (character: CharacterView): SettingsForm => {
  const { settings } = character
  return {
    attack: settings.attack ?? "auto",
    potionHpPercent: String(settings.potionHpPercent),
    potionSpPercent: String(settings.potionSpPercent),
    restHpPercent: String(settings.restHpPercent),
    restSpPercent: String(settings.restSpPercent),
    escapeHpPercent: String(settings.escapeHpPercent),
    specialAttackName: character.specialAttackName,
  }
}

const parsePercent = (text: string, minimum: number): number | null => {
  const trimmed = text.trim()
  if (!/^-?\d+$/.test(trimmed)) return null
  const value = Number(trimmed)
  return value >= minimum && value <= 100 ? value : null
}

// 補給與住宿 -1～100（-1 略過條件），逃跑 0～100
export const parseSettings = (form: SettingsForm): ParsedSettings => {
  const potionHpPercent = parsePercent(form.potionHpPercent, -1)
  const potionSpPercent = parsePercent(form.potionSpPercent, -1)
  const restHpPercent = parsePercent(form.restHpPercent, -1)
  const restSpPercent = parsePercent(form.restSpPercent, -1)
  const escapeHpPercent = parsePercent(form.escapeHpPercent, 0)
  if (
    potionHpPercent === null ||
    potionSpPercent === null ||
    restHpPercent === null ||
    restSpPercent === null ||
    escapeHpPercent === null
  )
    return { ok: false, error: InvalidPercentMessage }
  return {
    ok: true,
    command: {
      attack: form.attack === "auto" ? null : form.attack,
      potionHpPercent,
      potionSpPercent,
      restHpPercent,
      restSpPercent,
      escapeHpPercent,
      specialAttackName: form.specialAttackName,
    },
  }
}
