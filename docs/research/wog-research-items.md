**重要說明：我沒有寫入報告檔。** 我的角色是唯讀（沒有 Write 工具，也禁止用 Bash 重導向寫檔），所以 `/private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-items.md` 不存在。完整報告（繁體中文）直接附在下方，請由你存檔。

路徑縮寫：根目錄 `/Users/ethan/Project/WOG4.5_Releas_CT`。`FN` = `forum_support/function.php`，`ARM` = `class/wog_act_arm.php`，`CFG` = `wog_act_config.php`（注意是根目錄，不在 class/）。

# WOG 4.5 單機道具經濟規則研究

## 0. 先釐清的結構事實

- **SQL dump 缺失。** 欄位語意都是從程式碼推論。
- **道具主檔是 `wog_df`。** 欄位 `d_id`、`d_name`、`d_type`、`d_fie`、`d_money`、`d_lv`、`d_dbst`、`d_send`、`d_hole`、`d_plus` 等（推論，ARM:461、shop:75）。
- **背包不是逐筆資料列。** 每位玩家在 `wog_item` 只有一列，每個分類一個欄位，內容是逗號分隔字串。
  - 欄位：`a_id`（武器）、`d_head_id`、`d_body_id`、`d_hand_id`、`d_foot_id`、`d_item_id`、`d_stone_id`、`d_honor_id`、`d_key_id`、`d_plus_id`（建立角色時全為空字串，chara:77）。
  - 可堆疊類（道具、魔石、勳章、鑰匙、精鍊石）寫成 `id*數量`。
  - 裝備類每件一格，寫成 `id[:鑲嵌ID][&精鍊ID]`（`get_arm_id`，FN:651）。冒號後是 `wog_stone_setup.hs_id`，`&` 後是 `wog_plus_setup.ps_id`。
- **「背包容量」算的是字串元素數**，不是總件數。

## 1. 道具（`wog_df`）與分類

### 1.1 分類對照（`type_name`，FN:122；`check_type`，FN:105）

`check_type` 先查 `wog_df.d_fie`，再呼叫 `type_name` 取得欄位名。

| `d_fie`／`d_type` | 欄位 | 意義 | 背包 UI 分頁 |
|---|---|---|---|
| 0 | `a_id` | 武器 | 武器 |
| 1 | `d_head_id` | 頭部 | 頭部 |
| 2 | `d_body_id` | 身體 | 身體 |
| 3 | `d_hand_id` | 手部 | 手部 |
| 4 | `d_foot_id` | 腳部 | 腳部 |
| 5、6、"5,6" | `d_item_id` | 道具類（消耗品、材料、密藥、寵物蛋等） | 道具 |
| 11 | `d_item_id2` | 第二道具欄（HP/SP 補給品）。背包欄位仍是 `d_item_id`（unsetup 時 11 轉 5，ARM:630 附近） | （併入道具） |
| 7 | `d_stone_id` | 魔石（鑲嵌用） | 魔石 |
| 8 | `d_honor_id` | 勳章 | 勳章 |
| 9 | `d_key_id` | 鑰匙 | 鑰匙 |
| 10 | `d_plus_id` | 精鍊石 | 精鍊石 |

UI 分頁見 `js/wog_item.js:420-428`。

### 1.2 `wog_df` 欄位語意

| 欄位 | 語意 | 證據 |
|---|---|---|
| `d_at`、`d_mat` | 物攻、魔攻（裝備加成） | ARM:110、129 |
| `d_df`、`d_mdf` | 物防、魔防 | ARM:110 |
| `d_g_str`、`d_g_smart`、`d_g_agi`、`d_g_life`、`d_g_vit`、`d_g_au`、`d_g_be` | 裝備提供的能力值（力、智、敏、生命、體質、魅力、信仰） | ARM:111、FN:317 |
| `d_g_hp`、`d_g_sp` | 補給品每次恢復的 HP、SP | ARM:461、fight_select:90 |
| `d_mstr`、`d_msmart`、`d_magl`、`d_mau` | 裝備需求（力、智、敏、魅），比對玩家**基礎**能力 | ARM:463 |
| `ch_id` | 職業限制，0 表示不限 | ARM:463 |
| `d_s` | 元素屬性：0 無、1 地、2 水、3 火、4 木、5 風、6 毒 | js/wog_data.js:119-127 |
| `d_hole` | 孔數 | hole.php:36、hole.php:530-537 |
| `d_plus` | 精鍊上限等級 | plus.php:423 |
| `d_money` | 商店售價；賣給 NPC 時取 50% | shop.php:75、ARM:729-760 |
| `d_lv` | 商店等級 1~7（也用於寶箱抽選） | shop.php:40、js/wog_data.js:235 |
| `d_dbst` | 0 才會進商店並可用於合成；非 0 為商店禁售（推論） | shop.php:65、syn.php:92 |
| `d_send` | 1 表示綁定，不可贈送、轉倉、交易 | ARM:700 附近、ARM:1560 附近 |
| `skill`、`stime` | 裝備附帶技能（主武器） | ARM:116、fight_select:90 |
| `d_vip` | 取出但未使用 | ARM:461 |

