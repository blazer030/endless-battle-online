// content API 不提供成長值，初階職業的介紹文字在此維護（依 backend/content/jobs.json 的 growth 撰寫）
export type JobBlurb = { role: string; growth: string }

export const jobBlurbs: Readonly<Partial<Record<string, JobBlurb>>> = {
  warrior: { role: "近身前排，攻守兼備", growth: "成長：力量／體質" },
  sorcerer: { role: "遠距施法，魔攻見長", growth: "成長：智力／信仰" },
  thief: { role: "身手敏捷，先發制人", growth: "成長：敏捷" },
  bard: { role: "樂聲支援，攻守靈活", growth: "成長：魅力" },
}
