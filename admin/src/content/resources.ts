import {
  BugOutlined,
  EnvironmentOutlined,
  GlobalOutlined,
  IdcardOutlined,
  ShoppingOutlined,
  ThunderboltOutlined,
} from "@ant-design/icons"
import type { ComponentType } from "react"

// 第一版可編輯的內容種類；name 是 admin API 的 /admin/content/{kind}，icon 是 sidebar 用的圖示
export type ContentResource = {
  name: string
  label: string
  icon: ComponentType
  columns: string[]
}

export const contentResources: ContentResource[] = [
  {
    name: "items",
    label: "道具",
    icon: ShoppingOutlined,
    columns: ["id", "name", "category", "shopLevel", "price"],
  },
  {
    name: "monsters",
    label: "怪物",
    icon: BugOutlined,
    columns: ["id", "name", "level", "placeId"],
  },
  {
    name: "jobs",
    label: "職業",
    icon: IdcardOutlined,
    columns: ["id", "name", "tier", "parentId"],
  },
  {
    name: "skills",
    label: "技能",
    icon: ThunderboltOutlined,
    columns: ["id", "name", "jobId", "type"],
  },
  { name: "areas", label: "大陸", icon: GlobalOutlined, columns: ["id", "name"] },
  {
    name: "places",
    label: "地區",
    icon: EnvironmentOutlined,
    columns: ["id", "name", "recommended"],
  },
]
