import type { HonourExchange } from "../../game/honour"
import { ContentImage } from "../ContentImage"
import { categoryPlaceholders } from "../itemPlaceholders"
import mercenaryHelmet from "../../assets/honour/mercenary-helmet.webp"
// 獎品圖：道具用內容圖（缺圖時分類佔位圖），傭兵用紅羽頭盔
export const RewardPicture = ({
  exchange,
  className,
}: {
  exchange: HonourExchange
  className: string
}) =>
  exchange.item === null ? (
    <img className={`${className} ${className}--mercenary`} src={mercenaryHelmet} alt="" />
  ) : (
    <ContentImage
      className={className}
      category="items"
      assetKey={exchange.rewardId}
      alt=""
      placeholder={categoryPlaceholders[exchange.item.category]}
    />
  )
