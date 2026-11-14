import {
  bonusRows,
  categoryLine,
  effectText,
  mercenaryRows,
  quantityBadge,
  requirementRows,
  sealBlock,
  slotLine,
  type ClerkLine,
  type HonourExchange,
} from "../../game/honour"
import { formatNumber } from "../../game/format"
import { ContentImage } from "../ContentImage"
import { KeeperSay } from "../companions/KeeperSay"
import { RewardPicture } from "./RewardPicture"

type Props = {
  exchange: HonourExchange
  hiredMercenaryName: string | null
  busy: boolean
  say: ClerkLine | null
  onExchange: () => void
  onClose: () => void
}

type Row = { label: string; value: string }

const StatGrid = ({ title, rows }: { title: string; rows: Row[] }) => (
  <>
    <h4>{title}</h4>
    <dl className="honour-sheet__stats">
      {rows.map((row) => (
        <div key={row.label}>
          <dt>{row.label}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
  </>
)

// 拉近的吊牌：詳情寫在羊皮紙上（超出時紙內捲動），兌換列、莉瑟的話、印章固定在下面
export const HonourSheet = ({
  exchange,
  hiredMercenaryName,
  busy,
  say,
  onExchange,
  onClose,
}: Props) => {
  const item = exchange.item
  const badge = quantityBadge(exchange.quantity)
  const effect = item === null ? null : effectText(item)
  const bonuses = item === null ? [] : bonusRows(item.bonus)
  const requirements = item === null ? [] : requirementRows(item)
  const slots = item === null ? null : slotLine(item)
  const blocked = sealBlock(exchange, hiredMercenaryName)
  return (
    <>
      <div
        className="honour-sheet__mask"
        aria-hidden="true"
        onClick={() => {
          if (!busy) onClose()
        }}
      />
      <section
        className="honour-sheet"
        role="dialog"
        aria-modal="true"
        aria-label={exchange.rewardName}
      >
        <button
          type="button"
          className="honour-sheet__close"
          aria-label="關閉"
          disabled={busy}
          onClick={onClose}
        />
        <div className="honour-sheet__scroll">
          <div className="honour-sheet__top">
            <span className="honour-sheet__picture">
              <RewardPicture exchange={exchange} className="honour-sheet__image" />
              {badge !== null && <span className="honour-sheet__quantity">{badge}</span>}
            </span>
            <div>
              <h3>{exchange.rewardName}</h3>
              <p className="honour-sheet__sub">{item === null ? "傭兵" : categoryLine(item)}</p>
            </div>
          </div>
          {effect !== null && <p className="honour-sheet__effect">{effect}</p>}
          {exchange.mercenary !== null && (
            <StatGrid title="契約" rows={mercenaryRows(exchange.mercenary)} />
          )}
          {bonuses.length > 0 && <StatGrid title="能力加成" rows={bonuses} />}
          {requirements.length > 0 && (
            <>
              <h4>裝備需求</h4>
              <ul className="honour-sheet__requirements">
                {requirements.map((requirement) => (
                  <li key={requirement.text} data-met={requirement.met}>
                    {requirement.text}
                  </li>
                ))}
              </ul>
            </>
          )}
          {slots !== null && <p className="honour-sheet__meta">{slots}</p>}
        </div>
        <div className="honour-sheet__pay">
          <h4>兌換</h4>
          {exchange.cost.map((cost) => (
            <div
              key={cost.itemId}
              className="honour-sheet__cost"
              data-testid={`pay-${cost.itemId}`}
            >
              <ContentImage category="items" assetKey={cost.itemId} alt="" />
              <span>{cost.name}</span>
              <small>
                持有 <em data-short={cost.owned < cost.quantity}>{formatNumber(cost.owned)}</em>
                ／需要 <em>{formatNumber(cost.quantity)}</em>
              </small>
            </div>
          ))}
        </div>
        {say !== null && (
          <div className="honour-sheet__say" data-testid="clerk-say" data-failed={say.failed}>
            <KeeperSay line={{ keeper: "lise", ...say }} />
          </div>
        )}
        <div className="honour-sheet__actions">
          <span className="honour-sheet__note">{blocked ?? ""}</span>
          <button
            type="button"
            className={`seal honour-sheet__seal${busy ? " honour-sheet__seal--busy" : ""}`}
            aria-busy={busy}
            disabled={busy || blocked !== null}
            onClick={onExchange}
          >
            <span>兌換</span>
          </button>
        </div>
      </section>
    </>
  )
}
