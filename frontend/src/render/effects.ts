import gsap from "gsap"
import { BlurFilter, Container, Graphics, Text, TextStyle } from "pixi.js"
import type { TextStyleOptions } from "pixi.js"
import {
  DamageNumberSeconds,
  HitShakeSeconds,
  InkSplashSeconds,
  SealSeconds,
  SlashSeconds,
} from "../motion/timings"
import { Palette } from "./palette"

export const FontFamily = "LXGW WenKai TC"

const degrees = (value: number) => (value * Math.PI) / 180

const dispose = (target: Container) => {
  gsap.killTweensOf([target, target.scale])
  target.destroy({ children: true })
}

export const createInkText = (text: string, style: TextStyleOptions) =>
  new Text({
    text,
    style: new TextStyle({ fontFamily: FontFamily, fill: Palette.ink, ...style }),
    resolution: window.devicePixelRatio,
  })

// 金色劍光由左向右掃過舞台中段
export const playSlash = (layer: Container, stageWidth: number) => {
  const length = Math.max(120, stageWidth - 110)
  const blade = new Graphics()
    .rect(-length / 2, -4.5, length, 9)
    .fill({ color: Palette.swordGold, alpha: 0.25 })
    .rect(-length / 2, -1.5, length, 3)
    .fill(Palette.swordGold)
    .rect(-length / 6, -0.75, length / 3, 1.5)
    .fill(Palette.paperLight)
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
  const text = createInkText(value.toLocaleString("zh-TW"), {
    fontSize: critical ? 32 : 22,
    fontWeight: "700",
    fill: Palette.vermilion,
    ...(critical ? { stroke: { color: Palette.paperLight, width: 3 } } : {}),
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

// 朱紅印章：放大蓋下、停留、淡出
export const playSeal = (layer: Container, x: number, y: number, label: string) => {
  const text = createInkText(label, { fontSize: 28, fontWeight: "700", fill: Palette.vermilion })
  text.anchor.set(0.5)
  const width = text.width + 16
  const height = text.height + 4
  const border = new Graphics()
    .roundRect(-width / 2, -height / 2, width, height, 6)
    .stroke({ width: 3, color: Palette.vermilion })
  const seal = new Container()
  seal.addChild(border, text)
  seal.position.set(x, y)
  seal.rotation = degrees(-12)
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

// 潑墨：墨團擴散後淡去
export const playInkSplash = (layer: Container, x: number, y: number) => {
  const splash = new Graphics()
    .circle(0, 0, 65)
    .fill({ color: Palette.ink, alpha: 0.08 })
    .circle(0, 0, 45)
    .fill({ color: Palette.ink, alpha: 0.15 })
    .circle(0, 0, 26)
    .fill({ color: Palette.ink, alpha: 0.3 })
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
