import { translateReasons } from "./reasons"
import type { AdventureSettings, AttackKind, CharacterVo } from "./character"

export type AttackChoice = AttackKind | "auto"

// 表單欄位一律是字串，送出前才轉成整數
export type SettingsForm = {
  attack: AttackChoice
  potionHealthPointsPercent: string
  potionManaPointsPercent: string
  restHealthPointsPercent: string
  restManaPointsPercent: string
  escapeHealthPointsPercent: string
}

export type ParsedSettings = { ok: true; command: AdventureSettings } | { ok: false; error: string }

// WOG js/wog_fight.js 的選項文字
export const attackChoices: { value: AttackChoice; label: string }[] = [
  { value: "auto", label: "自動" },
  { value: "Physical", label: "物理攻擊" },
  { value: "Magic", label: "魔法攻擊" },
]

export const InvalidPercentMessage = "門檻必須是 -1～100 的整數（逃跑 0～100）"

const reasonTexts: Record<string, string> = {
  invalidPercent: InvalidPercentMessage,
}

export const settingsErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)

export const settingsForm = (character: CharacterVo): SettingsForm => {
  const { settings } = character
  return {
    attack: settings.attack ?? "auto",
    potionHealthPointsPercent: String(settings.potionHealthPointsPercent),
    potionManaPointsPercent: String(settings.potionManaPointsPercent),
    restHealthPointsPercent: String(settings.restHealthPointsPercent),
    restManaPointsPercent: String(settings.restManaPointsPercent),
    escapeHealthPointsPercent: String(settings.escapeHealthPointsPercent),
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
  const potionHealthPointsPercent = parsePercent(form.potionHealthPointsPercent, -1)
  const potionManaPointsPercent = parsePercent(form.potionManaPointsPercent, -1)
  const restHealthPointsPercent = parsePercent(form.restHealthPointsPercent, -1)
  const restManaPointsPercent = parsePercent(form.restManaPointsPercent, -1)
  const escapeHealthPointsPercent = parsePercent(form.escapeHealthPointsPercent, 0)
  if (
    potionHealthPointsPercent === null ||
    potionManaPointsPercent === null ||
    restHealthPointsPercent === null ||
    restManaPointsPercent === null ||
    escapeHealthPointsPercent === null
  )
    return { ok: false, error: InvalidPercentMessage }
  return {
    ok: true,
    command: {
      attack: form.attack === "auto" ? null : form.attack,
      potionHealthPointsPercent,
      potionManaPointsPercent,
      restHealthPointsPercent,
      restManaPointsPercent,
      escapeHealthPointsPercent,
    },
  }
}