- 屬性相剋：`s_check` 差值 -1 或 5 乘 1.2，差值 1 或 -5 乘 0.8（wog_fight_m.php:899-919）。
- 消耗品效果表是 `wog_df_used`（ARM:1015），見第 5 節。

### 1.3 稀有度與品質

- 程式碼中沒有 rarity 或 quality 欄位。
- 只有 `d_lv`（1~7）、`d_dbst`、`d_hole`（孔數）、`d_plus`（精鍊上限）。「稀有」由掉落機率和商店禁售決定，**未知**（資料表缺失）。
- 沒有等級需求（沒有 `d_lv` 對 `p_lv` 的判斷，ARM:463），也沒有耐久度。
- 「耐久」只出現在公會據點（`g_durable`，CFG:153、155）。**個人裝備無耐久、無修理**。

## 2. 裝備

### 2.1 `wog_player_arm` 欄位

| 欄位 | 意義 |
|---|---|
| `a_id`、`d_head_id`、`d_body_id`、`d_hand_id`、`d_foot_id` | 目前穿著的武器、頭、身、手、腳（`wog_df.d_id`） |
| `d_item_id`、`d_item_num` | 道具欄 1 及其數量 |
| `d_item_id2`、`d_item_num2` | 道具欄 2（HP/SP 補給）及其數量 |
| `s_a_id`、`s_head_id`、`s_body_id`、`s_hand_id`、`s_foot_id`（`s_item_id` 也有定義） | **s 系列**：該部位穿著裝備的鑲嵌設定 ID（`wog_stone_setup.hs_id`） |
| `p_a_id`、`p_head_id`、`p_body_id`、`p_hand_id`、`p_foot_id`（`p_item_id` 也有定義） | **p 系列**：該部位的精鍊設定 ID（`wog_plus_setup.ps_id`） |

- s/p 對應表在 `get_type_name`（FN:686-722）。
- `s_item_id`、`p_item_id` 實務上不會被道具欄寫入有效值，`check_status` 也不讀取。

### 2.2 穿脫規則

**穿上（`arm_setup`，ARM:437-557）**

- 先在 `wog_df` 與 `wog_player` 做 JOIN，條件為：
  - `(ch_id = 玩家ch_id 或 ch_id = 0)`
  - `base_agi >= d_magl`、`base_str >= d_mstr`、`base_smart >= d_msmart`、`base_au >= d_mau`
  - 不符合就回「能力不足,無法裝備」（ARM:463、ARM:547 附近）。
- 比對的是**基礎能力 `base_*`**，不含裝備與魔石。**沒有等級需求**。
- 若 `d_type==6`（即時使用物）：從背包扣 1 個並呼叫 `arm_item_use`，不佔裝備欄（ARM:467-483）。
- 一般裝備：背包裡同欄位的該件移除，原本穿著的放回背包（`arm_equit`，ARM:1414）。同時把 `s_*`、`p_*` 寫入 `wog_player_arm`（ARM:534）。
- 道具類（`d_item_id`、`d_item_id2`）：
  - `d_type==11` 寫入欄 2，其餘寫入欄 1（ARM:494-500）。
  - 數量由使用者填，必須在 1~99（ARM:491）。
  - 原本裝備的同欄位物品（含數量）退回背包，並合併堆疊（`arm_item`，ARM:1361-1412）。
- 穿完呼叫 `check_arm_status` 重算能力。

**脫下（`arm_unsetup`，ARM:559-640）**

- 欄位清 0，鑲嵌與精鍊 ID 一併清 0。
- 放回背包：裝備用 `item_in` 加 1 格；道具用原數量堆疊。

### 2.3 能力計算（`check_status`，FN:235-422）

