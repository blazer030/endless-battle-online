import gsap from "gsap"
import { Application, Assets, Container, Graphics, Sprite } from "pixi.js"
import type { Text, Texture } from "pixi.js"
import { initialStageState } from "../game/stageModel"
import type { StageEffect, StageState } from "../game/stageModel"
import { BreathingCycleSeconds } from "../motion/timings"
import { contentArtCandidates, loadFirst } from "./contentAssets"
import {
  createPlate,
  createStageText,
  Fonts,
  getFogGradient,
  playAuraFlash,
  playBeam,
  playDamageNumber,
  playDropFloat,
  playFade,
  playHitShake,
  playImpactBurst,
  playMiss,
  playPlate,
  playSlash,
  playStandOff,
  stopTweens,
} from "./effects"
import { Palette } from "./palette"
import { PetBox, StageHeight } from "./stageLayout"

const clamp = (value: number, minimum: number, maximum: number) =>
  Math.max(minimum, Math.min(value, maximum))

export type StageHandle = Pick<StageRenderer, "showEncounter" | "updateHealth" | "play">

const Gutter = 14
const BarWidth = 96
const BarHeight = 5
const PlayerBox = { left: 10, width: 100, height: 170 }
const MonsterBox = { right: 4, width: 130, height: 180 }
const PetNameSize = 14
const PetPlatePadding = { x: 6, y: 3 }
const PetRetireSeconds = 0.6
const MonsterFadeSeconds = 0.6
const MonsterNameRight = 14
const MonsterNameSize = 28
const MonsterPlateCenterY = 120
const MonsterPlateMaxWidth = 176
const MonsterPlatePadding = { x: 14, y: 6 }
const FogHeight = 70
const SidePlateY = 56
const SidePlateMaxWidth = 150
const PlayerLabel = "你"

