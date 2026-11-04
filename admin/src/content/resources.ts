// 第一版可編輯的內容種類；name 是 admin API 的 /admin/content/{kind}
export type ContentResource = { name: string; label: string; columns: string[] }

export const contentResources: ContentResource[] = [
  { name: "items", label: "道具", columns: ["id", "name", "category", "shopLevel", "price"] },
  { name: "monsters", label: "怪物", columns: ["id", "name", "level", "placeId"] },
  { name: "jobs", label: "職業", columns: ["id", "name", "tier", "parentId"] },
  { name: "skills", label: "技能", columns: ["id", "name", "jobId", "type"] },
  { name: "areas", label: "大陸", columns: ["id", "name"] },
  { name: "places", label: "地區", columns: ["id", "name", "recommended"] },
]