| 項目 | 規則 | 行 |
|---|---|---|
| 裝備加成來源 | 取 `a_id`、頭、身、手、腳、`d_item_id` 的 `wog_df` 數值加總（**不含 `d_item_id2`**），再加魔石（`wog_stone_list` 加 `wog_stone_temp`）與精鍊（`wog_plus_setup`） | FN:236-355 |
| 七項能力 | `agi`、`life`、`au`、`be`、`vit`、`smart`、`str` = 魔石 + 精鍊 + 裝備 + `wog_player.base_*` | FN:380-389 |
| `at` | 裝備 + 魔石 + 精鍊 的 `at` 再加 `str`（`$at+=$str`） | FN:391 |
| `mat` | `mat` 加 `smart` | FN:392 |
| `df` | `df` 加 `vit` | FN:393 |
| `mdf` | `mdf` 加 `be` | FN:394 |
| `hpmax` | `round(life*25)+50` 再加魔石的 `s_hpmax` 總和 | FN:426-428、FN:396 |
| `spmax` | `round((smart+be)*3)+25`（傳入 `smart+be`，`smart` 已含裝備） | FN:434-436、FN:397 |
| 密藥 buff | 對 `at`、`mat`、`df`、`mdf`、`hpmax`、`spmax` 做**乘算**（`wog_player_buffer` 欄位值，需 `end_time>now`） | FN:398-410 |

- `hp`、`sp` 不會自動被 clamp（本函式只寫 `hpmax`、`spmax`）。
- 魔石中 `d_class<99` 取 `wog_stone_list` 固定值；`d_class=99` 取 `wog_stone_temp` 的擲骰值（見第 3.2 節）。

### 2.4 角色初始值（chara.php:62-80）

| 項目 | 值 |
|---|---|
| 初始金錢 | 2000 |
| 初始 hp/hpmax | 50 |
| 初始 sp/spmax | 25 |
| 初始 `base_*` | 各項 +10（`str`、`life`、`smart`、`agi` 另加 `total_point=10` 的自由分配） |
| 起始背包 | 全空，建立 `wog_player_arm` 空列 |

## 3. 強化

### 3.1 精鍊（`class/wog_act_plus.php`）

- 精鍊石是 `d_type=10` 的道具，存於 `d_plus_id`。屬性定義在 `wog_plus_list`。
- `wog_plus_list` 欄位：`d_id`、`d_lv`（1~10）、`d_type`（種類 1~18）、`d_at`、`d_mat`、`d_df`、`d_mdf`、`d_str`、`d_agi`、`d_smart`、`d_life`、`d_vit`、`d_au`、`d_be`。能力欄為 `"min,max"` 字串（plus.php:455-530）。

| 功能 | 規則 | 成本／機率 | 行 |
|---|---|---|---|
| 精鍊石合成 `plus_make` | 一次投入兩顆同等級石，數量 1~49 組 | **無金錢成本**，兩個來源各扣 `item_num` 顆 | plus.php:49-205 |
| 同類（`d_type` 相同）合成 | 產生同類 +1 級石，最高 LV10 | 必成功 | plus.php:88-97 |
| 異類合成 | 產出 -1 級石（LV1 不可異類）。`d_type` 區間：1~7 佔 50%、8~11 佔 30%、12~15 佔 15%、16~18 佔 5%；排除兩個來源的類型 | 逐顆獨立擲骰 | plus.php:100-137 |
| 合成背包檢查 | 結果格數超過 `item_limit + p_bag` 就失敗 | － | plus.php:140 |
| 裝備精鍊 `plus_make_arm` | 選一顆精鍊石加一件裝備 | 見下 | plus.php:349-588 |
| 目標等級 | `plus_num+1` 必須等於石頭 `d_lv`（要依序用 LV1、LV2…） | 不符回「必須使用『LV%s』精練石」 | plus.php:415 |
| 上限 | 目標 `plus_num` 不得超過 `wog_df.d_plus`，否則「已達精鍊上限」 | － | plus.php:423 |
| **成功率** | `r=rand(1,100)`；若 `10*n > r`（n=目標等級）則**失敗**。成功機率 = (101-10n)%：+1 91%、+2 81%、+3 71%、+4 61%、+5 51%、+6 41%、+7 31%、+8 21%、+9 11%、+10 1% | － | plus.php:427 |
| 失敗處理 | `set=1`（安全精鍊石）：扣 1 顆 id 2284 的道具，**精鍊石與裝備都保留**。`set=2`：只扣該精鍊石。**裝備永不損毀**。訊息「精練失敗」 | 見左 | plus.php:429-467 |
| 成功加成 | 精鍊石各能力 `"min,max"` 擲 `rand` 後**累加**到 `wog_plus_setup` 該裝備列。消耗石與原裝備，結果寫回新字串（附 `&ps_id`） | － | plus.php:468-586 |
| 能力範圍 | 影響 `at`、`mat`、`df`、`mdf`、`str`、`agi`、`smart`、`life`、`vit`、`au`、`be`（不影響 hp） | － | plus.php:468-530 |

