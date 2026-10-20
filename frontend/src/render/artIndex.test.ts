import { describe, expect, it } from "vitest"
import { createArtResolver } from "./artIndex"

describe("artIndex", () => {
  it("artResolver_ReturnsUrl_ForKnownId_AndNull_Otherwise", () => {
    const resolve = createArtResolver({ "../assets/art/snow-sprite.webp": "/x.webp" })

    expect(resolve("snow-sprite")).toBe("/x.webp")
    expect(resolve("frost-wolf")).toBeNull()
  })
})
