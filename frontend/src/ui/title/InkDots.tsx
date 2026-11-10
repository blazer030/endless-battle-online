import "./InkDots.css"

// 讀取中：三個輪流跳動的墨點；位置與何時出現由使用的地方決定
export const InkDots = ({ className }: { className: string }) => (
  <div className={`ink-dots ${className}`} aria-hidden="true">
    <i />
    <i />
    <i />
  </div>
)
