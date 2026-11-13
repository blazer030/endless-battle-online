import { describe, expect, it } from "vitest"
import { personalityLabel } from "./pets"

describe("personalityLabel", () => {
  it("uses the WOG personality names", () => {
    expect(personalityLabel("Aggressive")).toBe("積極")
    expect(personalityLabel("Cold")).toBe("冷酷")
    expect(personalityLabel("Ironwall")).toBe("鐵壁")
    expect(personalityLabel("Desperate")).toBe("危急")
  })
})
