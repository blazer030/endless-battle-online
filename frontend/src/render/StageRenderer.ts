import gsap from "gsap"
import { Application, Assets, Container, Graphics, Sprite } from "pixi.js"
import type { Text, Texture } from "pixi.js"
import { initialStageState } from "../game/stageModel"
import type { StageEffect, StageState } from "../game/stageModel"
import { BreathingCycleSeconds } from "../motion/timings"
import { resolveArt } from "./artIndex"
import {
  createPlate,
  createStageText,
  Fonts,
  getFogGradient,
  playBeam,
  playDamageNumber,
  playFade,
  playHitShake,
  playImpactBurst,
  playLevelUpFlash,
  playMiss,
  playPlate,
  playSlash,
  playStandOff,
} from "./effects"
import { Palette } from "./palette"
import { StageHeight } from "./stageLayout"

export type StageHandle = Pick<StageRenderer, "showEncounter" | "updateHealth" | "play">

const Gutter = 14
const BarWidth = 96
const BarHeight = 5
const PlayerBox = { left: 10, width: 100, height: 170 }
const MonsterBox = { right: 4, width: 130, height: 180 }
const MonsterNameRight = 14
const MonsterNameSize = 28
const MonsterPlateCenterY = 120
const MonsterPlateMaxWidth = 176
const MonsterPlatePadding = { x: 14, y: 6 }
const FogHeight = 70
const PlayerLabel = "你"

type Box = { width: number; height: number }
// slot 由版面定位在立繪底部中心；body 承載呼吸與受擊抖動
type Figure = { slot: Container; body: Container }
type HealthBar = { root: Container; label: Text; meter: Graphics; color: number }

const fontFaces = [
  `400 16px "${Fonts.ui}"`,
  `700 16px "${Fonts.ui}"`,
  `700 16px "${Fonts.title}"`,
  `700 16px "${Fonts.latin}"`,
]

const fontReady = (text: string): Promise<unknown> =>
  Promise.all(fontFaces.map((font) => document.fonts.load(font, text))).catch(() => undefined)

export class StageRenderer {
  private readonly app: Application
  private readonly mountains = new Graphics()
  private readonly fog = new Graphics()
  private readonly player = createFigure()
  private readonly monster = createFigure()
  private readonly playerBar = createHealthBar(PlayerLabel, Palette.crimson, "left")
  private readonly monsterBar = createHealthBar("", Palette.crimson, "right")
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
      this.fog,
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
      fontReady(`${PlayerLabel}CRITICALLEVELUPMISS膠著0123456789,`),
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
    drawMeter(this.playerBar, state.playerHp, state.playerHpMax)
    drawMeter(this.monsterBar, state.monsterHp, state.monsterHpMax)
  }

  play(effects: StageEffect[]): void {
    const width = this.app.screen.width
    const monsterX = this.monster.slot.x
    // 傷害數字落在目標內側，避開怪物名字與玩家的劍
    const damageX = { player: this.player.slot.x + 70, monster: monsterX - 80 }
    for (const effect of effects) {
      switch (effect.type) {
        case "slash":
          playSlash(this.effects, width)
          break
        case "beam":
          playBeam(this.effects, width)
          break
        case "damageNumber":
          playDamageNumber(this.effects, damageX[effect.side], 70, effect.value, effect.critical)
          break
        case "miss":
          playMiss(this.effects, damageX[effect.side], 70)
          break
        case "plate":
          playPlate(this.effects, width * 0.48, 82, effect.text, effect.tone, width - Gutter * 4)
          break
        case "hitShake":
          playHitShake(this[effect.side].body)
          break
        case "impactBurst":
          playImpactBurst(this.effects, monsterX - 16, StageHeight - 95)
          break
        case "standOff":
          playStandOff(this.player.body, -1)
          playStandOff(this.monster.body, 1)
          break
        case "levelUpFlash":
          playLevelUpFlash(this.effects, this.player.slot.x, StageHeight - PlayerBox.height / 2)
          break
        case "fade":
          playFade(this.app.stage, effect.seconds)
          break
      }
    }
  }

  destroy(): void {
    if (this.destroyed) return
    this.destroyed = true
    const animated = [this.app.stage, this.player.body, this.monster.body, ...this.effects.children]
    gsap.killTweensOf(animated.flatMap((target) => [target, target.scale]))
    this.app.renderer.off("resize", this.onResize)
    this.app.destroy({ removeView: true }, { children: true })
  }

  private layout() {
    const width = this.app.screen.width
    drawMountains(this.mountains, width)
    this.fog
      .clear()
      .rect(0, StageHeight - FogHeight, width, FogHeight)
      .fill(getFogGradient())
    this.player.slot.position.set(PlayerBox.left + PlayerBox.width / 2, StageHeight)
    this.monster.slot.position.set(width - MonsterBox.right - MonsterBox.width / 2, StageHeight)
    this.playerBar.root.position.set(Gutter, 10)
    this.monsterBar.root.position.set(width - Gutter - BarWidth, 10)
    this.updateHealth(this.state)
  }

  // 沒有立繪時以橫排名牌代替，太長的名字縮小到名牌寬度內
  private drawMonsterName(name: string) {
    const text = createStageText(name, {
      fontFamily: Fonts.title,
      fontSize: MonsterNameSize,
      fontWeight: "700",
    })
    text.anchor.set(0.5)
    const available = MonsterPlateMaxWidth - MonsterPlatePadding.x * 2
    if (text.width > available) text.scale.set(available / text.width)
    const plate = new Container()
    plate.addChild(createPlate(text, MonsterPlatePadding.x, MonsterPlatePadding.y), text)
    const slotX = this.monster.slot.x
    const rightmostCenter = this.app.screen.width - MonsterNameRight - plate.width / 2
    plate.position.set(Math.min(slotX, rightmostCenter) - slotX, MonsterPlateCenterY - StageHeight)
    this.monster.body.addChild(plate)
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
  for (const child of body.removeChildren()) child.destroy({ children: true })
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
  const text = createStageText(label, { fontSize: 11 })
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
    .fill({ color: Palette.text, alpha: 0.12 })
  if (ratio > 0) bar.meter.roundRect(0, 0, BarWidth * ratio, BarHeight, 3).fill(bar.color)
}

