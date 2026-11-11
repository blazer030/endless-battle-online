import { useId, useState, type FormEvent } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  attackChoices,
  parseSettings,
  settingsErrorMessage,
  settingsForm,
  type AttackChoice,
  type SettingsForm,
} from "../game/settings"
import type { CharacterVo } from "../game/character"
import "./SettingsPanel.css"

export type SettingsApi = Pick<ApiClient, "updateSettings">

type Props = {
  api: SettingsApi
  character: CharacterVo
  onCharacterChanged: (character: CharacterVo) => void
  onClose: () => void
}

const fallbackError = "發生錯誤，請稍後再試"
const conflictError = "角色資料已被其他請求更新，請再試一次"

const describeError = (error: unknown) => {
  if (!(error instanceof ApiError)) return fallbackError
  if (error.status === 409) return conflictError
  return settingsErrorMessage(error.reasons, error.detail)
}

// 值一律來自 attackChoices；找不到時退回自動
const attackChoiceOf = (value: string): AttackChoice =>
  attackChoices.find((choice) => choice.value === value)?.value ?? "auto"

type PercentField = Exclude<keyof SettingsForm, "attack">

// 文案照 WOG js/wog_fight.js 的戰鬥設定
export function SettingsPanel({ api, character, onCharacterChanged, onClose }: Props) {
  const titleId = useId()
  const [form, setForm] = useState(() => settingsForm(character))
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const percent = (field: PercentField, label: string) => (
    <input
      className="settings__percent"
      type="text"
      aria-label={label}
      maxLength={3}
      value={form[field]}
      onChange={(event) => setForm({ ...form, [field]: event.target.value })}
    />
  )

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const parsed = parseSettings(form)
    if (!parsed.ok) {
      setError(parsed.error)
      return
    }
    setBusy(true)
    setError(null)
    try {
      onCharacterChanged(await api.updateSettings(parsed.command))
      onClose()
    } catch (caught) {
      setError(describeError(caught))
      setBusy(false)
    }
  }

  return (
    <section className="settings" role="dialog" aria-labelledby={titleId}>
      <header className="settings__head">
        <h2 id={titleId} className="settings__title">
          冒險設定
        </h2>
        <button type="button" className="settings__close" onClick={onClose}>
          關閉
        </button>
      </header>
      <form className="settings__body" onSubmit={(event) => void save(event)}>
        <label className="settings__row">
          攻擊方式
          <select
            className="settings__select"
            value={form.attack}
            onChange={(event) => setForm({ ...form, attack: attackChoiceOf(event.target.value) })}
          >
            {attackChoices.map((choice) => (
              <option key={choice.value} value={choice.value}>
                {choice.label}
              </option>
            ))}
          </select>
        </label>
        <p className="settings__row">
          HP低於 {percent("potionHealthPointsPercent", "恢復道具 HP 門檻")} %，SP低於{" "}
          {percent("potionManaPointsPercent", "恢復道具 SP 門檻")} %自動使用恢復道具
        </p>
        <p className="settings__row">
          HP低於 {percent("restHealthPointsPercent", "住宿 HP 門檻")} % ， SP低於{" "}
          {percent("restManaPointsPercent", "住宿 SP 門檻")} %自動住宿(-1表示略過條件)
        </p>
        <p className="settings__row">
          HP低於 {percent("escapeHealthPointsPercent", "逃跑 HP 門檻")} %逃離戰鬥
        </p>
        <p className="settings__note">
          恢復道具的 -1 也表示略過條件；SP 預設 100% 時，SP 全滿不會使用恢復道具。
        </p>
        {error !== null && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <div className="actions">
          <button type="submit" disabled={busy}>
            儲存
          </button>
        </div>
      </form>
    </section>
  )
}
