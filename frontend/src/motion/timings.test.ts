import { expect, test } from "vitest"
import {
  ActionWaitDelaySeconds,
  ActionWaitFadeInSeconds,
  ActionWaitFadeOutSeconds,
  ActionWaitMinimumMilliseconds,
  InkDotsDelaySeconds,
  PopupInSeconds,
  PopupOutSeconds,
} from "./timings"

// 規格 action-feedback「Waiting delay matches the title screen」與時長表
test("ActionWaitDelayMatchesInkDots", () => {
  expect(ActionWaitDelaySeconds).toBe(InkDotsDelaySeconds)
  expect([
    ActionWaitDelaySeconds,
    ActionWaitMinimumMilliseconds,
    ActionWaitFadeInSeconds,
    ActionWaitFadeOutSeconds,
    PopupInSeconds,
    PopupOutSeconds,
  ]).toEqual([0.45, 300, 0.2, 0.15, 0.22, 0.15])
})
