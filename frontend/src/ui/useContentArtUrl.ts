import { useEffect, useState } from "react"
import { contentArtCandidates, loadFirst, type ContentCategory } from "../render/contentAssets"

const loadImage = (url: string) =>
  new Promise<string>((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(url)
    image.onerror = () => reject(new Error(`讀不到 ${url}`))
    image.src = url
  })

// 給 CSS 背景、遮罩用的內容圖網址：先用 R2 的圖，確認讀不到就換成該類佔位圖（<img> 用 ContentImage）
export const useContentArtUrl = (category: ContentCategory, key: string): string => {
  const candidates = contentArtCandidates(category, key)
  const first = candidates[0] ?? ""
  const [loaded, setLoaded] = useState<{ first: string; url: string } | null>(null)

  useEffect(() => {
    let isCurrent = true
    void loadFirst(contentArtCandidates(category, key), loadImage).then((url) => {
      if (isCurrent && url !== null) setLoaded({ first, url })
    })
    return () => {
      isCurrent = false
    }
  }, [category, key, first])

  return loaded !== null && loaded.first === first ? loaded.url : first
}
