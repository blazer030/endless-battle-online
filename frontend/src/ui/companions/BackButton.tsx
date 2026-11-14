type Props = { label: string; disabled?: boolean; onClick: () => void }

// 左上返回鍵（同旅館）；送出中可停用
export const BackButton = ({ label, disabled = false, onClick }: Props) => (
  <button type="button" className="inn__back" disabled={disabled} onClick={onClick}>
    <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24">
      <path d="M14.5 5.5c-2.2 2.3-4.4 4.4-6.5 6.4 2 2.1 4.2 4.3 6.6 6.6" />
    </svg>
    {label}
  </button>
)
