import type { BonusRow } from "../../game/town"
import { ContentImage } from "../ContentImage"

// 圓章裡的圖：住宿是房型徽章，密藥是道具圖
export const BonusBadgeImage = ({ row }: { row: BonusRow }) =>
  row.isLodging ? (
    <ContentImage category="room-badges" assetKey={row.roomId ?? "unknown"} alt="" />
  ) : (
    <ContentImage category="items" assetKey={row.itemId ?? "unknown"} alt="" />
  )
