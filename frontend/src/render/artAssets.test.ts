/// <reference types="node" />
import { readdirSync, readFileSync, statSync } from "node:fs"
import { join } from "node:path"
import { describe, expect, it } from "vitest"
import { ArtCanvas, MaxArtBytes, artClassOf, readWebpSize } from "../test/artRules"

const ascii = (text: string) => [...text].map((character) => character.charCodeAt(0))
const le32 = (value: number) => [
  value & 0xff,
  (value >> 8) & 0xff,
  (value >> 16) & 0xff,
  (value >>> 24) & 0xff,
]
const le24 = (value: number) => [value & 0xff, (value >> 8) & 0xff, (value >> 16) & 0xff]

const riff = (chunk: number[]) =>
  new Uint8Array([...ascii("RIFF"), ...le32(chunk.length + 4), ...ascii("WEBP"), ...chunk])

const vp8x = (width: number, height: number) =>
  riff([...ascii("VP8X"), ...le32(10), 0, 0, 0, 0, ...le24(width - 1), ...le24(height - 1)])

const vp8l = (width: number, height: number) => {
  const packed = (width - 1) | ((height - 1) << 14)
  return riff([...ascii("VP8L"), ...le32(5), 0x2f, ...le32(packed)])
}

const vp8 = (width: number, height: number) =>
  riff([
    ...ascii("VP8 "),
    ...le32(10),
    0,
    0,
    0,
    0x9d,
    0x01,
    0x2a,
    width & 0xff,
    width >> 8,
    height & 0xff,
    height >> 8,
  ])

const artDirectory = join(import.meta.dirname, "../assets/art")
const monstersPath = join(import.meta.dirname, "../../../backend/content/monsters.json")

const readMonsterIds = (): Set<string> => {
  const parsed: unknown = JSON.parse(readFileSync(monstersPath, "utf8"))
  const ids = new Set<string>()
  if (
    typeof parsed === "object" &&
    parsed !== null &&
    "monsters" in parsed &&
    Array.isArray(parsed.monsters)
  ) {
    for (const monster of parsed.monsters as unknown[]) {
      if (
        typeof monster === "object" &&
        monster !== null &&
        "id" in monster &&
        typeof monster.id === "string"
      )
        ids.add(monster.id)
    }
  }
  return ids
}

describe("artRules", () => {
  it("readWebpSize reads VP8X, VP8L and VP8 canvases", () => {
    expect(readWebpSize(vp8x(600, 1020))).toEqual({ width: 600, height: 1020 })
    expect(readWebpSize(vp8l(720, 1000))).toEqual({ width: 720, height: 1000 })
    expect(readWebpSize(vp8(64, 48))).toEqual({ width: 64, height: 48 })
  })

  it("readWebpSize rejects non-webp bytes", () => {
    expect(readWebpSize(new Uint8Array(ascii("not a webp file at all, really nope")))).toBeNull()
    expect(readWebpSize(new Uint8Array([1, 2, 3]))).toBeNull()
    expect(
      readWebpSize(riff([...ascii("ABCD"), ...le32(10), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])),
    ).toBeNull()
  })

  it("artClassOf maps player, monsters and unknown ids", () => {
    const monsterIds = new Set(["meadow-slime"])

    expect(artClassOf("player", monsterIds)).toBe("player")
    expect(artClassOf("meadow-slime", monsterIds)).toBe("monster")
    expect(artClassOf("mystery", monsterIds)).toBeNull()
  })

  it("every art file is a known id with the class canvas and size budget", () => {
    const monsterIds = readMonsterIds()
    const files = readdirSync(artDirectory).filter((name) => name.endsWith(".webp"))

    for (const file of files) {
      const id = file.replace(/\.webp$/, "")
      const artClass = artClassOf(id, monsterIds)
      expect(artClass, `${file} is not a known id`).not.toBeNull()
      if (artClass === null) continue
      const path = join(artDirectory, file)
      expect(readWebpSize(new Uint8Array(readFileSync(path))), `${file} canvas`).toEqual(
        ArtCanvas[artClass],
      )
      expect(statSync(path).size, `${file} bytes`).toBeLessThanOrEqual(MaxArtBytes)
    }
  })
})
