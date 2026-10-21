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
  "--green": "#4fc47a",
  "--night": "#0b1020",
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

  it("palette_MirrorsEveryCssColorToken", () => {
    const fromPalette = new Map(
      Object.entries(Palette).map(([key, value]) => [cssName(key), hexOf(value)]),
    )
    expect(fromPalette).toEqual(declaredColors(themeCss))
  })
})
