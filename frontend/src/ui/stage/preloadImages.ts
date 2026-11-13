// 預先下載並解碼圖片，第一次打開畫面時圖不會一張張冒出來；同一張只載一次，失敗就算了（畫面照常用原本的佔位）
const loaded = new Map<string, Promise<void>>()

const load = (url: string) =>
  new Promise<void>((resolve) => {
    const image = new Image()
    image.onload = () => {
      image.decode().then(resolve, resolve)
    }
    image.onerror = () => resolve()
    image.src = url
  })

export const preloadImages = (urls: readonly string[]): Promise<void> =>
  Promise.all(
    urls.map((url) => {
      const existing = loaded.get(url)
      if (existing !== undefined) return existing
      const loading = load(url)
      loaded.set(url, loading)
      return loading
    }),
  ).then(() => undefined)
