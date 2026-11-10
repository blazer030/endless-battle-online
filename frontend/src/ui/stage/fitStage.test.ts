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
  // 比 1.6 扁、左右本來就要補背景：舞台盡量拉高（最高 780），倍率不低於 1；視窗比 624 矮時才縮小
  { width: 375, height: 555, scale: 0.8894, stageHeight: 624, sideSpace: 14.1, topSpace: 0 },
  { width: 414, height: 660, scale: 1, stageHeight: 660, sideSpace: 12, topSpace: 0 },
  { width: 1440, height: 720, scale: 1, stageHeight: 720, sideSpace: 525, topSpace: 0 },
  { width: 820, height: 1180, scale: 1.5128, stageHeight: 780, sideSpace: 115, topSpace: 0 },
  // 電腦大螢幕也不設倍率上限
  { width: 1920, height: 950, scale: 1.2179, stageHeight: 780, sideSpace: 722.5, topSpace: 0 },
])("fitStage_$width×$height", ({ width, height, scale, stageHeight, sideSpace, topSpace }) => {
  const fit = fitStage(width, height)

  expect(fit.scale).toBeCloseTo(scale, 4)
  expect(oneDecimal(fit.stageHeight)).toBe(stageHeight)
  expect(oneDecimal(fit.offsetX)).toBe(sideSpace)
  expect(oneDecimal(fit.offsetY)).toBe(topSpace)
})
