# WOG 4.5 原版內容資料

從 WOG 4.5 繁體釋出版的資料庫 SQL（`WOG4.5_sql_CT.rar` → `WOG4.5_sql_utf-8.sql`，2026-10-23 由使用者提供）整理出來的遊戲內容。SQL 本身不放進 repo；本目錄的 Markdown 與 `data/*.json` 就是記錄，`export.py` 可從 SQL 重新產生。欄位語意依 `docs/research/wog-research-*.md`，當初標為「資料表遺失」的數值以這裡為準。

## 匯入正式內容

本作的正式內容 `backend/content/*.json` 由 `scripts/import-wog45.py` 從本目錄的 `data/*.json` 產生（在 repo 根目錄執行 `python3 -I scripts/import-wog45.py`，可重跑）。原版有、但本作規則還不支援而沒有匯入的紀錄與欄位，以及保留的自訂內容，列在 [`gaps.md`](gaps.md)（由匯入腳本產生）。

## 檔案

| 文件 | 內容 | JSON |
|---|---|---|
| [`monsters.md`](monsters.md) | 怪物 464 隻，依地點分節：數值、必殺技、技能與發動率、掉落與機率、任務怪／NPC | `data/monsters.json` |
| [`items.md`](items.md) | 道具 3139 列（1461 種名稱），依類型分節；裝備含數值、需求、孔數、精鍊上限與變體 | `data/items.json` |
| [`stones-and-consumables.md`](stones-and-consumables.md) | 魔石 151 顆、精鍊石 180 顆的數值，使用型道具效果 71 項 | `data/magic-stones.json`、`data/refine-stones.json`、`data/consumable-effects.json` |
| [`jobs-and-skills.md`](jobs-and-skills.md) | 職業 12 種成長與轉職條件；技能 597 列依職業與等級 | `data/jobs.json`、`data/skills.json` |
| [`recipes.md`](recipes.md) | 合成配方 196 條 | `data/recipes.json` |
| [`missions.md`](missions.md) | 任務 345 個：條件、目標、獎勵、全文 | `data/missions.json` |
| [`shops-and-misc.md`](shops-and-misc.md) | 榮譽商店 142 項、地區鑰匙、傭兵、交易所物資、推薦等級 | `data/honour-shop.json`、`data/mercenaries.json` |
| － | 地點與子地區對照（名稱取自本作 `world.json`） | `data/places.json` |

## 數量總覽

- 怪物 464：21 個地點，任務怪 108、NPC 28，獨立圖檔 450 張，等級 0～943。
- 道具（不同名稱）：武器 278、頭部 104、身體 129、手部 122、腳部 110、道具 285、即時使用物 71、魔石 152、勳章 5、鑰匙 4、精鍊石 180、補給品 21。
- 職業 12、技能 88 種（597 列）、配方 196、任務 345（每日 8）、榮譽商店 142、傭兵 5。

## 沒有整理的表

公會（`wog_group_*`、`wog_build_*`、`wog_weapon_*`）、玩家與存檔（`wog_player*`、`wog_item`、`wog_skill_book`、`wog_stone_setup`、`wog_plus_setup`、`wog_pet`、`wog_message`、`wog_log_main`）、聊天與論壇。這些是執行期資料或確定不做的多人系統。

## 已知限制

- 怪物子地區用 `m_subplace` 位元遮罩還原；黑曜神廟（20）沒有子地區。
- 怪物技能欄 `skill_1～5` 以 `wog_ch_skill.s_id` 查名稱；查不到的顯示為「技能 id」。
- 任務文本中的 `rn` 是原資料的換行符殘留，已轉成換行。
- 精鍊石的「種類」1～18 只在 `wog_plus_list.d_type` 出現，名稱對照見精鍊石名稱前綴。
- `wog_area_lv` 的 id 與 `m_place` 編號不同，只當建議等級參考。

## 重新產生

```bash
python3 docs/research/wog45/export.py /path/to/WOG4.5_sql_utf-8.sql
```
