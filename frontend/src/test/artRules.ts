// 美術圖規格（只給測試用）；說明見 docs/art/README.md
export const ArtCanvas = {
  player: { width: 600, height: 1020 },
  monster: { width: 720, height: 1000 },
} as const

export const MaxArtBytes = 153_600

export type ArtClass = keyof typeof ArtCanvas

export const artClassOf = (id: string, monsterIds: ReadonlySet<string>): ArtClass | null => {
  if (id === "player") return "player"
  return monsterIds.has(id) ? "monster" : null
}

const fourCC = (bytes: Uint8Array, offset: number): string =>
  String.fromCharCode(
    bytes[offset] ?? 0,
    bytes[offset + 1] ?? 0,
    bytes[offset + 2] ?? 0,
    bytes[offset + 3] ?? 0,
  )

export const readWebpSize = (bytes: Uint8Array): { width: number; height: number } | null => {
  if (bytes.length < 25 || fourCC(bytes, 0) !== "RIFF" || fourCC(bytes, 8) !== "WEBP") return null
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength)
  const chunk = fourCC(bytes, 12)

  if (chunk === "VP8X" && bytes.length >= 30) {
    const width = (view.getUint8(24) | (view.getUint8(25) << 8) | (view.getUint8(26) << 16)) + 1
    const height = (view.getUint8(27) | (view.getUint8(28) << 8) | (view.getUint8(29) << 16)) + 1
    return { width, height }
  }
  if (chunk === "VP8L") {
    if (view.getUint8(20) !== 0x2f) return null
    const packed = view.getUint32(21, true)
    return { width: (packed & 0x3fff) + 1, height: ((packed >>> 14) & 0x3fff) + 1 }
  }
  if (chunk === "VP8 " && bytes.length >= 30) {
    if (view.getUint8(23) !== 0x9d || view.getUint8(24) !== 0x01 || view.getUint8(25) !== 0x2a)
      return null
    return { width: view.getUint16(26, true) & 0x3fff, height: view.getUint16(28, true) & 0x3fff }
  }
  return null
}
