**Note:** I did not write the .md file. My role here is read-only and forbids creating files, so I am returning the full report in this message. Intended path was /private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-world.md. Please save it from this message. All paths below are relative to /Users/ethan/Project/WOG4.5_Releas_CT.

---

# WOG 4.5 單機化範圍盤點：世界、任務、寵物、傭兵、榮譽與其他系統

## 0. 先講三個和需求描述不同的發現

1. **`class/wog_etc_well.php` 不是休息或旅館。**
   - 它是登入頁的「冠軍展示」（`well_view`，讀 `wog_cp`，`wog_etc_well.php:19-65`）。
   - 休息旅館是 `class/wog_act_store.php:18` 的 `hotel()`。
2. **移動不是獨立系統。**
   - 沒有「旅行」動作、沒有旅費、沒有等級限制。
   - 「所在地」只是戰鬥表單選的 `act`（place id），戰鬥時寫入 `wog_player.p_place`（`class/wog_fight_m.php:192, 782`）。
3. **寵物攻擊傷害在這個版本被歸零。**
   - `fight_pet_dm` 算完傷害後，於 `class/wog_fight_m.php:434` 無條件執行 `$temp_d=0;`。
   - 寵物只剩「替玩家擋傷（`guard`，`wog_fight_m.php:943-962`）」和演出訊息。
   - 重製時要決定是否照抄。

---

## 1. 世界與地點

**是什麼**
- 21 個冒險地：`wog_act_config.php:195-202` 的 `$wog_arry["place"]`（id 1-21）。
- 4 個區域：`js/wog_data.js:36` 的 `temp_country`，即 中央大陸、魔法王國、熱帶雨林、末日王城。
- 玩家也有「出生地」`birth[1..4]`，對應同樣四個名稱（`js/wog_data.js:~112-117`）。

**區域與地點對應**（寫死在 `js/wog_fight.js:123-160` 的 `select_area()`）

| 區域 | 地點 id |
|---|---|
| 1 中央大陸 | 1 中央平原、3 黑暗沼澤、7 王者之路、13 水晶之間 |
| 2 魔法王國 | 2 試鍊洞窟、8 幻獸森林、12 天空之城、15 最果之島 |
| 3 熱帶雨林 | 4 迷霧森林、11 無淵洞窟、16 冷峰寒地、9 星河異界 |
| 4 末日王城 | 10 灼熱荒漠、5 古代遺跡、6 久遠戰場、14 失落古船、21 血之魔域 |
| 禁區（不屬於任何區域） | 17 廢棄洞窟、18 日沒碉堡、19 靜止之城、20 黑曜神廟（`js/wog_data.js:108-112` 的 `sp_sec`） |

- 每個地點有 4 個子地點，資料在 `js/wog_data.js:84-106` 的 `sec[]`。
- 例外：20 黑曜神廟沒有子地點。
- 子地點用位元遮罩篩怪：`m_subplace & (1<<(sub-1))`（`class/wog_fight_select.php:~135-139`）。

**核心規則**
- 玩家在戰鬥面板選「冒險修行」+ 區域 + 地點 + 子地點。
- 伺服器隨機抽怪：`select * from wog_monster where m_place in (act,0) and m_meet <= rand(0,100) ... ORDER BY RAND() LIMIT 1`（`wog_fight_select.php:~139-146`）。
  - `m_place=0` 代表全區通用怪。
  - 有任務時用 `m_mission` 欄位篩任務怪。
- 沒有旅費、沒有等級門檻。
  - `wog_area_lv`（min_lv, max_lv, area_name）只用在登入時「推薦地點」提示（`class/wog_act_chara.php:743`，`start_tips`）。
- 禁區（`a_type=3`）需要鑰匙：
  - 流程是 `wog_fight.php:238-282` 的 `key_check` 與 `key_use`。
  - 用 `wog_key_main(area_id,d_id,area_time)` 查鑰匙道具，扣一把後寫入 `wog_key_list(p_id,area_id,area_time)`，到期前可免費重進。
- 戰鬥節奏相關設定（`wog_act_config.php`）：
  - `f_time=2`：戰鬥間隔。
  - `f_count=40`：回合上限。
  - `act_num`：行動力，每 180 秒補 6 點，不足時報酬 ×0.5（`f_down`）。
