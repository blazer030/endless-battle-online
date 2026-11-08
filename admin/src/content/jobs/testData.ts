import type { JobDto } from "./JobConverter"

const range = (minimum: number, maximum: number) => ({ minimum, maximum })

export const jobDto = (overrides: Partial<JobDto> = {}): JobDto => ({
  id: "job-6",
  name: "戰士",
  tier: 1,
  parentJobId: null,
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
  growth: {
    strength: range(1, 3),
    intelligence: range(0, 1),
    life: range(1, 3),
    vitality: range(1, 3),
    agility: range(0, 1),
    charisma: range(1, 1),
    faith: range(0, 1),
  },
  ...overrides,
})

export const jobs = [
  jobDto(),
  jobDto({ id: "job-7", name: "術士" }),
  jobDto({ id: "job-10", name: "狂戰士", tier: 2, parentJobId: "job-6" }),
]
