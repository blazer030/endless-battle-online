// 新畫面的舞台：寬固定、高在範圍內伸縮，整個舞台依視窗等比縮放
export const StageWidth = 390
export const StageMinimumHeight = 683
export const StageMaximumHeight = 722
export const StageMaximumScale = 1.25

export type StageFit = {
  scale: number
  // 舞台本身（縮放前）的高度
  stageHeight: number
  // 縮放後的舞台在視窗裡的左上角，也就是左右、上下各留白多少
  offsetX: number
  offsetY: number
}

export const fitStage = (width: number, height: number): StageFit => {
  const scale = Math.min(width / StageWidth, height / StageMinimumHeight, StageMaximumScale)
  const stageHeight = Math.min(Math.max(height / scale, StageMinimumHeight), StageMaximumHeight)
  return {
    scale,
    stageHeight,
    offsetX: (width - StageWidth * scale) / 2,
    offsetY: (height - stageHeight * scale) / 2,
  }
}
