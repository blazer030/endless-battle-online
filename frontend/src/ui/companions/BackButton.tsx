// 左上返回鍵（同旅館）
export const BackButton = ({ label, onClick }: { label: string; onClick: () => void }) => (
  <button type="button" className="inn__back" onClick={onClick}>
    <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24">
      <path d="M14.5 5.5c-2.2 2.3-4.4 4.4-6.5 6.4 2 2.1 4.2 4.3 6.6 6.6" />
    </svg>
    {label}
  </button>
)
