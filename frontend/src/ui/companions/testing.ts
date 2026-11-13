import { vi } from "vitest"
import type { ApiClient } from "../../api/client"
import { MercenaryOverviewVo, type MercenaryContract } from "../../game/mercenaries"
import { makeMercenaryOverview, makePet, makePets, makeWorld } from "../../test/fixtures"
import type { CompanionsApi } from "./CompanionsScreen"

// 同伴畫面測試共用的假 API：兩隻寵物（小風攜帶中）與一份戰鬥中的契約
export const fighting: MercenaryContract = {
  mercenaryId: "mercenary-f",
  name: "葛雷",
  remaining: 64,
  placeId: 4,
  placeName: "迷霧森林",
  fighting: true,
  nextFightAt: null,
}

const twoPets = makePets([
  makePet({ id: 1, name: "小風", carried: true }),
  makePet({ id: 2, name: "夜影" }),
])
const withContract = makeMercenaryOverview({ contract: fighting })

export const createCompanionsApi = (
  pets = twoPets,
  mercenaries: MercenaryOverviewVo = withContract,
): CompanionsApi & Pick<ApiClient, "advance"> => ({
  getPets: vi.fn().mockResolvedValue(pets),
  carryPet: vi.fn(),
  storePet: vi.fn(),
  releasePet: vi.fn(),
  renamePet: vi.fn(),
  feedPet: vi.fn(),
  getMercenaries: vi.fn().mockResolvedValue(mercenaries),
  hireMercenary: vi.fn(),
  deployMercenary: vi.fn(),
  restMercenary: vi.fn(),
  renameMercenary: vi.fn(),
  dismissMercenary: vi.fn(),
  getWorld: vi.fn().mockResolvedValue(makeWorld()),
  getMyCharacter: vi.fn(),
  advance: vi.fn(),
})