- 「`set` 兩種」：UI 上選擇是否使用安全精鍊石，不選會被擋（`wog_act_plus_er6`，plus.php:361-364）。
- 具體每顆石的數值範圍存在 `wog_plus_list`，**未知**。
- 精鍊石是否可買：商店只開放 `d_fie` 0~5、7~9（js/wog_data.js:245），未見 10 的商店。取得途徑：開精鍊寶盒（ARM:884，固定從 11 個 ID `1933,1943,…,2033` 隨機選一）、掉落、合成。

### 3.2 鑲嵌（`class/wog_act_hole.php`）

資料表：`wog_stone_setup`（`hs_id`、`p_id`、`d_id`、`hole_1~4`、`hole_temp_1~4`）、`wog_stone_list`（魔石數值）、`wog_stone_temp`（`d_class=99` 的擲骰結果）。

| 功能 | 規則 | 成本／機率 | 行 |
|---|---|---|---|
| 孔數 | 裝備 `wog_df.d_hole`（最多 4 孔，欄位 `hole_1~4`） | － | hole.php:36、hole.php:133 |
| 鑲嵌 `hole_setup` | 選裝備、孔位、魔石；孔位編號必須 ≤ `d_hole` | 魔石扣 1 顆 | hole.php:106-435 |
| 限制 | 該孔已有魔石 → 失敗；同一裝備不可重複同種魔石 | － | hole.php:169、173 |
| 固定魔石（`d_class<99`） | 能力取 `wog_stone_list` 當前值 | 無擲骰 | FN:249-262 |
| 隨機魔石（`d_class=99`） | 欄位格式 `"min-max"`，鑲嵌當下 `rand` 後寫入 `wog_stone_temp`（含 `s_hpmax`） | 一旦寫入固定 | hole.php:175-249 |
| 首次鑲嵌 | 新建 `wog_stone_setup` 列，背包裝備字串改為 `d_id:hs_id[&ps_id]` | － | hole.php:343-381 |
| 拆除 `hole_remove` | 需消耗 id **1378** 的道具 1 個 | `rand(1,100)<25` 時（約 24%）魔石回背包；否則魔石消失（顯示「分離失敗」） | hole.php:436-519、491 |
| 打洞 `hole_mh` | 找 `d_name` 相同且 `d_hole = 原孔數+1` 的另一件 `wog_df`（**不同 d_id 的變體**）；找不到則「洞數已達上限」。需 id **3361** 道具 1 個 | `rand(1,100)>8` 即失敗（成功 8%）。失敗只扣 3361，裝備保留；成功則裝備換成新孔數的 d_id（鑲嵌與精鍊 ID 保留） | hole.php:521-583 |
| 魔石加成能力 | `s_at`、`s_mat`、`s_df`、`s_mdf`、`s_agl`、`s_str`、`s_life`、`s_vit`、`s_smart`、`s_au`、`s_be`、`s_hpmax` | － | FN:260-300 |
| 魔石開箱 | `arm_open_box`：依 `d_lv` 抽魔石。擲 `rand(1,100)`：≤60 抽 `d_class≤1`、61~90 抽 `d_class≤2`、91~100 抽 `d_class≤3`。`d_lv==4` 時改 3 且抽 2 顆 | － | ARM:826-882 |

### 3.3 合成（`class/wog_act_syn.php`）

**`wog_act.php` 只路由 `list`、`detail`、`special`**（wog_act.php:273-282）。`syn_purify`（隨機與一般合成）和 `syn_view` **沒有入口，是死碼**，不建議重製。