type Box = { width: number; height: number }
// slot 由版面定位在立繪底部中心；body 承載呼吸與受擊抖動
// art 只放立繪（或沒有立繪時代替它的怪物名牌），換圖時只清這一層；寵物名牌放在 art 外面
type Figure = { slot: Container; body: Container; art: Container }
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
  private readonly pet = createFigure()
  private readonly monster = createFigure()
  private readonly playerBar = createHealthBar(PlayerLabel, Palette.crimson, "left")
  private readonly monsterBar = createHealthBar("", Palette.crimson, "right")
  private readonly effects = new Container()
  private readonly onResize = () => this.layout()
  private state: StageState = initialStageState
  private monsterToken = 0
  private petToken = 0
  private destroyed = false

  private constructor(app: Application) {
    this.app = app
    app.stage.addChild(
      this.mountains,
      this.player.slot,
      this.pet.slot,
      this.monster.slot,
      this.fog,
      this.playerBar.root,
      this.monsterBar.root,
      this.effects,
    )
    drawPlayerSilhouette(this.player.body)
    for (const figure of [this.player, this.pet, this.monster]) breathe(figure.body)
    app.renderer.on("resize", this.onResize)
    this.layout()
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
      fontReady(`${PlayerLabel}LEVELUPMISS膠著0123456789,`),
    ])
    container.appendChild(app.canvas)
    return new StageRenderer(app)
  }

  showEncounter(monsterId: string, monsterName: string, state: StageState): void {
    const token = ++this.monsterToken
    const isCurrent = () => !this.destroyed && token === this.monsterToken
    // 上一隻逃跑時淡出了，新的怪物要回到不透明
    gsap.killTweensOf(this.monster.slot)
    this.monster.slot.alpha = 1
    clearFigure(this.monster)
    this.monsterBar.label.text = ""
    this.updateHealth(state)
    this.showPet(state)

    void this.showArt(
      contentArtCandidates("monsters", monsterId),
      this.monster,
      MonsterBox,
      isCurrent,
    )
    void fontReady(monsterName).then(() => {
      if (!isCurrent()) return
      this.monsterBar.label.text = monsterName
      if (this.monster.art.children.length === 0) this.drawMonsterName(monsterName)
    })
  }

  private showPet(state: StageState) {
    const token = ++this.petToken
    const isCurrent = () => !this.destroyed && token === this.petToken
    gsap.killTweensOf(this.pet.slot)
    clearFigure(this.pet)
    this.pet.slot.alpha = 1
    this.pet.slot.visible = state.petActive
    if (!state.petActive || state.petName === null) return

    const name = state.petName
    // 寵物沒有血條，名牌字型好了就畫、一直留著；圖另外載入，只換 art 那一層（沒有圖就是佔位圖）
    if (state.petMonsterId !== null)
      void this.showArt(
        contentArtCandidates("monsters", state.petMonsterId),
        this.pet,
        PetBox,
        isCurrent,
      )
    void fontReady(name).then(() => {
      if (isCurrent()) this.drawPetName(name)
    })
  }

  updateHealth(state: StageState): void {
    this.state = state
    drawMeter(this.playerBar, state.playerHp, state.playerHpMax)
    drawMeter(this.monsterBar, state.monsterHp, state.monsterHpMax)
    // NPC 沒有血條
    this.monsterBar.root.visible = state.monsterHpMax > 0
  }

  play(effects: StageEffect[]): void {
    const width = this.app.screen.width
    const monsterX = this.monster.slot.x
    // 傷害數字落在目標內側，避開怪物名字與玩家的劍
    const damageX = {
      player: this.player.slot.x + 70,
      monster: monsterX - 80,
      pet: this.pet.slot.x,
    }
    const auraY = StageHeight - PlayerBox.height / 2
    let dropOrder = 0
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
          // 技能名是不在預載範圍的中文，字形要等載入後才畫，否則會先用備用字型
          void fontReady(effect.text).then(() => {
            if (this.destroyed) return
            const stageWidth = this.app.screen.width
            // 指定一方時名牌落在該方頭頂，寬度以該方的框為限
            const anchor =
              effect.over === undefined
                ? { x: stageWidth * 0.48, y: 82, maxWidth: stageWidth - Gutter * 4 }
                : {
                    x: clamp(
                      this[effect.over].slot.x,
                      Gutter + SidePlateMaxWidth / 2,
                      stageWidth - Gutter - SidePlateMaxWidth / 2,
                    ),
                    y: effect.standing === true ? MonsterPlateCenterY : SidePlateY,
                    maxWidth: SidePlateMaxWidth,
                  }
            playPlate(this.effects, anchor.x, anchor.y, effect.text, effect.tone, anchor.maxWidth)
          })
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
          playAuraFlash(this.effects, this.player.slot.x, auraY, Palette.gold)
          break
        case "skillFlash":
          playAuraFlash(this.effects, this.player.slot.x, auraY, Palette.cyan)
          break
        case "potionFlash":
          playAuraFlash(this.effects, this.player.slot.x, auraY, Palette.green)
          break
        case "dropFloat": {
          const order = dropOrder++
          // 物品名是中文，等字形載入後才畫
          void fontReady(effect.text).then(() => {
            if (this.destroyed) return
            playDropFloat(
              this.effects,
              this.monster.slot.x - 16,
              StageHeight - 110,
              effect.text,
              order,
            )
          })
          break
        }
        case "petRetire":
          gsap.to(this.pet.slot, { alpha: 0, duration: PetRetireSeconds, ease: "power1.in" })
          break
        case "monsterFade":
          gsap.to(this.monster.slot, { alpha: 0, duration: MonsterFadeSeconds, ease: "power1.in" })
          break
        case "clearMonster":
          // 讓還在等字型的 showEncounter 不會再把名稱寫回去
          this.monsterToken++
          gsap.to(this.monster.slot, { alpha: 0, duration: MonsterFadeSeconds, ease: "power1.in" })
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
    const animated = [
      this.app.stage,
      this.player.body,
      this.pet.body,
      this.monster.body,
      ...this.effects.children,
    ]
    stopTweens([
      ...animated.flatMap((target) => [target, target.scale]),
      this.pet.slot,
      this.monster.slot,
    ])
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
    this.pet.slot.position.set(PetBox.left + PetBox.width / 2, StageHeight)
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
    this.monster.art.addChild(plate)
  }

  private drawPetName(name: string) {
    const text = createStageText(name, {
      fontFamily: Fonts.title,
      fontSize: PetNameSize,
      fontWeight: "700",
    })
    text.anchor.set(0.5)
    const available = PetBox.width - PetPlatePadding.x * 2
    if (text.width > available) text.scale.set(available / text.width)
    const plate = new Container()
    plate.addChild(createPlate(text, PetPlatePadding.x, PetPlatePadding.y), text)
    plate.position.set(0, -plate.height / 2 - 24)
    this.pet.body.addChild(plate)
  }

  // 依序嘗試每個網址，第一個載入成功的就用；全部失敗就保持原樣（名字仍在血條或名牌上）
  private async showArt(
    urls: readonly string[],
    figure: Figure,
    box: Box,
    isCurrent: () => boolean,
  ) {
    const texture = await loadFirst(urls, (url) => Assets.load<Texture>(url))
    if (texture === null || this.destroyed || !isCurrent()) return
    const sprite = new Sprite(texture)
    sprite.anchor.set(0.5, 1)
    sprite.scale.set(Math.min(box.width / texture.width, box.height / texture.height))
    clearBody(figure.art)
    figure.art.addChild(sprite)
  }
}

const createFigure = (): Figure => {
  const slot = new Container()
  const body = new Container()
  const art = new Container()
  slot.addChild(body)
  body.addChild(art)
  return { slot, body, art }
}

const clearBody = (body: Container) => {
  for (const child of body.removeChildren()) child.destroy({ children: true })
}

const clearFigure = (figure: Figure) => {
  clearBody(figure.art)
  for (const child of [...figure.body.children])
    if (child !== figure.art) {
      figure.body.removeChild(child)
      child.destroy({ children: true })
    }
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