- 旅館：`class/wog_act_store.php:18-86`。
  - 費用 = `hotel_money(10) × 等級 × 係數`。
  - 係數：死亡 2.1、HP 低於 10% 為 1.8、其餘為 1.4×損血比例。
  - 只有等級大於 100 才收費。付不起就扣 20% 經驗。
  - 等待 5 秒，死亡時 15 秒（`hotel_time`、`hotel_die_time`）。

**資料表**
- `wog_monster`：欄位 `m_id, m_name, m_place, m_subplace, m_meet, m_mission, m_npc, m_topr, d_id, at/df/mat/mdf/hp/sp…`。
- `wog_area_lv`、`wog_key_main`、`wog_key_list`。
- 玩家 `wog_player.p_place`。

**單機相關？** 是，核心。禁區和鑰匙也是單機內容。

**預估規模**
- 地點選單與怪物篩選：S。
- 怪物資料：資料量大，SQL 遺失，需要另外重建。
- 禁區鑰匙：S。
- 旅館：S。

---

## 2. 任務

**是什麼**
- 一般任務由 DB 的 `wog_mission_main` 驅動，`mission/` 資料夾裡的 PHP 不是主線腳本。
- `mission/wog_mission_{222,223,224,225,271,272,273,274}.php` 共 8 個，全部只有 `mission_start()`，`mission_body` 與 `mission_end` 是空函式。
  - 它們是「每日重複任務」的隨機模板，用 `mission_type` 區分：
    - type 1（222、224）：獎勵為榮譽道具，殺怪組合隨機（`mission/wog_mission_222.php:14-96`）。
    - type 2（223、225）：交寵物（`m_pet_id`）。
    - type 3（271-274）：殺怪 + 交易所資源 `ex_id/ex_num`（`wog_mission_271.php:14-19`）。
- 主線任務的內容（任務文字、需求、獎勵）全部在 DB，SQL 遺失，需重新設計或重建。

**核心規則**
- 同時進行上限：`mission_get_num=3`（`wog_act_config.php:215`；檢查在 `class/wog_act_mission.php:241-243`，數的是 `m_status<2`）。
- 狀態 `wog_mission_book.m_status`：0 進行中、1 條件達成待交、2 已完成、3 另一種完成（`mission_list` 判斷前置任務用 `m_status=2 or 3`，`wog_act_mission.php:27-42`）。
- 可接任務條件（`wog_act_mission.php:37-42`，也見 `wog_act_chara.php:709-716` 的 `start_tips`）：
  - 職業 `m_job` 符合或為 99（通用）。
  - 等級 `m_lv ≤ 玩家等級`。
  - 性別 `m_sex`（3=不限）。
  - 出生地 `m_birth`。
  - 前置任務 `m_need_id`、排斥任務 `m_not_id`。
- 殺怪計數：
  - `m_monster_id` 格式為 `"怪id*數量,怪id*數量"`，進度存在 `wog_mission_book.m_kill_num`。
  - 戰鬥勝利時遞減（`class/wog_fight_select.php:~160-200, 760-840`），全部歸零就把狀態設為 1。
  - 戰鬥也會用 `wog_monster.m_mission/m_npc` 讓任務專屬怪出現。
- 交付與驗證（`class/wog_mission_tool.php:17-59`）：
  - 狀態必須為 1，或殺怪數為 0。
  - 需求物品（`n_a_id, n_d_body_id, n_d_foot_id, n_d_hand_id, n_d_head_id, n_d_item_id, n_d_stone_id, n_d_honor_id, n_d_plus_id`）要扣。
  - 技能需求 `n_skill_id` 要檢查 `wog_ch_skill`。
  - 寵物需求 `m_pet_id` 要有對應寵物並扣掉。
- 獎勵（`wog_act_mission.php:335-393` 與 `wog_mission_tool.php`）：
  - 物品：`g_*` 欄位，道具格式 `id*數量`。
  - 寵物：`g_pet_id/g_pet_lv`（`mission_pet_get`，`wog_mission_tool.php:61-78`）。
  - 技能：`g_skill_id`（`mission_skill`，`wog_mission_tool.php:97`）。
  - 金錢：`g_money`。
  - 交易所資源：`ex_id/ex_num`（`mission_ex_get`，`wog_mission_tool.php:79`）。
  - 經驗固定為 `m_lv × 190`（`mission_status_update`，`wog_mission_tool.php:208`）。
