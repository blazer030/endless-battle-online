import { describe, expect, test } from "vitest"
import { makeCharacter } from "../test/fixtures"
import {
  InvalidPercentMessage,
  parseSettings,
  settingsErrorMessage,
  settingsForm,
} from "./settings"
import type { SettingsForm } from "./settings"

const form = (overrides: Partial<SettingsForm> = {}): SettingsForm => ({
  attack: "auto",
  potionHpPercent: "15",
  potionSpPercent: "100",
  restHpPercent: "15",
  restSpPercent: "15",
  escapeHpPercent: "0",
  ...overrides,
})

describe("settingsForm", () => {
  test("settingsForm_ConvertsCharacterSettingsToStrings", () => {
    const character = makeCharacter({
      settings: {
        attack: "Magic",
        potionHpPercent: 30,
        potionSpPercent: -1,
        restHpPercent: 20,
        restSpPercent: -1,
        escapeHpPercent: 10,
      },
    })

    expect(settingsForm(character)).toEqual({
      attack: "Magic",
      potionHpPercent: "30",
      potionSpPercent: "-1",
      restHpPercent: "20",
      restSpPercent: "-1",
      escapeHpPercent: "10",
    })
  })

  test("settingsForm_AutomaticAttackIsAuto", () => {
    expect(settingsForm(makeCharacter()).attack).toBe("auto")
  })
})

describe("parseSettings", () => {
  test("parseSettings_ReturnsCommandWhenValid", () => {
    expect(
      parseSettings(form({ attack: "Physical", potionSpPercent: " -1 ", escapeHpPercent: "100" })),
    ).toEqual({
      ok: true,
      command: {
        attack: "Physical",
        potionHpPercent: 15,
        potionSpPercent: -1,
        restHpPercent: 15,
        restSpPercent: 15,
        escapeHpPercent: 100,
      },
    })
  })

  test("parseSettings_AutoAttackIsNull", () => {
    const parsed = parseSettings(form())
    expect(parsed.ok && parsed.command.attack).toBeNull()
  })

  test.each<[string, Partial<SettingsForm>]>([
    ["empty", { potionHpPercent: "" }],
    ["decimal", { restHpPercent: "12.5" }],
    ["not a number", { restSpPercent: "abc" }],
    ["below -1", { potionSpPercent: "-2" }],
    ["above 100", { potionHpPercent: "101" }],
    ["escape below 0", { escapeHpPercent: "-1" }],
    ["escape above 100", { escapeHpPercent: "101" }],
  ])("parseSettings_RejectsInvalidPercent_%s", (_name, overrides) => {
    expect(parseSettings(form(overrides))).toEqual({ ok: false, error: InvalidPercentMessage })
  })

  test("InvalidPercentMessage_MatchesCopy", () => {
    expect(InvalidPercentMessage).toBe("門檻必須是 -1～100 的整數（逃跑 0～100）")
  })
})

describe("settingsErrorMessage", () => {
  test("settingsErrorMessage_TranslatesKnownReasons", () => {
    expect(settingsErrorMessage(["invalidPercent"], "fallback")).toBe(InvalidPercentMessage)
    expect(settingsErrorMessage(["unknown"], "fallback")).toBe("fallback")
  })
})
