# 研究文件索引

三類文件：`wog-research-*.md` 是從 WOG 4.5 原始碼（`WOG4.5_Releas_CT`）推出的規則研究，是設計文件與各計畫的依據；[`wog45/`](wog45/README.md) 是 2026-10-23 找回的 WOG 4.5 資料庫 SQL 整理出的原版內容總表（怪物、道具、技能、任務等，附 JSON），研究報告裡標「資料表遺失／未知」的數值以它為準；其餘是本作的內容總表，記錄自訂數值與測試釘住的規則，改內容檔時要一起改。

WOG 路徑以原始碼根目錄為準；研究報告內用的縮寫（`FM`、`ARM`、`CFG` 等）在各報告開頭定義。四份研究報告開頭有一段研究代理留下的「沒有寫檔」說明，可以忽略，正文從標題開始。

| 文件 | 範圍 | 對應的 WOG 檔案 | 用到的計畫 |
|---|---|---|---|
| [`wog-research-stats-jobs.md`](wog-research-stats-jobs.md) | 角色欄位、七項能力與衍生值、創角、升級與經驗金錢、職業與轉職 | `class/wog_act_chara.php`、`class/wog_act_job.php`、`class/wog_fight_m.php`（升級）、`wog_act_config.php` | 3、4、5 |
| [`wog-research-battle.md`](wog-research-battle.md) | 戰鬥流程、怪物欄位、技能系統、掉落、隨機事件、疑似 bug 清單 | `class/wog_fight_m.php`、`class/wog_fight_select.php`、`class/wog_fight_skill.php`、`class/wog_fight_event.php`、`wog_fight.php`、`wog_act_config.php` | 4、5、6、7 |
| [`wog-research-items.md`](wog-research-items.md) | 道具分類、裝備、精鍊與鑲嵌、商店與經濟、背包與道具使用 | `class/wog_act_arm.php`、`forum_support/function.php`、`wog_act_config.php`、`shop.php`、`js/wog_shop.js`、`js/wog_data.js` | 7、10 |
| [`wog-research-world.md`](wog-research-world.md) | 世界與地點、任務、寵物、傭兵、榮譽與冠軍、其他單機系統、確認不做的多人系統 | `class/wog_fight_m.php`、`class/wog_fight_select.php`、`class/wog_act_store.php`、`class/wog_act_pet.php`、`class/wog_act_mission.php`、`mission/wog_mission_tool.php`、`class/wog_act_honor.php`、`class/wog_etc_well.php` | 6、8、9、10 |
| [`job-growth-expectation.md`](job-growth-expectation.md) | 各職業每級成長範圍與 Lv100 期望值、轉職門檻對照 | 本作 `backend/content/jobs.json`（WOG `wog_character` 遺失） | 5 |
| [`monster-roster.md`](monster-roster.md) | 怪物生成公式與倍率、校準、掉落、勳章掉落、新手安全規則、任務怪、全部怪物總表 | 本作 `backend/content/monsters.json`；公式依 `class/wog_fight_m.php` 的傷害公式校準 | 6、7、8、9、10 |
| [`item-table.md`](item-table.md) | 裝備公式與全部道具、精鍊石、魔石、材料、勳章、鑰匙、寶箱與書、密藥、配方 | 本作 `backend/content/items.json`、`recipes.json`（WOG `wog_df` 數值遺失） | 7、9、10 |
| [`mission-table.md`](mission-table.md) | 任務規則、一般任務、每日任務、任務怪、與研究報告的差異 | 本作 `backend/content/missions.json`；每日任務移植自 `mission/wog_mission_{222～225,271～274}.php` | 8、9 |
| [`companion-table.md`](companion-table.md) | 勳章、捕捉道具、餵食選單、傭兵目錄、勳章所、冠軍種子與其載入驗證 | 本作 `mercenaries.json`、`honour-shop.json`、`champion.json`、`items.json`（WOG 傭兵、勳章所、冠軍資料遺失） | 9、10 |
