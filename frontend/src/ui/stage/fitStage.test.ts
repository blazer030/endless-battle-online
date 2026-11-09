import { expect, test } from "vitest"
import { fitStage } from "./fitStage"

// 表上的數字四捨五入到一位小數
const oneDecimal = (value: number) => Math.round(value * 10) / 10

// spec screen-layout 的 stage fit 範例表；留白＝(視窗 − 舞台×倍率) ÷ 2
test.each([
  { width: 390, height: 624, scale: 1, stageHeight: 624, sideSpace: 0, topSpace: 0 },
  { width: 390, height: 780, scale: 1, stageHeight: 780, sideSpace: 0, topSpace: 0 },
  { width: 402, height: 714, scale: 1.0308, stageHeight: 692.7, sideSpace: 0, topSpace: 0 },
  { width: 412, height: 880, scale: 1.0564, stageHeight: 780, sideSpace: 0, topSpace: 28.0 },
  { width: 375, height: 555, scale: 0.8894, stageHeight: 624, sideSpace: 14.1, topSpace: 0 },
  // 電腦大螢幕也以較緊的一邊貼滿，不設倍率上限
  { width: 1920, height: 950, scale: 1.5224, stageHeight: 624, sideSpace: 663.1, topSpace: 0 },
])("fitStage_$width×$height", ({ width, height, scale, stageHeight, sideSpace, topSpace }) => {
  const fit = fitStage(width, height)

  expect(fit.scale).toBeCloseTo(scale, 4)
  expect(oneDecimal(fit.stageHeight)).toBe(stageHeight)
  expect(oneDecimal(fit.offsetX)).toBe(sideSpace)
  expect(oneDecimal(fit.offsetY)).toBe(topSpace)
})
