import { describe, expect, it } from "vitest"
import { Palette } from "../render/palette"
import themeCss from "./theme.css?raw"

const colorTokens = {
  "--bg-top": "#121826",
  "--bg-bottom": "#1b2232",
  "--panel": "#222b3d",
  "--border": "#2e3950",
  "--text": "#e8ecf4",
  "--text-muted": "#9aa5bd",
  "--gold": "#e0a63a",
  "--cyan": "#4cc3d9",
  "--crimson": "#e2554f",
  "--crimson-text": "#f0706a",
  "--green": "#4fc47a",
  "--night": "#0b1020",
}

// 新畫面（標題、建角起）的紙、墨、木頭色；只給 CSS 用，Pixi 沒有對應
const storybookTokens = {
  "--paper": "#f6ebd5",
  "--paper-light": "#f3e3c0",
  "--ink": "#2a1a0c",
  "--umber-deep": "#3a2410",
  "--umber": "#4a2e14",
  "--umber-mid": "#5a3a1c",
  "--umber-faded": "#8a6a40",
  "--wood": "#6a4a2c",
  "--wood-deep": "#6a4626",
  "--seal-red": "#8a2a1a",
  "--error-red": "#a8321e",
  "--brass": "#c9a464",
  "--amber": "#b07a2a",
}

const declaredColors = (css: string) =>
  new Map(
    [...css.matchAll(/(--[a-z-]+):\s*(#[0-9a-f]{6})\s*;/gi)].map(([, name, hex]) => [
      name,
      hex.toLowerCase(),
    ]),
  )

const cssName = (key: string) =>
  `--${key.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`)}`

const hexOf = (value: number) => `#${value.toString(16).padStart(6, "0")}`

describe("theme", () => {
  it("theme_DropsThePaperAndInkPalette", () => {
    expect(themeCss).toContain(":root")
    for (const hex of ["#f3ead8", "#e6d9bd", "#2b2620", "#b8322a", "#a87a12", "#d4a017"]) {
      expect(themeCss.toLowerCase()).not.toContain(hex)
    }
  })

  it("theme_DeclaresModernFantasyTokens", () => {
    const colors = declaredColors(themeCss)
    for (const [name, hex] of Object.entries(colorTokens)) {
      expect(colors.get(name), name).toBe(hex)
    }
    expect(themeCss).toMatch(/--font-ui:\s*[^;]*"Noto Sans TC"/)
    expect(themeCss).toMatch(/--font-title:\s*[^;]*"Cinzel"[^;]*"Noto Serif TC"/)
  })

  it("theme_DeclaresStorybookTokens", () => {
    const colors = declaredColors(themeCss)
    for (const [name, hex] of Object.entries(storybookTokens)) {
      expect(colors.get(name), name).toBe(hex)
    }
    // 陰影與壓暗用的 RGB 三元組，寫成 rgb(var(--shade-rgb) / .55)
    expect(themeCss).toMatch(/--shade-rgb:\s*20 10 4;/)
    expect(themeCss).toMatch(/--umber-rgb:\s*90 60 30;/)
    expect(themeCss).toMatch(/--glow-rgb:\s*255 244 222;/)
    // 舞台外共用的背景只定義在這裡
    expect(themeCss).toMatch(/--stage-backdrop:\s*var\(--ink\);/)
    expect(themeCss).toMatch(/--font-serif:\s*"Noto Serif TC"/)
    expect(themeCss).toMatch(/--font-number:\s*"Cinzel"/)
    expect(themeCss).toMatch(/--font-script:\s*"Great Vibes"/)
  })

  // 新畫面的變數不給 Pixi 用，所以只檢查 Palette 有的鍵
  it("palette_MatchesTheCssColorsItMirrors", () => {
    const colors = declaredColors(themeCss)
    for (const [key, value] of Object.entries(Palette)) {
      expect(colors.get(cssName(key)), key).toBe(hexOf(value))
    }
  })
})
