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
  potionHealthPointsPercent: "15",
  potionManaPointsPercent: "100",
  restHealthPointsPercent: "15",
  restManaPointsPercent: "15",
  escapeHealthPointsPercent: "0",
  ...overrides,
})

describe("settingsForm", () => {
  test("settingsForm_ConvertsCharacterSettingsToStrings", () => {
    const character = makeCharacter({
      settings: {
        attack: "Magic",
        potionHealthPointsPercent: 30,
        potionManaPointsPercent: -1,
        restHealthPointsPercent: 20,
        restManaPointsPercent: -1,
        escapeHealthPointsPercent: 10,
      },
    })

    expect(settingsForm(character)).toEqual({
      attack: "Magic",
      potionHealthPointsPercent: "30",
      potionManaPointsPercent: "-1",
      restHealthPointsPercent: "20",
      restManaPointsPercent: "-1",
      escapeHealthPointsPercent: "10",
    })
  })

  test("settingsForm_AutomaticAttackIsAuto", () => {
    expect(settingsForm(makeCharacter()).attack).toBe("auto")
  })
})

describe("parseSettings", () => {
  test("parseSettings_ReturnsCommandWhenValid", () => {
    expect(
      parseSettings(
        form({
          attack: "Physical",
          potionManaPointsPercent: " -1 ",
          escapeHealthPointsPercent: "100",
        }),
      ),
    ).toEqual({
      ok: true,
      command: {
        attack: "Physical",
        potionHealthPointsPercent: 15,
        potionManaPointsPercent: -1,
        restHealthPointsPercent: 15,
        restManaPointsPercent: 15,
        escapeHealthPointsPercent: 100,
      },
    })
  })

  test("parseSettings_AutoAttackIsNull", () => {
    const parsed = parseSettings(form())
    expect(parsed.ok && parsed.command.attack).toBeNull()
  })

  test.each<[string, Partial<SettingsForm>]>([
    ["empty", { potionHealthPointsPercent: "" }],
    ["decimal", { restHealthPointsPercent: "12.5" }],
    ["not a number", { restManaPointsPercent: "abc" }],
    ["below -1", { potionManaPointsPercent: "-2" }],
    ["above 100", { potionHealthPointsPercent: "101" }],
    ["escape below 0", { escapeHealthPointsPercent: "-1" }],
    ["escape above 100", { escapeHealthPointsPercent: "101" }],
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