| 功能 | 規則 | 成本／機率 | 行 |
|---|---|---|---|
| `syn_list` | 列出 `wog_syn` 配方，可依 `d_type` 篩選，`syn_mission=1` 表示任務配方（type=99） | － | syn.php:180-231 |
| `syn_special` 配方欄位 | `syn_element`（`"id*數量,id*數量"`）、`syn_result`、`syn_num`（單次產量）、`syn_topr`（成功率百分比）、`syn_need_mission`（需要接過的任務，查 `wog_mission_book`） | － | syn.php:287-293 |
| **金錢成本** | `1000 * item_num`（要合成的次數）。**只有成功時才扣**（至少一次成功才執行 `update p_money`） | 1000／次 | syn.php:283、438 |
| 成功判定 | 每次獨立 `rand(1,100) <= syn_topr`，成功次數 × `syn_num` 為產量。全失敗顯示「no,3」 | 機率見 `syn_topr`（**未知**） | syn.php:426-433 |
| **材料** | **不論成敗都先扣掉並提交**（`COMMIT` 在擲骰之前） | 失敗仍損失材料 | syn.php:406-424 |
| 材料來源 | 背包中任何分類（裝備、道具、魔石、精鍊石）。裝備類若已鑲嵌／精鍊，其鑲嵌與精鍊設定會被一併刪除（`stone_key`、`plus_key`） | － | syn.php:474-526 |
| 打洞裝備替代 | 材料裝備若缺，會以同名、孔數更高的版本代替（`item_syn_special_out` 遞迴） | － | item_tool.php:128-188 |
| 背包檢查 | 結果分類格數估算超過 `item_limit(+p_bag)` 則「no,4」 | － | syn.php:290-312 |
| `syn_purify`（死碼） | `syn_way=1` 價格加總±`rand(-80,80)*Σd_lv` 後找最接近的 `d_money<` 的物品；`syn_way=2` 隨機。總有 25%（`rand(1,4)==1`）失敗 | － | syn.php:61-178 |

### 3.4 其他強化／開箱

| 功能 | 規則 | 行 |
|---|---|---|
| 能力藥水 `d_type=0`（`wog_df_used`） | 永久加 `base_*`（由 `wog_df.d_g_*`）和經驗 `d_g_exp` | ARM:1020-1033 |
| 行動力藥水 `d_type=7` | `act_num += d_g_exp`，上限 50 | ARM:1044-1052 |
| 背包擴充 `d_type=1` | `p_bag = d_g_bag`（設為該值，**不是累加**） | ARM:1053-1058 |
| 倉庫擴充 `d_type=3` | `p_depot = d_g_bag` | ARM:1059-1064 |
| 密藥 `d_type=9` | `d_lv` 1~8 分別為 exp、skill_exp、at、mat、df、mdf、hp、sp 倍率，持續 `use_time` 秒 | ARM:1080-1235 |
| 寶箱 `d_type=2` | `arm_open_box`，抽魔石 | ARM:1065 |
| 其他 | 4/5 粽子、10 寵物蛋、11 紅包、12 精鍊寶盒、13 技能書、6 心得書 | ARM:1066-1237 |

## 4. 商店與經濟

| 項目 | 規則 | 行 |
|---|---|---|
| 商店 `shop` | 依 `d_fie`（部位）與 `d_lv`（1~7）分頁，每頁 8 件，依價格排序，只列 `d_dbst=0` | shop.php:17-75 |
| 購買 `buy` | 單價取 `wog_df.d_money`；道具類（`d_item_id`）總價 = 單價 × 數量（1~99）；裝備類固定 1 件 | shop.php:299-366 |
| 購買檢查 | 金錢不足、格數超限（`item_limit + p_bag`，裝備類僅 `item_limit`）皆拒絕 | shop.php:334-350 |
| 賣給 NPC | `arm_sale`：價格 = `Σ(d_money × 數量) × 0.5`，無折舊與強化加成；鑲嵌／精鍊設定一併刪除（強化不會補償） | ARM:729-824 |
| 玩家拍賣 `wog_sale`／bid | 多人功能，限制 `sale_day=10` 天、起標價手續費、`bid2_money=1000` 收購最低金額。**單機不需要**（`wog_sale` 在 `js/wog_shop.js` 只是拍賣 UI，`sale_item` 在 216 行） | CFG:100、116 |
| 勳章所 | 以勳章（item 1304、1305、1306、1307、2222）換物品或傭兵 | honor.php:95-175 |
| 加值商城 `mall` | 全部直接回「功能關閉」 | mall.php:17-35 |
| 住宿 `hotel` | 玩家等級≤100：免費全回復 HP/SP（等待 `hotel_time=5` 秒，HP=0 時 15 秒）。等級>100：費用 `round(hotel_money(10) × p_lv × slv)`，`slv`：HP=0 為 2.1、HP<最大/10 為 1.8、其餘為 `1.4×(缺血比例)`；費用下限 `10×p_lv`。**錢不夠仍會全回復，但經驗值×0.8** | store.php:18-86 |
| 銀行 | `bank_save`：需 `p_lv>=15`，金額為正整數，`p_money→p_bank`。`bank_get` 無等級限制。`BANK_TABLE=wog_player`，`BANK_FIELB=p_bank`，`MONEY_FIELB=p_money`（單機模式） | store.php:88-143、CFG:18-22 |
| 變性 | `chang_sex=1,000,000` | store.php:145-163 |
| 換系統頭像 | `chang_face=1000` | store.php:167-192 |
| 自訂頭像網址 | 同樣 `chang_face=1000`（js 說 3000，與程式不符，**以程式為準**） | store.php:194-213、js/wog_shop.js:191 附近 |
| 查看對手 | `view2_money=10` | chara.php:973-980 |

