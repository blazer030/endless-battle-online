import gsap from "gsap"
import { BlurFilter, Color, Container, FillGradient, Graphics, Text, TextStyle } from "pixi.js"
import type { TextStyleOptions } from "pixi.js"
import {
  BeamSeconds,
  DamageNumberSeconds,
  DropFloatSeconds,
  DropFloatStaggerSeconds,
  HitShakeSeconds,
  ImpactBurstSeconds,
  LevelUpFlashSeconds,
  MissSeconds,
  PlateSeconds,
  RestFadeMaxSeconds,
  SlashSeconds,
  StandOffSeconds,
} from "../motion/timings"
import { Palette } from "./palette"

// 介面與數字用無襯線，怪物名牌用中文襯線，CRITICAL 用 Cinzel
export const Fonts = { ui: "Noto Sans TC", title: "Noto Serif TC", latin: "Cinzel" } as const

// 名牌文字可能是英文（LEVEL UP）或玩家取的中文必殺技名，拉丁字用 Cinzel、中文退到襯線
const PlateFontFamily = [Fonts.latin, Fonts.title]

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

// 漸層只建一次，所有斬擊共用
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

// 斬擊由左向右掃過舞台中段，尾端金、刃尖白
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

// 魔法：青色光束由玩家射向怪物，從左端伸展後淡去
export const playBeam = (layer: Container, stageWidth: number) => {
  const length = Math.max(120, stageWidth - 150)
  const beam = new Graphics()
    .rect(0, -6, length, 12)
    .fill({ color: Palette.cyan, alpha: 0.2 })
    .rect(0, -2.5, length, 5)
    .fill({ color: Palette.cyan, alpha: 0.85 })
    .rect(0, -0.75, length, 1.5)
    .fill(Palette.white)
  beam.filters = [new BlurFilter({ strength: 1 })]
  beam.position.set(70, 118)
  beam.scale.x = 0
  layer.addChild(beam)
  gsap.to(beam.scale, { x: 1, duration: BeamSeconds * 0.35, ease: "power2.out" })
  gsap.to(beam, {
    alpha: 0,
    delay: BeamSeconds * 0.45,
    duration: BeamSeconds * 0.55,
    ease: "power1.in",
    onComplete: () => dispose(beam),
  })
}

// 未命中：目標上方飄起淡色 MISS
export const playMiss = (layer: Container, x: number, y: number) => {
  const text = createStageText("MISS", {
    fontFamily: Fonts.latin,
    fontSize: 18,
    fontWeight: "700",
    fill: Palette.textMuted,
    letterSpacing: 2,
  })
  text.anchor.set(0.5)
  text.position.set(x, y + 6)
  text.alpha = 0
  layer.addChild(text)
  gsap.to(text, {
    keyframes: [
      { alpha: 1, y, duration: MissSeconds * 0.15 },
      { y: y - 18, duration: MissSeconds * 0.45 },
      { alpha: 0, y: y - 26, duration: MissSeconds * 0.4 },
    ],
    onComplete: () => dispose(text),
  })
}

export type PlateTone = "gold" | "muted" | "cyan"

const plateColors: Record<PlateTone, number> = {
  gold: Palette.gold,
  muted: Palette.textMuted,
  cyan: Palette.cyan,
}

// 名牌：黑底描邊的橫排字，放大落下、停留、淡出；暴擊、升級用金色，技能用青色，膠著、暈眩用淡色
export const playPlate = (
  layer: Container,
  x: number,
  y: number,
  label: string,
  tone: PlateTone,
  maxWidth: number,
) => {
  const color = plateColors[tone]
  const text = createStageText(label, {
    fontFamily: PlateFontFamily,
    fontSize: 22,
    fontWeight: "700",
    fill: color,
    letterSpacing: 3,
  })
  text.anchor.set(0.5)
  const plate = createPlate(text, 16, 6, color)
  const plateGroup = new Container()
  plateGroup.addChild(plate, text)
  // 玩家取的必殺技名可能很長，縮到舞台寬度內
  const restingScale = Math.min(1, maxWidth / plateGroup.width)
  plateGroup.position.set(x, y)
  plateGroup.rotation = degrees(-6)
  plateGroup.scale.set(2.2 * restingScale)
  plateGroup.alpha = 0
  layer.addChild(plateGroup)
  gsap.to(plateGroup.scale, {
    x: restingScale,
    y: restingScale,
    duration: PlateSeconds * 0.08,
    ease: "power2.in",
  })
  gsap.to(plateGroup, {
    keyframes: [
      { alpha: 0.95, duration: PlateSeconds * 0.08 },
      { alpha: 0.95, duration: PlateSeconds * 0.62 },
      { alpha: 0, duration: PlateSeconds * 0.3 },
    ],
    onComplete: () => dispose(plateGroup),
  })
}

export const playHitShake = (body: Container) => {
  gsap.killTweensOf(body, "x")
  body.x = 0
  const step = HitShakeSeconds / 4
  gsap.to(body, { keyframes: [6, -4, 3, 0].map((x) => ({ x, duration: step, ease: "none" })) })
}

