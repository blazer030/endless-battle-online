import { vi } from "vitest"
import type { ApiClient } from "../../api/client"
import type { Mission, MissionBoardVo } from "../../game/missions"
import { makeMission, makeMissionBoard } from "../../test/fixtures"
import type { MissionsScreenApi } from "./MissionsScreen"

export const createMissionsApi = (
  board: MissionBoardVo = makeMissionBoard(),
): MissionsScreenApi & Pick<ApiClient, "advance"> => ({
  getMissions: vi.fn().mockResolvedValue(board),
  acceptMission: vi.fn(),
  abandonMission: vi.fn(),
  completeMission: vi.fn(),
  advance: vi.fn(),
})

// 測試用委託：每日的標題帶「[每日]」前綴（同正式內容）
export const daily = (id: string, level: number, overrides: Partial<Mission> = {}) =>
  makeMission({
    id,
    level,
    daily: true,
    title: `[每日]委託${id}`,
    giver: "冒險者公會",
    ...overrides,
  })

export const regular = (id: string, level = 1, overrides: Partial<Mission> = {}) =>
  makeMission({ id, level, title: `委託${id}`, giver: "村長", ...overrides })

// 示範的 Lv 150：每日 3 張可接＋1 張今天完成、一般 21 張可接、完成 2 張一般
export const lv150Board = (overrides: Partial<MissionBoardVo> = {}) =>
  makeMissionBoard({
    available: [
      daily("mission-222", 50),
      daily("mission-226", 150),
      daily("mission-227", 150),
      ...Array.from({ length: 21 }, (_, index) => regular(`mission-${index + 1}`, index + 1)),
    ],
    completed: [daily("mission-225", 50), regular("mission-90", 40), regular("mission-80", 30)],
    ...overrides,
  })
