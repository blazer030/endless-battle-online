import { useQuery } from "@tanstack/react-query"
import { request } from "../../api/dataProvider"
import { parseArray, parseBoolean, parseNumber, parseObject, parseString } from "./parse"

export type NamedOption = { readonly id: string; readonly name: string }

export type SkillOption = NamedOption & { readonly maxLevel: number }

export type MissionOption = NamedOption & { readonly daily: boolean }

async function listNamedOptions(kind: string): Promise<NamedOption[]> {
  return parseArray(await request<unknown>(`/admin/content/${kind}`), kind, (item, path) => {
    const record = parseObject(item, path)
    return {
      id: parseString(record.id, `${path}.id`),
      name: parseString(record.name, `${path}.name`),
    }
  })
}

export const useItemOptions = () =>
  useQuery({
    queryKey: ["admin", "content", "items", "options"],
    queryFn: () => listNamedOptions("items"),
    retry: false,
  })

async function listSkillOptions(): Promise<SkillOption[]> {
  return parseArray(await request<unknown>("/admin/content/skills"), "skills", (item, path) => {
    const skill = parseObject(item, path)
    return {
      id: parseString(skill.id, `${path}.id`),
      name: parseString(skill.name, `${path}.name`),
      maxLevel: parseNumber(skill.maxLevel, `${path}.maxLevel`),
    }
  })
}

export const useSkillOptions = () =>
  useQuery({
    queryKey: ["admin", "content", "skills", "options"],
    queryFn: listSkillOptions,
    retry: false,
  })

async function listMissionOptions(): Promise<MissionOption[]> {
  return parseArray(await request<unknown>("/admin/content/missions"), "missions", (item, path) => {
    const mission = parseObject(item, path)
    return {
      id: parseString(mission.id, `${path}.id`),
      name: parseString(mission.title, `${path}.title`),
      daily: parseBoolean(mission.daily, `${path}.daily`),
    }
  })
}

export const useMissionOptions = () =>
  useQuery({
    queryKey: ["admin", "content", "missions"],
    queryFn: listMissionOptions,
    retry: false,
  })
