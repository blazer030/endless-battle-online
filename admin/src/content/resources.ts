import {
  BugOutlined,
  EnvironmentOutlined,
  GlobalOutlined,
  IdcardOutlined,
  ShoppingOutlined,
  ThunderboltOutlined,
} from "@ant-design/icons"
import type { ComponentType } from "react"
import { ItemListPage } from "./items/ItemListPage"
import { JobListPage } from "./jobs/JobListPage"
import { MonsterListPage } from "./monsters/MonsterListPage"
import { SkillListPage } from "./skills/SkillListPage"
import { AreaListPage } from "./world/AreaListPage"
import { PlaceListPage } from "./world/PlaceListPage"

// 可編輯的內容種類；name 也是網址路徑，icon 是 sidebar 用的圖示
export type ContentResource = {
  name: string
  label: string
  icon: ComponentType
  page: ComponentType
}

export const contentResources: ContentResource[] = [
  { name: "items", label: "道具", icon: ShoppingOutlined, page: ItemListPage },
  { name: "monsters", label: "怪物", icon: BugOutlined, page: MonsterListPage },
  { name: "jobs", label: "職業", icon: IdcardOutlined, page: JobListPage },
  { name: "skills", label: "技能", icon: ThunderboltOutlined, page: SkillListPage },
  { name: "areas", label: "大陸", icon: GlobalOutlined, page: AreaListPage },
  { name: "places", label: "地區", icon: EnvironmentOutlined, page: PlaceListPage },
]