// 夜色遠景：後排山脊用 night 剪影，前排是 bgBottom 80% 的近山（viewBox 0 0 300 100）
const drawMountains = (graphics: Graphics, width: number) => {
  const scaleX = width / 300
  const top = StageHeight - 100
  const point = (x: number, y: number): [number, number] => [x * scaleX, top + y]
  graphics
    .clear()
    .moveTo(...point(0, 100))
    .lineTo(...point(0, 55))
    .quadraticCurveTo(...point(30, 20), ...point(70, 45))
    .quadraticCurveTo(...point(110, 5), ...point(150, 38))
    .quadraticCurveTo(...point(200, 0), ...point(240, 32))
    .quadraticCurveTo(...point(275, 18), ...point(300, 40))
    .lineTo(...point(300, 100))
    .closePath()
    .fill(Palette.night)
    .moveTo(...point(0, 100))
    .lineTo(...point(0, 70))
    .quadraticCurveTo(...point(40, 30), ...point(80, 60))
    .quadraticCurveTo(...point(120, 20), ...point(170, 55))
    .quadraticCurveTo(...point(220, 25), ...point(260, 50))
    .quadraticCurveTo(...point(285, 40), ...point(300, 55))
    .lineTo(...point(300, 100))
    .closePath()
    .fill({ color: Palette.bgBottom, alpha: 0.8 })
}

// 對照 battle-v2.html 的玩家剪影（viewBox 110×200，底部中心為原點），以淡色輪廓呈現
const drawPlayerSilhouette = (body: Container) => {
  const scale = PlayerBox.height / 200
  const silhouette = { color: Palette.text, alpha: 0.3 }
  const graphics = new Graphics()
    .circle(0, -165, 15)
    .fill(silhouette)
    .moveTo(-35, 0)
    .quadraticCurveTo(-29, -110, 0, -140)
    .quadraticCurveTo(29, -110, 37, 0)
    .closePath()
    .fill(silhouette)
    .moveTo(15, -130)
    .lineTo(53, -192)
    .stroke({ width: 3, ...silhouette })
  graphics.scale.set(scale)
  body.addChild(graphics)
}
