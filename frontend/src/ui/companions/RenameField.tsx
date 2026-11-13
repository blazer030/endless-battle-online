import { useRef, useState } from "react"
import { nameLength } from "../../game/names"

type Props = {
  label: string
  initial: string
  maxLength: number
  busy: boolean
  onConfirm: (name: string) => void
  onCancel: () => void
}

// 改名欄：旁邊顯示字數 n / 上限（去頭尾空白、以字碼計，與伺服器一致），0 字時紅、確定停用
// 超過上限時截到上限；注音、拼音選字中不截，選完才截。不用輸入框的 maxLength，它以 UTF-16 計，emoji 會算兩個字
// 去掉開頭空白後最多留 max 個字碼（開頭空白保留，確定時才去頭尾）
const clip = (value: string, max: number) => {
  const body = value.trimStart()
  return value.slice(0, value.length - body.length) + [...body].slice(0, max).join("")
}

export const RenameField = ({ label, initial, maxLength, busy, onConfirm, onCancel }: Props) => {
  const [draft, setDraft] = useState(initial)
  const composing = useRef(false)
  const name = draft.trim()
  const length = nameLength(draft)
  return (
    <span className="rename-field">
      <input
        className="rename-field__input"
        type="text"
        aria-label={label}
        value={draft}
        autoFocus
        onChange={(event) =>
          setDraft(composing.current ? event.target.value : clip(event.target.value, maxLength))
        }
        onCompositionStart={() => {
          composing.current = true
        }}
        onCompositionEnd={(event) => {
          composing.current = false
          setDraft(clip(event.currentTarget.value, maxLength))
        }}
      />
      <small className={`rename-field__count${length === 0 ? " rename-field__count--empty" : ""}`}>
        {`${length} / ${maxLength}`}
      </small>
      <button
        type="button"
        className="rename-field__ok"
        disabled={busy || length === 0 || length > maxLength}
        onClick={() => onConfirm(name)}
      >
        確定
      </button>
      <button
        type="button"
        className="rename-field__cancel"
        aria-label="取消改名"
        onClick={onCancel}
      >
        ×
      </button>
    </span>
  )
}
