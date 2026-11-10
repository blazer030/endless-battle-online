// 新畫面的舞台：寬固定、高在範圍內伸縮，整個舞台依視窗等比縮放（不設倍率上限）
export const StageWidth = 390
export const StageMinimumHeight = 624
export const StageMaximumHeight = 780

export type StageFit = {
  scale: number
  // 舞台本身（縮放前）的高度
  stageHeight: number
  // 縮放後的舞台在視窗裡的左上角，也就是左右、上下各留白多少
  offsetX: number
  offsetY: number
}

const clampHeight = (height: number) =>
  Math.min(Math.max(height, StageMinimumHeight), StageMaximumHeight)

export const fitStage = (width: number, height: number): StageFit => {
  // 視窗不比 1.6 扁：照寬度貼滿，高度在範圍內伸縮（比 2.0 細長時停在最高、上下補背景）
  // 比 1.6 扁、左右本來就要補背景：舞台盡量拉高、倍率不低於 1，比最矮還矮時才縮小
  const fillsWidth = height * StageWidth >= width * StageMinimumHeight
  const scale = fillsWidth ? width / StageWidth : height / clampHeight(height)
  const stageHeight = clampHeight(height / scale)
  return {
    scale,
    stageHeight,
    offsetX: (width - StageWidth * scale) / 2,
    offsetY: (height - stageHeight * scale) / 2,
  }
}
