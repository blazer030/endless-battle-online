import gsap from "gsap"
import { Container } from "pixi.js"
import { expect, test } from "vitest"
import { playDamageNumber, playDropFloat, stopTweens } from "./effects"

// 用 updateRoot 推進 GSAP 的時間，收集推進時丟出的錯誤
const advanceCollectingErrors = (from: number, seconds: number) => {
  const errors: string[] = []
  for (let elapsed = 0.05; elapsed <= seconds; elapsed += 0.05) {
    try {
      gsap.updateRoot(from + elapsed)
    } catch (error) {
      errors.push(String(error))
    }
  }
  return errors
}

// 舞台銷毀時傷害數字與掉落物還在飄：後面的關鍵影格、還在 delay 的都要停掉，銷毀後不再讀已銷毀的物件
test("StopTweens_StopsEffectsMidFlightBeforeTheStageIsDestroyed", () => {
  const start = gsap.globalTimeline.time()
  const layer = new Container()
  const other = new Container()
  playDamageNumber(layer, 100, 70, 12, false)
  playDropFloat(layer, 100, 70, "藥草", 2)
  gsap.updateRoot(start + 0.05)

  stopTweens([...layer.children.flatMap((child) => [child, child.scale]), other, other.scale])
  layer.destroy({ children: true })

  expect(advanceCollectingErrors(start + 0.05, 3)).toEqual([])
})

// 最小重現：一次傳好幾個對象時，還沒開始的補間也要停
test("StopTweens_StopsTweensThatHaveNotStarted", () => {
  const start = gsap.globalTimeline.time()
  const target = new Container()
  const other = new Container()
  gsap.to(target, { delay: 0.5, y: -30, duration: 0.3 })

  stopTweens([target, other])
  target.destroy()

  expect(advanceCollectingErrors(start, 1)).toEqual([])
})
