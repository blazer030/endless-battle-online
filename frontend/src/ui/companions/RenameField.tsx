import { useRef, useState } from "react"
import { fitName, nameLength } from "../../game/names"

type Props = {
  label: string
  initial: string
  maxLength: number
  busy: boolean
  onConfirm: (name: string) => void
  onCancel: () => void
}

// 改名欄：旁邊顯示字數 n / 上限（去頭尾空白、以字碼計，與伺服器一致），0 字時紅、確定停用
// 超過上限時新輸入打不進去（只截掉超出的部分）；注音、拼音選字中不截，選完才截。不用輸入框的 maxLength，它以 UTF-16 計，emoji 會算兩個字
export const RenameField = ({ label, initial, maxLength, busy, onConfirm, onCancel }: Props) => {
  const [draft, setDraft] = useState(initial)
  // 選字開始前的名字；選完時拿它比對這次新插入了什麼
  const beforeComposing = useRef<string | null>(null)
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
        onChange={(event) => {
          const next = event.target.value
          setDraft(beforeComposing.current === null ? fitName(draft, next, maxLength) : next)
        }}
        onCompositionStart={() => {
          beforeComposing.current = draft
        }}
        onCompositionEnd={(event) => {
          const previous = beforeComposing.current ?? draft
          beforeComposing.current = null
          setDraft(fitName(previous, event.currentTarget.value, maxLength))
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
