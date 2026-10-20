import gsap from "gsap"
import { Application, Assets, Container, Graphics, Sprite } from "pixi.js"
import type { Text, Texture } from "pixi.js"
import { initialStageState } from "../game/stageModel"
import type { StageEffect, StageState } from "../game/stageModel"
import { BreathingCycleSeconds } from "../motion/timings"
import { resolveArt } from "./artIndex"
import {
  createInkText,
  FontFamily,
  playDamageNumber,
  playHitShake,
  playInkSplash,
  playSeal,
  playSlash,
} from "./effects"
import { Palette } from "./palette"
import { StageHeight } from "./stageLayout"

export type StageHandle = Pick<StageRenderer, "showEncounter" | "updateHealth" | "play">

const Gutter = 14
const BarWidth = 96
const BarHeight = 5
const PlayerBox = { left: 10, width: 100, height: 170 }
const MonsterBox = { right: 4, width: 130, height: 180 }
const MonsterNameTop = 44
const MonsterNameRight = 14
const MonsterNameSize = 52
const PlayerLabel = "你"

type Box = { width: number; height: number }
// slot 由版面定位在立繪底部中心；body 承載呼吸與受擊抖動
type Figure = { slot: Container; body: Container }
type HealthBar = { root: Container; label: Text; meter: Graphics; color: number }

const fontReady = (text: string): Promise<unknown> =>
  Promise.all([
    document.fonts.load(`400 16px "${FontFamily}"`, text),
    document.fonts.load(`700 16px "${FontFamily}"`, text),
  ]).catch(() => undefined)

export class StageRenderer {
  private readonly app: Application
  private readonly mountains = new Graphics()
  private readonly player = createFigure()
  private readonly monster = createFigure()
  private readonly playerBar = createHealthBar(PlayerLabel, Palette.ink, "left")
  private readonly monsterBar = createHealthBar("", Palette.vermilion, "right")
  private readonly effects = new Container()
  private readonly onResize = () => this.layout()
  private state: StageState = initialStageState
  private monsterToken = 0
  private destroyed = false

  private constructor(app: Application) {
    this.app = app
    app.stage.addChild(
      this.mountains,
      this.player.slot,
      this.monster.slot,
      this.playerBar.root,
      this.monsterBar.root,
      this.effects,
    )
    drawPlayerSilhouette(this.player.body)
    for (const figure of [this.player, this.monster]) breathe(figure.body)
    app.renderer.on("resize", this.onResize)
    this.layout()

    const playerArt = resolveArt("player")
    if (playerArt !== null) void this.showArt(playerArt, this.player, PlayerBox, () => true)
  }

  static async create(container: HTMLElement): Promise<StageRenderer> {
    const app = new Application()
    await Promise.all([
      app.init({
        resizeTo: container,
        height: StageHeight,
        backgroundAlpha: 0,
        antialias: true,
        resolution: window.devicePixelRatio,
        autoDensity: true,
      }),
      fontReady(`${PlayerLabel}會心0123456789,`),
    ])
    container.appendChild(app.canvas)
    return new StageRenderer(app)
  }

  showEncounter(monsterId: string, monsterName: string, state: StageState): void {
    const token = ++this.monsterToken
    const isCurrent = () => !this.destroyed && token === this.monsterToken
    clearBody(this.monster.body)
    this.monsterBar.label.text = ""
    this.updateHealth(state)

    const art = resolveArt(monsterId)
    if (art !== null) void this.showArt(art, this.monster, MonsterBox, isCurrent)
    void fontReady(monsterName).then(() => {
      if (!isCurrent()) return
      this.monsterBar.label.text = monsterName
      if (this.monster.body.children.length === 0) this.drawMonsterName(monsterName)
    })
  }

  updateHealth(state: StageState): void {
    this.state = state
    drawMeter(this.playerBar, state.playerHealth, state.playerMaxHealth)
    drawMeter(this.monsterBar, state.monsterHealth, state.monsterMaxHealth)
  }

  play(effects: StageEffect[]): void {
    const width = this.app.screen.width
    const monsterX = this.monster.slot.x
    // 傷害數字落在目標內側，避開妖怪名字與玩家的劍
    const damageX = { player: this.player.slot.x + 70, monster: monsterX - 80 }
    for (const effect of effects) {
      switch (effect.type) {
        case "slash":
          playSlash(this.effects, width)
          break
        case "damageNumber":
          playDamageNumber(this.effects, damageX[effect.side], 70, effect.value, effect.critical)
          break
        case "seal":
          playSeal(this.effects, width * 0.48, 82, effect.text)
          break
        case "hitShake":
          playHitShake(this[effect.side].body)
          break
        case "inkSplash":
          playInkSplash(this.effects, monsterX - 16, StageHeight - 95)
          break
      }
    }
  }