- 重複任務：
  - `wog_mission_repeat(m_id, update_time, repeat_type, reward_type)`。
  - 每天 09:00 重置（`updateRepeatMission`，`wog_act_mission.php:395-437`）。
  - 重置時從模板陣列隨機抽 `repeat_type` 和 `reward_type`，覆寫 `wog_mission_main` 的怪物與獎勵欄位。
  - 到期時把 `wog_mission_book` 的紀錄刪掉，可以重接（`wog_act_mission.php:26`）。

**資料表**
- `wog_mission_main`：m_id, m_name, m_subject, m_body, m_lv, m_job, m_sex, m_birth, m_need_id, m_not_id, m_repeat, m_monster_id, m_pet_id, m_npc_message, m_end_message, n_*, g_*, ex_id, ex_num, g_money, g_pet_id, g_pet_lv, g_skill_id, n_skill_id。
- `wog_mission_book(p_id, m_id, m_status, m_kill_num)`。
- `wog_mission_repeat`。

**單機相關？** 是，核心。
- 公會任務 `group_mission/wog_gmission_1..4.php` 與 `class/wog_group_mission.php` 屬於多人，不做。

**預估規模**
- 任務引擎（接取、進度、交付、獎勵、前置鏈）：M。
- 每日隨機任務：S。
- 任務內容：資料量很大，要自己編寫。

---

## 3. 寵物

**是什麼**：玩家最多養 3 隻（`pe_st in (0,2)` 的數量小於 3，`wog_fight_select.php:291-293`；`wog_act_arm.php` 的寵物蛋同理）。

**取得方式**（都寫入 `wog_pet`，初始 `pe_fu=80, pe_hu=0`）
- 戰鬥捕捉（`wog_fight_select.php:273-310`）：
  - 副手裝備捕捉道具（id 258 / 1837 / 2236）且目前沒帶寵物時，勝利有 12% + `get_pet` 機率抓到所打的怪。
  - 道具有 1/40 機率損耗。
  - 初始能力 `rand(1,10)×等級倍率`，倍率為 1 / 8 / 13（`class/wog_fight_event.php:271-290` 的 `pet_stats`）。
  - 抓到的 `pe_st=2`（可以先放著）。
- 寵物蛋（道具 case 10，`class/wog_act_arm.php:1198-1220`）：隨機一隻怪，倍率 280，因此能力非常高。
- 任務獎勵（`wog_mission_tool.php:61-78`）。
- 拍賣：`pet_sale`（`class/wog_act_pet.php:29-68`）與 `pe_st=1` 的上架狀態，屬多人，不做。

**餵食與訓練**（`class/wog_act_pet.php:106-286`）
- 一般餵食：
  - 小：+2 能力，花 20 元，飽食度 +10~20。
  - 大：+4 能力，花 50 元，飽食度 +18~28。
- 特殊道具 2319-2322：+8 能力，不收錢，扣道具，並寫入 `wog_vip_log`。
- 能力選項有 4 種：at、mt、def、fi（fi 為戰意或個性值）。
- 限制：
  - 寵物能力不可超過主人對應能力（`pe_at > player.at` 就拒絕）。
  - 兩次餵食間隔 `pet_ac_time=10` 秒。
  - 飽食度 `pe_fu ≥ 100` 時不能再餵。
  - 每次餵食 `pe_he`（健康）+2~6。
- `pet_eat_money=20` 在設定檔存在，但程式把金額寫死在 case 裡，沒有引用設定值。
- 「特訓」`pet_ac`（`wog_act_pet.php:406-478`）整段被註解掉，不用做。

**飢餓與老化**
- 飢餓：登入維持呼叫 `wog_etc_peo.php:20-38`，超過 `pet_fu_time×60 = 900` 秒就 `pe_he -1`、`pe_fu -rand(8,12)`，下限 0。
- 老化：`pe_b_old = round((now - pe_b_dateline)/pet_age)`，`pet_age=864000`（10 天一歲）（`wog_act_pet.php:300`）。
  - 5 歲以上會把 `p_send` 設為 1，代表不能再轉讓或拍賣。
  - 死亡機制（15 歲以上機率死亡）在 `wog_act_pet.php:~301-312` 被註解掉，即沒有自然死亡。