### 4.1 倉庫（`wog_player_depot`）

- 欄位：`id`、`p_id`、`d_id`、`d_type`、`d_num`、`hs_id`、`ps_id`。
- **容量**：不同「格」數上限 = `depot_limit(20) + wog_player.p_depot`（ARM:1483）。
- **堆疊**：只對 `d_type>4`（道具以上）且 `d_id` 相同才合併；合併後 `d_num>=255` 會被拒絕（ARM:1469）。裝備類各占一格並保存 `hs_id`、`ps_id`。
- **存入**：`arm_depot_add`，數量 1~99；裝備一次只能存 1（`item_out` 會擋）。
- **取出**：`arm_depot_move`（本來是轉給別人，`pay_id` 填自己即取回自己）。取回時同樣檢查背包格數。
- **綁定物（`d_send=1`）**只能取回給自己。

### 4.2 資源交易所（`wog_exchange_*`）

- 資料表：`wog_exchange_main`（12 種資源，`ex_money` 單價、`ex_amount` 庫存、`ex_change`）、`wog_exchange_list`（玩家持有量與平均成本）、`wog_exchange_book`（最近 8 筆行情通知）。
- 買：總價 = `數量 × ex_money`，加 `exchange_money=0.001`（0.1%）手續費（不足 1 時加 1）；扣全服庫存。賣：反向。
- 價格由**戰鬥事件**隨機漲跌（`exchange_up`、`exchange_down`，fight_event.php:129-190；每次 ±(0~5).xx%）。庫存總量低於 `exchange_up=300000` 時，約 1/30 機率在 `wog_etc_peo.php:41-50` 自動補貨 5000~35000。
- 資源可從寶箱、任務、推薦人獎勵取得（ARM:922-950、mission_tool.php:82-92、fight_m.php:1225-1235）。
- **單機判斷：非必要。** 這是依賴全服共享庫存、價格波動、多人行情的系統，且資源主要用於公會（wog_act_group.php:1106 取出資源）。除非要重現公會功能，建議略過，或改成固定價格商店。

## 5. 背包與道具使用

| 項目 | 規則 | 行 |
|---|---|---|
| 容量 | 每個分類欄位的「元素個數」不得超過 `item_limit(15)`。**道具類（`d_item_id`）與魔石（`d_stone_id`）另加 `wog_player.p_bag`**。裝備類（0~4）只算 `item_limit` | CFG:105、shop.php:334-340、ARM:1585 |
| 其他類別的加成 | 精鍊石合成、合成成品用 `item_limit + p_bag`；勳章所對 `d_key_id` 也加 `p_bag`（但程式用了未定義的 `$p`，等於 0） | plus.php:140、honor.php:170 |
| 堆疊 | 可堆疊類每格最多 `item_app_limit=99`，超過就拆成新格（`item_in`） | item_tool.php:190-217 |
| 裝備 | 每件獨立一格，不堆疊 | item_tool.php:213-216 |
| `item_setapp_limit`（9） | **只在 config 定義，全專案無任何使用**。裝備欄的 1~99 判斷是寫死的 | CFG:107 |
| 移除 `item_out` | 堆疊類：從前往後逐格扣，不足就報「數量錯誤」。裝備類：一次只能移除 1 件 | item_tool.php:35-126 |
| 贈送 `arm_move` | 給其他玩家；單機不需要 | ARM:642-727 |
| 信箱 `wog_player_reitem` | 欄位：`id`、`p_id`、`d_id`、`d_type`、`d_num`、`hs_id`、`ps_id`、`end_time`。拍賣流標或背包滿時暫存物品，保存 `retime=864000`（10 天），過期刪除（message.php:99-159、bid.php:184、259）。單機不需要 | CFG:112-113 |
| 戰鬥掉寶 | 背包滿則該件放棄（`parent.get_item(...,0,bag)`）。機率：`rand(1,m_topr×0.9)<=1`（VIP 為 0.8、0.7 倍的分母） | fight_event.php:20-105 |

