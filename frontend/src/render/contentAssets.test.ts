import { describe, expect, it } from "vitest"
import manifestJson from "../assets/content-manifest.json"
import {
  artCandidates,
  ContentCategories,
  createContentAssets,
  loadFirst,
  validateManifest,
  type ContentCategory,
  type ContentManifest,
} from "./contentAssets"

const placeholders = Object.fromEntries(
  ContentCategories.map((category) => [category, `/placeholders/${category}.svg`]),
) as Record<ContentCategory, string>

const manifest = {
  baseUrl: "https://ebo-assets.blazer030.dev",
  assets: {
    "monsters/monster-1": {
      file: "monsters/monster-1.3fa2c1d04e5b.webp",
      original: "monsters/monster-1.9a8b7c6d5e4f.png",
    },
  },
}

describe("contentAsset", () => {
  it("清單有 monsters/monster-1 時回傳完整網址且不是佔位圖", () => {
    const contentAsset = createContentAssets(manifest, placeholders)

    expect(contentAsset("monsters", "monster-1")).toEqual({
      url: "https://ebo-assets.blazer030.dev/monsters/monster-1.3fa2c1d04e5b.webp",
      placeholder: false,
    })
  })

  it("沒有 items/item-9999 時回傳道具佔位圖並標記為佔位圖", () => {
    const contentAsset = createContentAssets(manifest, placeholders)

    expect(contentAsset("items", "item-9999")).toEqual({
      url: "/placeholders/items.svg",
      placeholder: true,
    })
  })

  it("鍵相同但類別不同時不會拿錯圖", () => {
    const contentAsset = createContentAssets(manifest, placeholders)

    expect(contentAsset("items", "monster-1").placeholder).toBe(true)
  })
})

describe("validateManifest", () => {
  it("格式正確時沒有錯誤", () => {
    expect(validateManifest(manifest)).toEqual([])
  })

  it("類別不合法時列出錯誤", () => {
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "weapons/sword": { file: "weapons/sword.3fa2c1d04e5b.webp", original: "o" } },
    })

    expect(errors.join("")).toContain("weapons/sword")
  })

  it("file 跟鍵對不上時列出錯誤", () => {
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "items/item-1": { file: "items/item-2.3fa2c1d04e5b.webp", original: "o" } },
    })

    expect(errors.join("")).toContain("items/item-1")
  })

  it("雜湊不是 12 位十六進位時列出錯誤", () => {
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "items/item-1": { file: "items/item-1.abc.webp", original: "o" } },
    })

    expect(errors).toHaveLength(1)
  })

  it("鍵含小寫英數與連字號以外的字元時列出錯誤", () => {
    // 「.」若被當成正規表示式的任意字元，itemx1 會被誤判成符合 item.1
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "items/item.1": { file: "items/itemx1.3fa2c1d04e5b.webp", original: "o" } },
    })

    expect(errors.join("")).toContain("items/item.1")
  })

  it("鍵裡有斜線時列出錯誤", () => {
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "items/a/b": { file: "items/a/b.3fa2c1d04e5b.webp", original: "o" } },
    })

    expect(errors.join("")).toContain("items/a/b")
  })

  it("缺 original 時列出錯誤", () => {
    const errors = validateManifest({
      baseUrl: "https://x",
      assets: { "items/item-1": { file: "items/item-1.3fa2c1d04e5b.webp" } },
    } as unknown as ContentManifest)

    expect(errors.join("")).toContain("original")
  })

  it("baseUrl 不是 https 時列出錯誤", () => {
    expect(validateManifest({ baseUrl: "http://x", assets: {} })).toHaveLength(1)
  })

  it("baseUrl 結尾有斜線時列出錯誤", () => {
    expect(validateManifest({ baseUrl: "https://x/", assets: {} })).toHaveLength(1)
  })

  it("正式清單格式正確", () => {
    expect(validateManifest(manifestJson)).toEqual([])
  })
})

describe("artCandidates", () => {
  it("有圖時先試 R2 網址、載入失敗再退回佔位圖", () => {
    const asset = { url: "https://x/monsters/monster-1.3fa2c1d04e5b.webp", placeholder: false }

    expect(artCandidates(asset, "/placeholders/monsters.svg")).toEqual([
      "https://x/monsters/monster-1.3fa2c1d04e5b.webp",
      "/placeholders/monsters.svg",
    ])
  })

  it("缺圖時只用佔位圖", () => {
    const asset = { url: "/placeholders/monsters.svg", placeholder: true }

    expect(artCandidates(asset, "/placeholders/monsters.svg")).toEqual([
      "/placeholders/monsters.svg",
    ])
  })
})

describe("loadFirst", () => {
  const loader = (failing: readonly string[]) => {
    const tried: string[] = []
    const load = async (url: string) => {
      tried.push(url)
      if (failing.includes(url)) throw new Error(`載入失敗：${url}`)
      return `texture:${url}`
    }
    return { tried, load }
  }

  it("R2 的圖載入失敗時改用佔位圖", async () => {
    const { tried, load } = loader(["https://x/monsters/monster-1.3fa2c1d04e5b.webp"])

    const result = await loadFirst(
      ["https://x/monsters/monster-1.3fa2c1d04e5b.webp", "/placeholders/monsters.svg"],
      load,
    )

    expect(result).toBe("texture:/placeholders/monsters.svg")
    expect(tried).toHaveLength(2)
  })

  it("第一張成功就不再載入後面的", async () => {
    const { tried, load } = loader([])

    expect(await loadFirst(["a", "b"], load)).toBe("texture:a")
    expect(tried).toEqual(["a"])
  })

  it("全部失敗時回傳 null", async () => {
    const { load } = loader(["a", "b"])

    expect(await loadFirst(["a", "b"], load)).toBeNull()
  })
})