**戰鬥貢獻**（`class/wog_fight_m.php`）
- 個性 `pe_type`：1 攻擊 ×1.2、2 魔法 ×1.2、3 防禦 ×1.2、4 `pe_fi` ×1.5（`wog_fight_m.php:503-519`）。
- 出擊率 `petact = pe_he/255×10 + pe_fi/255×10 + round(玩家au/怪au)`，上限 35%（`wog_fight_m.php:520-525`）。
- 出擊（`wog_fight_m.php:674-697`）：
  - `rand(1,100) < petact` 且 `pe_def>0` 才出手。
  - `fight_pet_dm`（`wog_fight_m.php:405-445`）中有 1/3 機率落空，爆擊看 `(au+agi)` 比值，上限 ×2。
  - 但 line 434 的 `$temp_d=0` 讓傷害實際為 0。
- 擋傷（`guard()`，`wog_fight_m.php:943-962`）：
  - 以 `petact` 為機率，用 `pe_def` 抵擋傷害。
  - `pe_def` 被扣光寵物就消失（`$pet=null`）。
  - 這個消失只影響本次戰鬥，DB 內 `pe_def` 沒有被寫回，但程式碼細節值得重製時再確認。

**寵物競技**：`pet_f_time=28800`（`wog_act_config.php:209`）在整個程式中沒有任何引用，競技功能在此版本不存在。

**資料表**：`wog_pet`（pe_id, pe_p_id, pe_name, pe_at, pe_mt, pe_def, pe_fi, pe_fu, pe_hu, pe_he, pe_type, pe_age, pe_b_old, pe_b_dateline, pe_dateline, pe_m_id, pe_mname, pe_mimg, pe_img_set/url, pe_st, pe_money, pe_s_dateline, p_send）。

**單機相關？** 是，中優先。
- 拍賣與競技不做。
- UI 在 `js/wog_pet.js`（132 行）。

**預估規模**：M（養成 + 戰鬥整合）。

---

## 4. 傭兵

**是什麼**：玩家花錢買一個「自動掛機戰鬥」的傭兵，同時只能有一個（`wog_mercenary_list` 每人一筆，`class/wog_act_mercenary.php:76-79`）。

**核心規則**
- 買傭兵：
  - `mercenary_buy`（`wog_act_mercenary.php:63-100`）扣 `me_money`，寫入 `wog_mercenary_list(p_id, me_id, me_count, me_status=0, me_name)`。
  - 商品來自 `wog_mercenary_main where me_status=0`，欄位：
    - `me_money`：價格。
    - `me_count`：可用次數。
    - `me_get_money`：金錢收益百分比。
    - `me_get_item`：掉寶率加成。
    - `me_get_exp`：經驗百分比。
    - `me_die`：戰敗時扣的次數。
- 狀態（`mercenary_set`，`wog_act_mercenary.php:101-140`）：
  - `me_status`：0 待命，1 出擊（指定 `me_place`），2 解僱，解僱會刪除紀錄與戰報。
  - 可改名，名稱長度上限 24 bytes。
- 戰鬥（`wog_etc_mercenary.php:17-129` 與 `class/wog_fight_select.php:711-880` 的 `fire_mercenary`）：
  - 由前端輪詢呼叫，間隔 `f_time_mercenary=5` 秒。
  - 用玩家自己的能力，在 `me_place` 打一隻怪，但「不消耗行動力」（`wog_arry["f_down"]=1`）。
  - 收益：金錢 × `me_get_money/100`、經驗 × `me_get_exp/100`（`wog_fight_m.php:137, 144`）。
  - 掉落物用 `mercenary_topr = me_get_item`（`wog_fight_event.php:40-42`）。
  - 玩家寵物也會參戰。
  - 勝利 `me_count -1`，並推進任務殺怪計數；落敗 `me_count -= me_die`。
  - `me_count<=0` 時發信通知、刪除傭兵。
  - 戰報存 `wog_mercenary_book`，只保留 1 小時內的資料（`wog_fight_select.php:859`）。
- 榮譽商店也能兌換傭兵（`wog_honor_main.h_type=1`，`class/wog_act_honor.php:~24-28, 105-125`）。

**資料表**：`wog_mercenary_main`、`wog_mercenary_list`、`wog_mercenary_book`。

**單機相關？** 是，中優先（離線掛機的取代機制）。

**預估規模**：S 到 M。若做成離線補算，建議改為「依時間差一次結算」，不用輪詢。

---

## 5. 榮譽、冠軍與英雄榜

