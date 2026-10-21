import gsap from "gsap"
import { BlurFilter, Color, Container, FillGradient, Graphics, Text, TextStyle } from "pixi.js"
import type { TextStyleOptions } from "pixi.js"
import {
  DamageNumberSeconds,
  HitShakeSeconds,
  InkSplashSeconds,
  SealSeconds,
  SlashSeconds,
} from "../motion/timings"
import { Palette } from "./palette"

// 介面與數字用無襯線，妖怪名牌用中文襯線，CRITICAL 用 Cinzel
export const Fonts = { ui: "Noto Sans TC", title: "Noto Serif TC", latin: "Cinzel" } as const

const degrees = (value: number) => (value * Math.PI) / 180

const dispose = (target: Container) => {
  gsap.killTweensOf([target, target.scale])
  target.destroy({ children: true })
}

export const createStageText = (text: string, style: TextStyleOptions) =>
  new Text({
    text,
    style: new TextStyle({ fontFamily: Fonts.ui, fill: Palette.text, ...style }),
    resolution: window.devicePixelRatio,
  })

// 漸層只建一次，所有劍氣共用
let bladeGradient: FillGradient | null = null
const getBladeGradient = () =>
  (bladeGradient ??= new FillGradient({
    start: { x: 0, y: 0 },
    end: { x: 1, y: 0 },
    colorStops: [
      { offset: 0, color: Palette.gold },
      { offset: 1, color: Palette.white },
    ],
  }))

// 劍氣由左向右掃過舞台中段，尾端金、刃尖白
export const playSlash = (layer: Container, stageWidth: number) => {
  const length = Math.max(120, stageWidth - 110)
  const blade = new Graphics()
    .rect(-length / 2, -5, length, 10)
    .fill({ color: Palette.gold, alpha: 0.25 })
    .rect(-length / 2, -1.5, length, 3)
    .fill(getBladeGradient())
    .rect(length / 6, -0.75, length / 3, 1.5)
    .fill(Palette.white)
  blade.position.set(10 + length / 2, 130)
  blade.rotation = degrees(-18)
  blade.alpha = 0
  layer.addChild(blade)
  gsap.to(blade, {
    keyframes: [
      { alpha: 1, x: "+=30", duration: SlashSeconds * 0.3, ease: "none" },
      { alpha: 0, x: "+=70", duration: SlashSeconds * 0.7, ease: "power1.in" },
    ],
    onComplete: () => dispose(blade),
  })
}

export const playDamageNumber = (
  layer: Container,
  x: number,
  y: number,
  value: number,
  critical: boolean,
) => {
  const text = createStageText(value.toLocaleString("zh-TW"), {
    fontSize: critical ? 34 : 22,
    fontWeight: "700",
    fill: Palette.crimson,
    ...(critical ? { stroke: { color: Palette.gold, width: 3 } } : {}),
  })
  text.anchor.set(0.5)
  text.position.set(x, y + 8)
  text.alpha = 0
  layer.addChild(text)
  gsap.to(text, {
    keyframes: [
      { alpha: 1, y, duration: DamageNumberSeconds * 0.1 },
      { y: y - 30, duration: DamageNumberSeconds * 0.45 },
      { alpha: 0, y: y - 40, duration: DamageNumberSeconds * 0.45 },
    ],
    onComplete: () => dispose(text),
  })
}

// 暴擊名牌：黑底金邊的橫排字，放大落下、停留、淡出
export const playSeal = (layer: Container, x: number, y: number, label: string) => {
  const text = createStageText(label, {
    fontFamily: Fonts.latin,
    fontSize: 22,
    fontWeight: "700",
    fill: Palette.gold,
    letterSpacing: 3,
  })
  text.anchor.set(0.5)
  const plate = createPlate(text, 16, 6)
  const seal = new Container()
  seal.addChild(plate, text)
  seal.position.set(x, y)
  seal.rotation = degrees(-6)
  seal.scale.set(2.2)
  seal.alpha = 0
  layer.addChild(seal)
  gsap.to(seal.scale, { x: 1, y: 1, duration: SealSeconds * 0.08, ease: "power2.in" })
  gsap.to(seal, {
    keyframes: [
      { alpha: 0.95, duration: SealSeconds * 0.08 },
      { alpha: 0.95, duration: SealSeconds * 0.62 },
      { alpha: 0, duration: SealSeconds * 0.3 },
    ],
    onComplete: () => dispose(seal),
  })
}

export const playHitShake = (body: Container) => {
  gsap.killTweensOf(body, "x")
  body.x = 0
  const step = HitShakeSeconds / 4
  gsap.to(body, { keyframes: [6, -4, 3, 0].map((x) => ({ x, duration: step, ease: "none" })) })
}

// 妖怪倒下：淡色光霧擴散後淡去
export const playInkSplash = (layer: Container, x: number, y: number) => {
  const splash = new Graphics()
    .circle(0, 0, 65)
    .fill({ color: Palette.text, alpha: 0.06 })
    .circle(0, 0, 45)
    .fill({ color: Palette.text, alpha: 0.12 })
    .circle(0, 0, 26)
    .fill({ color: Palette.gold, alpha: 0.25 })
  splash.filters = [new BlurFilter({ strength: 2 })]
  splash.position.set(x, y)
  splash.scale.set(0.2)
  splash.alpha = 0
  layer.addChild(splash)
  gsap.to(splash.scale, {
    keyframes: [
      { x: 1, y: 1, duration: InkSplashSeconds * 0.1 },
      { x: 1.3, y: 1.3, duration: InkSplashSeconds * 0.9 },
    ],
  })
  gsap.to(splash, {
    keyframes: [
      { alpha: 1, duration: InkSplashSeconds * 0.1 },
      { alpha: 0.4, duration: InkSplashSeconds * 0.5 },
      { alpha: 0, duration: InkSplashSeconds * 0.4 },
    ],
    onComplete: () => dispose(splash),
  })
}

// 名牌底板：以文字為中心的黑底金邊圓角框
export const createPlate = (text: Text, paddingX: number, paddingY: number) => {
  const width = text.width + paddingX * 2
  const height = text.height + paddingY * 2
  return new Graphics()
    .roundRect(-width / 2, -height / 2, width, height, 6)
    .fill({ color: Palette.night, alpha: 0.9 })
    .stroke({ width: 1, color: Palette.gold })
}

// 底部霧氣：由透明漸到白色 6%
let fogGradient: FillGradient | null = null
export const getFogGradient = () =>
  (fogGradient ??= new FillGradient({
    start: { x: 0, y: 0 },
    end: { x: 0, y: 1 },
    colorStops: [
      { offset: 0, color: new Color(Palette.white).setAlpha(0) },
      { offset: 1, color: new Color(Palette.white).setAlpha(0.06) },
    ],
  }))