// 超限收手：雙方各自後退一步再回到原位
export const playStandOff = (body: Container, direction: -1 | 1) => {
  gsap.killTweensOf(body, "x")
  body.x = 0
  gsap.to(body, {
    keyframes: [
      { x: direction * 14, duration: StandOffSeconds * 0.3, ease: "power2.out" },
      { x: direction * 14, duration: StandOffSeconds * 0.3 },
      { x: 0, duration: StandOffSeconds * 0.4, ease: "sine.inOut" },
    ],
  })
}

// 角色身上泛起光暈，擴散後淡去；升級用金色，施放技能用青色，補給用綠色
export const playAuraFlash = (layer: Container, x: number, y: number, color: number) => {
  const glow = new Graphics()
    .ellipse(0, 0, 58, 92)
    .fill({ color, alpha: 0.18 })
    .ellipse(0, 0, 36, 70)
    .fill({ color, alpha: 0.32 })
  glow.filters = [new BlurFilter({ strength: 8 })]
  glow.position.set(x, y)
  glow.scale.set(0.7)
  glow.alpha = 0
  layer.addChild(glow)
  gsap.to(glow.scale, { x: 1.15, y: 1.15, duration: LevelUpFlashSeconds, ease: "power1.out" })
  gsap.to(glow, {
    keyframes: [
      { alpha: 1, duration: LevelUpFlashSeconds * 0.15 },
      { alpha: 0.8, duration: LevelUpFlashSeconds * 0.35 },
      { alpha: 0, duration: LevelUpFlashSeconds * 0.5 },
    ],
    onComplete: () => dispose(glow),
  })
}

// 掉落：物品名從怪物處緩緩飄起後淡去；order 為同一批掉落中的順序
export const playDropFloat = (
  layer: Container,
  x: number,
  y: number,
  label: string,
  order: number,
) => {
  const text = createStageText(label, {
    fontSize: 15,
    fontWeight: "700",
    fill: Palette.gold,
    stroke: { color: Palette.night, width: 3 },
  })
  text.anchor.set(0.5)
  text.position.set(x, y)
  text.alpha = 0
  layer.addChild(text)
  gsap.to(text, {
    delay: order * DropFloatStaggerSeconds,
    keyframes: [
      { alpha: 1, y: y - 8, duration: DropFloatSeconds * 0.15 },
      { y: y - 34, duration: DropFloatSeconds * 0.55 },
      { alpha: 0, y: y - 46, duration: DropFloatSeconds * 0.3 },
    ],
    onComplete: () => dispose(text),
  })
}

// 住宿：整個舞台暗下再亮回來，時長取等待時間與上限的較小者
export const playFade = (scene: Container, waitSeconds: number) => {
  const duration = Math.min(waitSeconds, RestFadeMaxSeconds)
  gsap.killTweensOf(scene, "alpha")
  gsap.to(scene, {
    keyframes: [
      { alpha: 0.15, duration: duration * 0.4, ease: "power1.in" },
      { alpha: 0.15, duration: duration * 0.2 },
      { alpha: 1, duration: duration * 0.4, ease: "power1.out" },
    ],
  })
}

// 怪物倒下：淡色光霧擴散後淡去
export const playImpactBurst = (layer: Container, x: number, y: number) => {
  const burst = new Graphics()
    .circle(0, 0, 65)
    .fill({ color: Palette.text, alpha: 0.06 })
    .circle(0, 0, 45)
    .fill({ color: Palette.text, alpha: 0.12 })
    .circle(0, 0, 26)
    .fill({ color: Palette.gold, alpha: 0.25 })
  burst.filters = [new BlurFilter({ strength: 2 })]
  burst.position.set(x, y)
  burst.scale.set(0.2)
  burst.alpha = 0
  layer.addChild(burst)
  gsap.to(burst.scale, {
    keyframes: [
      { x: 1, y: 1, duration: ImpactBurstSeconds * 0.1 },
      { x: 1.3, y: 1.3, duration: ImpactBurstSeconds * 0.9 },
    ],
  })
  gsap.to(burst, {
    keyframes: [
      { alpha: 1, duration: ImpactBurstSeconds * 0.1 },
      { alpha: 0.4, duration: ImpactBurstSeconds * 0.5 },
      { alpha: 0, duration: ImpactBurstSeconds * 0.4 },
    ],
    onComplete: () => dispose(burst),
  })
}

// 名牌底板：以文字為中心的黑底描邊圓角框
export const createPlate = (
  text: Text,
  paddingX: number,
  paddingY: number,
  borderColor: number = Palette.gold,
) => {
  const width = text.width + paddingX * 2
  const height = text.height + paddingY * 2
  return new Graphics()
    .roundRect(-width / 2, -height / 2, width, height, 6)
    .fill({ color: Palette.night, alpha: 0.9 })
    .stroke({ width: 1, color: borderColor })
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
