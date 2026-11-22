import { useEffect, useState, type ReactNode } from "react"
import { PopupOutSeconds } from "../../motion/timings"
import "./Popup.css"

type Props = {
  label: string
  role?: "dialog" | "alertdialog"
  className?: string
  onClose: () => void
  children: ReactNode
  // 下方的按鈕；沒給就是一個「確定」。close 會先淡出再呼叫 onClose
  actions?: (close: () => void) => ReactNode
}

// 共用彈窗：紙框置中、遮罩；點遮罩或「確定」淡出後關閉
export const Popup = ({ label, role = "dialog", className, onClose, children, actions }: Props) => {
  const [closing, setClosing] = useState(false)
  // 淡出完才交給呼叫端關掉；卸載時取消
  useEffect(() => {
    if (!closing) return
    const timer = setTimeout(onClose, PopupOutSeconds * 1000)
    return () => clearTimeout(timer)
  }, [closing, onClose])
  const close = () => setClosing(true)
  return (
    <>
      <div
        className={`popup-scrim${closing ? " popup-scrim--closing" : ""}`}
        aria-hidden="true"
        onClick={close}
      />
      <div
        role={role}
        aria-modal="true"
        aria-label={label}
        className={`popup${closing ? " popup--closing" : ""}${className === undefined ? "" : ` ${className}`}`}
      >
        {children}
        {actions === undefined ? (
          <button type="button" className="popup__button popup__button--confirm" onClick={close}>
            確定
          </button>
        ) : (
          actions(close)
        )}
      </div>
    </>
  )
}