### 5.1 HP/SP 補給（單機最重要）

- 補給品是 `d_type=11` 的道具，穿在 `d_item_id2` 欄，數量存 `d_item_num2`。補給量 = `wog_df.d_g_hp`、`d_g_sp`。**實際數值未知**（資料表缺失）。
- 只有**戰鬥中自動使用**，沒有手動使用路徑（grep 無其他入口）。
- 觸發條件（fight_m.php:726）：`HP < hpmax×f_hp` **且** `SP < spmax×f_sp`（兩者**同時**成立，AND）、補給品有 `d_g_hp>0 或 d_g_sp>0`、`d_item_num2>0`、本場已用次數 `check_item<9`、非傭兵。
- 效果（fight_m.php:728-737）：`hp += d_g_hp`、`sp += d_g_sp`，各自不超過上限；`d_item_num2--`；為 0 時 `d_item_id2=0`。
- 預設門檻 `f_hp=15`（%）、`f_sp=100`（%）。SP 門檻預設 100%，等於「SP 沒滿就算」，因此**實務上由 HP 低於 15% 決定**。玩家可從 UI 改（0~100，超出範圍則回預設，wog_fight.php:93-104）。
- 戰後把剩餘數量寫回 `wog_player_arm`（fight_m.php:765-767）。
- id 258、1837、2236 放在 `d_item_id2` 時是寵物召喚物，戰鬥開頭消耗（fight_select.php:273-306）。

## 6. 單機相關 config 常數（`CFG`，行號即該常數所在行）

| 常數 | 值 | 行 | 意義 |
|---|---|---|---|
| `total_point` | 10 | 97 | 建立角色可自由分配的點數 |
| `player_num` | 1 | 99 | 每帳號角色數 |
| `del_day` | 62208000 | 95 | 多久沒玩刪除角色（720 天） |
| `del_confirm` | 604800 | 96 | 刪除確認期（7 天） |
| `view2_money` | 10 | 101 | 查看對手費用 |
| `chang_face` | 1000 | 102 | 換頭像費用 |
| `chang_sex` | 1000000 | 103 | 變性費用 |
| `depot_limit` | 20 | 104 | 倉庫基本格數 |
| `item_limit` | 15 | 105 | 背包每分類基本格數 |
| `item_app_limit` | 99 | 106 | 堆疊上限 |
| `item_setapp_limit` | 9 | 107 | **未使用** |
| `player_age` | 30 | 108 | 幾天算一歲 |
| `act_num_time` | 180 | 114 | 行動力補充週期（秒） |
| `act_num` | 6 | 115 | 每週期補充行動力 |
| `hotel_money` | 10 | 120 | 住宿費係數（×等級×倍率，等級>100 才收） |
| `hotel_time` | 5 | 121 | 住宿等待秒數 |
| `hotel_die_time` | 15 | 122 | HP=0 住宿等待秒數 |
| `act_dely_time` | 3 | 125 | `wog_act.php` 請求間隔 |
| `etc_dely_time` | 3 | 124 | `wog_etc.php` 請求間隔 |
| `cp_mmoney` | 500 | 131 | 挑戰冠軍所需金額 |
| `f_time` | 2 | 132 | 戰鬥間隔秒數 |
| `f_time1` | 12 | 133 | 行動力不足時的戰鬥間隔 |
| `f_down` | 0.5 | 134 | 行動力不足時報酬倍率 |
| `f_count` | 40 | 135 | 戰鬥回合上限 |
| `f_hp` | 15 | 136 | HP 低於 % 自動用補給 |
| `f_sp` | 100 | 137 | SP 低於 % 自動用補給 |
| `f_cp` | 604800 | 138 | 冠軍挑戰冷卻（7 天） |
| `f_hero` | 259200、345600 | 140-141 | 英雄重置時間 |
| `f_escape_hp` | 0 | 142 | HP 低於 % 逃跑 |
| `mission_get_num` | 3 | 214 | 同時進行任務上限 |
| `place` | 21 個地圖 | 195-202 | 地圖名稱表 |
| `pet_*` | 10、28800、15、864000、20 | 208-212 | 寵物特訓、競技、飢餓、歲數、餵食費 |
| `exchange_up`、`exchange_money` | 300000、0.001 | 110-111 | 資源交易所補貨門檻、手續費 |
| `bid2_money`、`sale_day`、`mall_time` | 1000、10、86400 | 100、116-117 | 拍賣／商城相關 |
| `g_*`、`t_*`、`creat_group` | － | 144-206 | 公會與組隊（多人，單機可略） |
| `event_ans*`、`keylen`、`attempts`、`unfreeze` | － | 216-220 | 隨機問答防外掛、帳號鎖 |
| `message_box`、`retime`、`timezone`、`gzip_compress`、`online_limit`、`login_time`、`offline_time`、`main_url`、`logout_url`、`code_url` | － | 98、112-113、118、127-129、221-223 | 伺服器、信箱、登入設定（單機不需要） |

