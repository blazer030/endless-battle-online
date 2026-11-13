// 名字旁的改名鉛筆鈕
export const PenButton = ({ disabled, onClick }: { disabled: boolean; onClick: () => void }) => (
  <button
    type="button"
    className="companions-head__pen"
    aria-label="改名"
    disabled={disabled}
    onClick={onClick}
  >
    <svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24">
      <path d="M4 20l4.5-1 10-10-3.5-3.5-10 10z" />
    </svg>
  </button>
)
