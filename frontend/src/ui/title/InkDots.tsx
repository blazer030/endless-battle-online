import "./InkDots.css"

type Props = {
  className: string
  // 給了名稱就是畫面上唯一的讀取提示（讀屏會念出來）；沒給就只是裝飾
  label?: string
}

// 讀取中：三個輪流跳動的墨點；位置與何時出現由使用的地方決定
export const InkDots = ({ className, label }: Props) => (
  <div
    className={`ink-dots ${className}`}
    {...(label === undefined ? { "aria-hidden": true } : { role: "status", "aria-label": label })}
  >
    <i />
    <i />
    <i />
  </div>
)
