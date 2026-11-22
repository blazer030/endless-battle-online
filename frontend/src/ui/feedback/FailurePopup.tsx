import { Popup } from "./Popup"
import "./FailurePopup.css"

type Props = { text: string; onClose: () => void }

// 全遊戲共用的失敗彈窗：紅蠟封「！」＋一句紅字（連線失敗或原因）＋確定
export const FailurePopup = ({ text, onClose }: Props) => (
  <Popup label="失敗" role="alertdialog" className="failure-popup" onClose={onClose}>
    <span className="failure-popup__mark" aria-hidden="true">
      ！
    </span>
    <p className="failure-popup__text">{text}</p>
  </Popup>
)