**榮譽商店**（`class/wog_act_honor.php`，186 行）
- 榮譽道具（`wog_item.d_honor_id`）是貨幣。
  - 來源是任務獎勵、Boss 和英雄 NPC 掉落（例如 `mission/wog_mission_222.php` 給 1304-1307）。
- `wog_honor_main`：`h_id, h_type, item_id, item_num, h_1..h_5`。
  - `h_1..h_5` 代表 5 種榮譽道具各需要幾個。
- `h_type`：
  - 1 傭兵
  - 2 鑰匙
  - 3-7 裝備（arm_1..arm_5）
  - 8 道具
- **單機相關**，中優先。
- 規模：S 到 M，主要是商品資料。

**冠軍（`wog_cp`）**
- 設定：`cp_mmoney=500` 挑戰費，`f_cp=604800`（挑戰成功者 7 天內不能再挑，記在 `wog_player.p_cp_time`）。
- 流程（`class/wog_fight_select.php:407-535`）：
  - `wog_cp` 只有一筆，是「當前冠軍」的完整能力快照，包含 `p_pid`（真人 id）。
  - 挑戰者付 500 元打冠軍快照。
  - 贏了就用自己的能力覆蓋 `wog_cp`。
  - 輸了的話冠軍保留，並給冠軍擁有者 70% 挑戰費（`wog_fight_select.php:509`）。
  - `wog_cp` 沒資料時，第一個挑戰者直接成為冠軍（`:513-527`）。
- 登入畫面用 `wog_etc_well.php` 顯示冠軍。
- **多人色彩重，但單機可保留**：
  - 作法是改成 NPC 冠軍，初始用固定數值，被打敗後換成「玩家的歷史快照」或自己的複製人。
  - 規模 S 到 M。

**英雄榜 `wog_etc_hero.php`、`wog_player_cp`**
- `wog_player_cp(p_id, hero_npc, hero_type, hero_time, p_cp_st)`。
- 英雄 NPC 是 `wog_player` 裡 `p_npc>1` 的角色，`p_pk_s=1`，所以可被 PK 挑戰。
  - 英雄類型 `p_npc` 為 2-5。
  - 登入時隨機重設出現地點 `rand(1,20)`（`class/wog_act_chara.php:362-366`）。
  - 資格：玩家自己不是英雄、等級 ≥ 200（`wog_fight_select.php:~604-606`）。
- 戰鬥（`wog_fight_select.php:604-690`）：
  - NPC 能力是挑戰者的鏡像（at/mat 取平均、df/mdf 取平均、agi/au/屬性/HP 同玩家，技能也複製）。
  - 贏了，玩家掛上英雄稱號（`hero_type`, `hero_time = now + rand(3~4 天)`），獲得專屬掉落（如 283/1842, 285/1844）。
  - 3-4 天後（`f_hero`）稱號到期，NPC 復位（`wog_act_chara.php:360-370`）。
- **單機相關**：鏡像戰概念是單機友善的。榜單頁面只剩自己一人。中到晚優先，規模 S。

**`wog_etc_king.php`**：五個排行榜（勝場、HP、攻、魔、敏，各取前 5）。多人專屬，單機只剩自己，不做或僅做個人紀錄。

---

## 6. 其他單機相關系統