## 7. 未知項目與已知矛盾

- **未知**：`wog_df`、`wog_df_used`、`wog_plus_list`、`wog_stone_list`、`wog_syn`、`wog_honor_main` 的實際資料（具體裝備數值、價格、補給量、各精鍊石範圍、配方成功率）。
- **未知**：`d_dbst` 的確切語意（推論為商店禁售）；`d_vip` 用途；`wog_player_buffer.sp` 欄位。
- **程式矛盾**：
  - 密藥 `case 8`（提升 SP）更新時誤寫 `mdf=` 而非 `sp=`（ARM:1186 附近）。
  - 住宿計費用 `$have_price[p_au]` 但查詢只選 `au`，故 HP 上限扣減永遠為 0（store.php:22、35）。
  - 自訂頭像費用 UI 寫 3000，程式收 `chang_face=1000`。
  - `arm_open_rice` 中的 `$d_lv` 重複判斷（`$r>190 && $r<=195` 寫了兩次，永遠是 2，ARM:~990）。
  - 勳章所的背包容量判斷引用未定義變數（honor.php:170-171）。
- **賣價**：`d_money*0.5` 可能出現小數，寫入 `p_money` 時是否進位，需看欄位型別（**未知**）。

*(以上所有 Markdown 內容即報告本文；檔案路徑 `/private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-items.md` 尚未建立，請由具備寫檔權限的一方存檔。)*

## 關鍵規則摘要（10 行）

1. 背包是每個分類一個逗號字串；可堆疊類用 `id*數量`（上限 99），裝備類每件一格，並以 `:hs_id`、`&ps_id` 附帶鑲嵌與精鍊。
2. 容量：每個分類最多 15 格；道具與魔石另加 `p_bag`，倉庫 20 格加 `p_depot`，倉庫堆疊上限 254。
3. 裝備只看基礎能力與職業，**沒有等級需求、沒有耐久與修理**。
4. 攻防公式：`at = 裝備攻擊 + str`、`df = 裝備防禦 + vit`，`hpmax = 25×life + 50`，`spmax = 3×(smart+be) + 25`。
5. 精鍊成功率為 `101-10n %`（+1 91% 至 +10 1%），失敗只損耗精鍊石（或改耗安全石 2284），**裝備不會壞**；沒有金錢成本。
6. 鑲嵌：固定魔石與隨機魔石（`d_class=99`，鑲嵌當下擲骰）；拆魔石需道具 1378，約 24% 回收；打洞需道具 3361，成功 8%，打洞是換成「同名、孔數+1」的另一個 `d_id`。
7. 合成只有「特殊配方」路徑有效（`wog_syn`）：每次 1000 元（成功才扣），材料不論成敗都消耗，成功率取自配方 `syn_topr`。
8. 商店只賣 `d_dbst=0`，依 `d_lv` 1~7 分頁；買價為 `d_money`，賣給 NPC 為 `d_money × 0.5`；初始金錢 2000。
9. 補給品放第二道具欄，戰鬥中 HP<15% 且 SP<100% 時自動使用，每場最多 9 次；住宿 100 級以下免費。
10. 最大未知：所有 `wog_df` 等資料表的實際數值（價格、補給量、精鍊石範圍、配方成功率）；資源交易所依賴全服共享狀態，建議單機版略過。
