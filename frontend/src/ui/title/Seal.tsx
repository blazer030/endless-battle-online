import "./Seal.css"

type Props = {
  label: string
  disabled?: boolean
  className?: string
  onClick: () => void
}

// 紅印章按鈕：入城、登記、出發、就職、重試；字直書
export const Seal = ({ label, disabled = false, className = "", onClick }: Props) => (
  <button type="button" className={`seal ${className}`} disabled={disabled} onClick={onClick}>
    <span>{label}</span>
  </button>
)
