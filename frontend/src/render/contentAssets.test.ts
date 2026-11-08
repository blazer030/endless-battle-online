import { describe, expect, it } from "vitest"
import manifestJson from "../assets/content-manifest.json"
import {
  ContentCategories,
  createContentAssets,
  validateManifest,
  type ContentCategory,
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
