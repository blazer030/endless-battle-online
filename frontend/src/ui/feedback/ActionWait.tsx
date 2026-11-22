import { InkDots } from "../title/InkDots"
import type { ActionFeedback } from "./useActionFeedback"
import "./ActionWait.css"

type Props = { feedback: Pick<ActionFeedback, "phase"> }

// 等待伺服器時蓋在舞台上的透明擋點擊層；等久了正中間出墨點底板
export const ActionWait = ({ feedback: { phase } }: Props) => {
  if (phase === "idle") return null
  const showDots = phase === "dots" || phase === "leaving"
  return (
    <div className={`action-wait${phase === "leaving" ? " action-wait--leaving" : ""}`}>
      {showDots && (
        <div className="action-wait__plate">
          <InkDots className="action-wait__dots" label="等待中" />
        </div>
      )}
    </div>
  )
}
