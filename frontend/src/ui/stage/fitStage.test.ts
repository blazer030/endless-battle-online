import { expect, test } from "vitest"
import { fitStage } from "./fitStage"

// 表上的數字四捨五入到一位小數
const oneDecimal = (value: number) => Math.round(value * 10) / 10

// spec screen-layout 的 stage fit 範例表；留白＝(視窗 − 舞台×倍率) ÷ 2
test.each([
  { width: 390, height: 683, scale: 1, stageHeight: 683, sideSpace: 0, topSpace: 0 },
  { width: 390, height: 722, scale: 1, stageHeight: 722, sideSpace: 0, topSpace: 0 },
  { width: 402, height: 714, scale: 1.0308, stageHeight: 692.7, sideSpace: 0, topSpace: 0 },
  { width: 412, height: 880, scale: 1.0564, stageHeight: 722, sideSpace: 0, topSpace: 58.6 },
  { width: 375, height: 555, scale: 0.8126, stageHeight: 683, sideSpace: 29.0, topSpace: 0 },
  // 電腦大螢幕也以較緊的一邊貼滿，不設倍率上限
  { width: 1920, height: 950, scale: 1.3909, stageHeight: 683, sideSpace: 688.8, topSpace: 0 },
])("fitStage_$width×$height", ({ width, height, scale, stageHeight, sideSpace, topSpace }) => {
  const fit = fitStage(width, height)

  expect(fit.scale).toBeCloseTo(scale, 4)
  expect(oneDecimal(fit.stageHeight)).toBe(stageHeight)
  expect(oneDecimal(fit.offsetX)).toBe(sideSpace)
  expect(oneDecimal(fit.offsetY)).toBe(topSpace)
})