| 系統 | 位置 | 建議 |
|---|---|---|
| 旅館/休息 | `class/wog_act_store.php:18-86` | 保留，S（見 §1） |
| 銀行 | `wog_act_store.php:88-144`，`wog_player.p_bank` | 保留，S |
| 改圖像 | `wog_act_store.php:167-193`，`chang_face=1000` | 保留，S |
| 變性 | `wog_act_store.php:145-166`，`chang_sex=1000000` | 保留，S；性別影響任務條件 `m_sex` |
| 商店/裝備/打洞/合成/強化 | `wog_act_shop.php`、`wog_act_arm.php`、`wog_act_hole.php`、`wog_act_syn.php`、`wog_act_plus.php`、`wog_act_skill.php`、`wog_act_job.php` | 不在本次範圍，但屬核心 |
| 商城 | `wog_act_mall.php`（37 行） | 只剩空殼，捨棄 |
| 查看對手 | `wog_act_chara.php:964` 的 `system_view2`，`view2_money=10` | 多人用途，不做 |
| 好友名單 | `wog_act_friend.php`（79 行，`wog_friend_list`） | 捨棄 |
| 信箱 | `wog_act_message.php`（`wog_message`），`message_box=30` 天、`retime=10` 天 | 系統信（傭兵耗盡通知、物品補發）有用，可縮減成系統通知；玩家互寄捨棄 |
| 推薦 | `wog_act_recomm.php`（36 行） | 全部是 `alertWindowMsg(wog_main_error4)` 的空殼，捨棄 |
| SNS | `wog_act_sns.php`（27 行） | 同樣是空殼，捨棄 |
| 驗證碼 | `wog_etc_confirm.php`（24 行，PhpCaptcha） | 捨棄 |
| 隨機問答防外掛 | `wog_arry["event_ans"]`，`wog_fight_select.php:~46-55` | 捨棄，單機不需要 |
| 交易所資源 | 只有任務會用到 `ex_id/ex_num` | 若不做交易所，任務獎勵要換成其他貨幣 |
| 登入、註冊、密碼、刪角 | `wog_act_chara.php`（1084 行） | 單機只需存檔管理 |
| 年齡 | `player_age=30` 天 1 歲 | 可保留作為展示欄位 |

---

## 7. 多人專屬，確認不做

| 系統 | 位置 | 理由 |
|---|---|---|
| 公會 | `class/wog_act_group.php`（2598 行）、`class/wog_fight_group.php`（1434 行）、`class/wog_group_skill.php` | 設定檔 `g_*` 全是公會戰/據點/兵種/資源，多人專屬 |
| 公會任務 | `group_mission/wog_gmission_1..4.php`、`class/wog_group_mission.php`（131 行） | 以 `g_id` 為單位的團體進度，多人專屬 |
| 拍賣/收購 | `class/wog_act_bid.php`（863 行）、`sale_day`、`bid2_money` | 玩家對玩家，多人專屬 |
| PK | `class/wog_act_pk.php`（54 行）、`fire_pk` | 玩家對玩家。例外：英雄 NPC 也走 `fire_pk` 的通道，單機要另外抽出 NPC 戰 |
| 組隊 | `class/wog_act_team.php`（356 行）、`t_peo=5`、`t_time=3` | 多人支援與經驗分享 |
| 聊天室 | `wog_chat/`、`wog_arry["chat_path"]` | 多人專屬 |
| 交易所 | `class/wog_act_exchange.php`（187 行）、`exchange_up`、`exchange_money`、`wog_exchange_main` | 資源是全伺服器共用的浮動價格市場，多人專屬。`wog_fight_select.php:~241-250` 戰鬥後觸發漲跌事件，也要一起移除 |
| 在線名單、king 榜 | `wog_etc_peo.php`、`wog_etc_king.php` | 多人專屬 |
| 論壇整合 | `wog_act_config.php:13-78`、`forum_support/` | 與遊戲邏輯無關 |

---

## 8. 建議實作順序

**A. 單機核心循環（先做）**
1. 角色與戰鬥引擎 `wog_fight_m.php`（含技能），這是所有其他系統的底座。
2. 世界與地點：21 地點、4 區域、子地點篩怪、`m_meet`、行動力。
3. 旅館與銀行。
4. 任務引擎：接取、殺怪計數、交付、獎勵、前置鏈、每日重複。任務內容需要自己重建。
5. 禁區與鑰匙（17-20），可放在 A 的後段。

**B. 中優先**
1. 寵物：捕捉、餵食、飢餓、擋傷。先決定 `fight_pet_dm` 的傷害要不要恢復。
2. 傭兵：改成離線結算。
3. 榮譽商店：配合任務與英雄掉落。
4. 改圖像、變性、信箱系統通知。
5. 冠軍：改為 NPC 冠軍。

**C. 後期**
1. 英雄榜與英雄 NPC 鏡像戰。
2. 排行榜，只做個人紀錄。
3. 年齡等展示功能。

**不做**：公會、公會任務、拍賣、PK、組隊、聊天、交易所、好友、推薦、SNS、驗證碼、隨機問答、論壇整合。

**資料風險**：SQL 遺失，以下資料表的內容都得重建，這是最大的工作量。
- `wog_monster`、`wog_df`（道具）、`wog_mission_main`。
- `wog_honor_main`、`wog_mercenary_main`、`wog_area_lv`、`wog_key_main`。
- 技能與職業相關表。