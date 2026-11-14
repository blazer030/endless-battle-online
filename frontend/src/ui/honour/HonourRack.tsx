import type { CSSProperties } from "react"
import { quantityBadge, rackSections, type HonourMedal, type HonourShopVo } from "../../game/honour"
import { ContentImage } from "../ContentImage"
import { RewardPicture } from "./RewardPicture"

type Props = {
  shop: HonourShopVo
  medal: HonourMedal
  onPick: (exchangeId: string) => void
}

// 吊牌輪流傾斜（照設計稿）
const tilts = [-3, 2, -1.5, 3, -2, 1.5, -2.5, 2.5]

const shortName = (name: string) => name.replace(/勳章$/, "")

// 陳列板：黃銅銘牌寫目前的勳章，吊牌依分區掛在綠絨布上，板內捲動
export const HonourRack = ({ shop, medal, onPick }: Props) => {
  let tagIndex = 0
  return (
    <section className="honour-rack" aria-label={`${medal.name}陳列板`}>
      <div className="honour-rack__felt" aria-hidden="true" />
      <h3 className="honour-rack__plate">
        <ContentImage category="items" assetKey={medal.itemId} alt="" />
        {shortName(medal.name)}
      </h3>
      <div className="honour-rack__list">
        {rackSections(shop.exchanges, medal.itemId).map((section) => (
          <div key={section.title}>
            <h4 className="honour-rack__section">{section.title}</h4>
            <div className="honour-rack__tags">
              {section.exchanges.map((exchange) => {
                const badge = quantityBadge(exchange.quantity)
                const dimmed =
                  !exchange.affordable ||
                  (exchange.kind === "mercenary" && shop.hiredMercenaryName !== null)
                const tilt = tilts[tagIndex++ % tilts.length]
                return (
                  <button
                    key={exchange.id}
                    type="button"
                    className={`honour-tag${dimmed ? " honour-tag--dimmed" : ""}`}
                    style={{ "--tilt": `${tilt}deg` } as CSSProperties}
                    aria-label={
                      badge === null ? exchange.rewardName : `${exchange.rewardName} ${badge}`
                    }
                    data-dimmed={dimmed}
                    onClick={() => onPick(exchange.id)}
                  >
                    <span className="honour-tag__picture">
                      <RewardPicture exchange={exchange} className="honour-tag__image" />
                      {badge !== null && (
                        <span className="honour-tag__quantity" aria-hidden="true">
                          {badge}
                        </span>
                      )}
                    </span>
                    <b className="honour-tag__name">{exchange.rewardName}</b>
                    <span
                      className={`honour-tag__cost${exchange.cost.length > 2 ? " honour-tag__cost--many" : ""}`}
                    >
                      {exchange.cost.map((cost) => (
                        <span key={cost.itemId}>
                          <ContentImage category="items" assetKey={cost.itemId} alt={cost.name} />
                          <em
                            className={cost.owned < cost.quantity ? "honour-tag__short" : undefined}
                          >
                            {`×${cost.quantity}`}
                          </em>
                        </span>
                      ))}
                    </span>
                  </button>
                )
              })}
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
