import { useState } from "react"
import { contentArtCandidates, type ContentCategory } from "../render/contentAssets"

type Props = {
  category: ContentCategory
  assetKey: string
  alt: string
  className?: string
}

// 內容圖：R2 的圖載入失敗時換成該類佔位圖
export const ContentImage = ({ category, assetKey, alt, className }: Props) => {
  const candidates = contentArtCandidates(category, assetKey)
  const [failed, setFailed] = useState({ key: `${category}/${assetKey}`, count: 0 })
  const current = `${category}/${assetKey}`
  const index = failed.key === current ? failed.count : 0
  const src = candidates[Math.min(index, candidates.length - 1)]
  return (
    <img
      className={className}
      src={src}
      alt={alt}
      data-asset={current}
      onError={() => setFailed({ key: current, count: index + 1 })}
    />
  )
}
