import { useQuery } from "@tanstack/react-query"
import { Select, Table, Tag } from "antd"
import { useState } from "react"
import { ContentListLayout, contentPageSize } from "../shared/ContentListLayout"
import {
  globalPlaceId,
  globalPlaceName,
  placeNameOf,
  usePlaceOptions,
} from "../shared/placeOptions"
import { matchesSearch } from "../shared/search"
import { monsterRepository } from "./monsterRepository"
import type { MonsterVo } from "./MonsterVo"

export function MonsterListPage() {
  const monsters = useQuery({
    queryKey: monsterRepository.queryKey,
    queryFn: monsterRepository.list,
    retry: false,
  })
  const places = usePlaceOptions()
  const [search, setSearch] = useState("")
  const [placeFilter, setPlaceFilter] = useState<number>()

  const placeList = places.data ?? []
  const rows = (monsters.data ?? []).filter(
    (monster) =>
      matchesSearch(monster, search) &&
      (placeFilter === undefined || monster.placeId === placeFilter),
  )

  return (
    <ContentListLayout
      title="怪物"
      search={search}
      onSearch={setSearch}
      error={monsters.error ?? places.error}
      filters={
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          aria-label="地區"
          placeholder="地區"
          style={{ width: 180 }}
          value={placeFilter}
          onChange={setPlaceFilter}
          options={[
            { value: globalPlaceId, label: globalPlaceName },
            ...placeList.map((place) => ({ value: place.id, label: place.name })),
          ]}
        />
      }
    >
      <Table<MonsterVo>
        rowKey="id"
        size="small"
        loading={monsters.isLoading}
        dataSource={rows}
        pagination={{ pageSize: contentPageSize, showSizeChanger: false }}
        columns={[
          { key: "id", title: "id", dataIndex: "id" },
          {
            key: "name",
            title: "名稱",
            render: (_, monster) => (
              <>
                {monster.name}
                {monster.isNpc && <Tag style={{ marginLeft: 8 }}>NPC</Tag>}
              </>
            ),
          },
          { key: "level", title: "等級", dataIndex: "level" },
          { key: "hitPoints", title: "HP", dataIndex: "hitPoints" },
          {
            key: "place",
            title: "地區",
            render: (_, monster) => placeNameOf(placeList, monster.placeId),
          },
        ]}
      />
    </ContentListLayout>
  )
}