  destroy(): void {
    if (this.destroyed) return
    this.destroyed = true
    const animated = [this.player.body, this.monster.body, ...this.effects.children]
    gsap.killTweensOf(animated.flatMap((target) => [target, target.scale]))
    this.app.renderer.off("resize", this.onResize)
    this.app.destroy({ removeView: true }, { children: true })
  }

  private layout() {
    const width = this.app.screen.width
    drawMountains(this.mountains, width)
    this.player.slot.position.set(PlayerBox.left + PlayerBox.width / 2, StageHeight)
    this.monster.slot.position.set(width - MonsterBox.right - MonsterBox.width / 2, StageHeight)
    this.playerBar.root.position.set(Gutter, 10)
    this.monsterBar.root.position.set(width - Gutter - BarWidth, 10)
    this.updateHealth(this.state)
  }

  private drawMonsterName(name: string) {
    const text = createInkText([...name].join("\n"), {
      fontSize: MonsterNameSize,
      fontWeight: "700",
      lineHeight: MonsterNameSize + 8,
      align: "center",
    })
    text.alpha = 0.88
    text.anchor.set(0.5, 0)
    const available = StageHeight - MonsterNameTop - 8
    if (text.height > available) text.scale.set(available / text.height)
    const nameCenter = this.app.screen.width - MonsterNameRight - MonsterNameSize / 2
    text.position.set(nameCenter - this.monster.slot.x, MonsterNameTop - StageHeight)
    this.monster.body.addChild(text)
  }

  private async showArt(url: string, figure: Figure, box: Box, isCurrent: () => boolean) {
    let texture: Texture
    try {
      texture = await Assets.load<Texture>(url)
    } catch {
      return
    }
    if (this.destroyed || !isCurrent()) return
    const sprite = new Sprite(texture)
    sprite.anchor.set(0.5, 1)
    sprite.scale.set(Math.min(box.width / texture.width, box.height / texture.height))
    clearBody(figure.body)
    figure.body.addChild(sprite)
  }
}

const createFigure = (): Figure => {
  const slot = new Container()
  const body = new Container()
  slot.addChild(body)
  return { slot, body }
}

const clearBody = (body: Container) => {
  for (const child of body.removeChildren()) child.destroy()
}

const breathe = (body: Container) =>
  gsap.to(body.scale, {
    y: 1.015,
    duration: BreathingCycleSeconds / 2,
    ease: "sine.inOut",
    yoyo: true,
    repeat: -1,
  })

const createHealthBar = (label: string, color: number, align: "left" | "right"): HealthBar => {
  const root = new Container()
  const text = createInkText(label, { fontSize: 11 })
  if (align === "right") {
    text.anchor.set(1, 0)
    text.x = BarWidth
  }
  const meter = new Graphics()
  meter.y = 16
  root.addChild(text, meter)
  return { root, label: text, meter, color }
}

const drawMeter = (bar: HealthBar, health: number, maxHealth: number) => {
  const ratio = maxHealth > 0 ? Math.min(Math.max(health / maxHealth, 0), 1) : 0
  bar.meter
    .clear()
    .roundRect(0, 0, BarWidth, BarHeight, 3)
    .fill({ color: Palette.ink, alpha: 0.15 })
  if (ratio > 0) bar.meter.roundRect(0, 0, BarWidth * ratio, BarHeight, 3).fill(bar.color)
}

// 對照 battle-v2.html 的 viewBox 0 0 300 80 遠山
const drawMountains = (graphics: Graphics, width: number) => {
  const scaleX = width / 300
  const top = StageHeight - 80
  const point = (x: number, y: number): [number, number] => [x * scaleX, top + y]
  graphics
    .clear()
    .moveTo(...point(0, 80))
    .lineTo(...point(0, 50))
    .quadraticCurveTo(...point(40, 10), ...point(80, 40))
    .quadraticCurveTo(...point(120, 0), ...point(170, 35))
    .quadraticCurveTo(...point(220, 5), ...point(260, 30))
    .quadraticCurveTo(...point(285, 20), ...point(300, 35))
    .lineTo(...point(300, 80))
    .closePath()
    .fill({ color: Palette.ink, alpha: 0.18 })
}

// 對照 battle-v2.html 的玩家剪影（viewBox 110×200，底部中心為原點）
const drawPlayerSilhouette = (body: Container) => {
  const scale = PlayerBox.height / 200
  const graphics = new Graphics()
    .circle(0, -165, 15)
    .fill(Palette.ink)
    .moveTo(-35, 0)
    .quadraticCurveTo(-29, -110, 0, -140)
    .quadraticCurveTo(29, -110, 37, 0)
    .closePath()
    .fill({ color: Palette.ink, alpha: 0.82 })
    .moveTo(15, -130)
    .lineTo(53, -192)
    .stroke({ width: 3, color: Palette.ink })
  graphics.scale.set(scale)
  body.addChild(graphics)
}
