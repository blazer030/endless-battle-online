**說明：** 我這次是唯讀模式，沒有寫檔，所以 `/private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-stats-jobs.md` **不存在**。完整報告就是下面的內容，需要的話請你或有寫入權限的 agent 把它存成該檔。引用的路徑都相對於 `/Users/ethan/Project/WOG4.5_Releas_CT/`（下稱 `ROOT`），格式為 `檔案:行號`。

---

# WOG 4.5 單機系統規則研究：角色數值、創角、升級、職業

## 0. 先講最重要的幾個發現
- **這個釋出版沒有「升級配點」機制。** `wog_player` 沒有 `p_point`、`p_lv` 以外的點數欄位。升級時能力值由職業表 `wog_character` 的範圍隨機決定（`class/wog_fight_m.php:158-180`）。全專案 grep `p_point` 只出現在公會的 `wog_group_member_point.p_point`，與角色能力無關。
- 唯一的配點是「創角時 4 項合計 ≤ 10 點」（`wog_act_config.php:97`、`class/wog_act_chara.php:114`）。
- 配點重置（`restup`）在釋出版被砍成空函式（`class/wog_act_mall.php:27-31`，只會跳錯誤訊息）。UI 仍在（`js/wog_chara.js:151`、`js/wog_shop.js:62-78`）。
- 沒有等級上限。`lv_up` 是 `while` 迴圈，無上限判斷（`class/wog_fight_m.php:158`）。全專案沒找到 max level。
- 資料庫 SQL 不在專案內（`find -iname "*.sql"` 為空）。`wog_character` 的實際資料（各職業 id、名稱、成長範圍、轉職條件）全是「未知」，只能從程式推語意。

---

## 1. 角色數值（`wog_player` 欄位）

### 1.1 欄位與語意

| 欄位 | 語意 / UI 名稱 | 證據 |
|---|---|---|
| `p_id`, `p_name`, `p_password`, `p_email`, `p_sex`(1男/2女), `i_img`, `p_cdate`(建立時間) | 帳號與外觀 | `class/wog_act_chara.php:62-75` |
| `str` | 力量 | `js/wog_chara.js:168` |
| `smart` | 智力（程式註解有時寫「智慧」） | `js/wog_chara.js:168`、`js/wog_job_skill.js:152` |
| `agi` | 敏捷 | `js/wog_chara.js:169` |
| `life` | 生命 | `js/wog_chara.js:169` |
| `vit` | 體質 | `js/wog_chara.js:171` |
| `au` | 魅力 | `js/wog_chara.js:170` |
| `be` | 信仰 | `js/wog_chara.js:170` |
| `base_str/smart/agi/life/vit/au/be` | **裸點**（不含裝備與鑲嵌）。UI 面板顯示的就是 base 值，裝備加成另外用 `etc_*`（= 總值 − base）以括號標示 | `class/wog_act_chara.php:534-540`、`:615` |
| `at` / `mat` / `df` / `mdf` | 物攻 / 魔攻 / 物防 / 魔防 | `js/wog_chara.js:172-173` |
| `hp`, `hpmax`, `sp`, `spmax` | 目前與上限 | `js/wog_chara.js:155` |
| `p_lv`, `p_exp`, `p_nextexp` | 等級、目前經驗、「本級」升級所需經驗 | `js/wog_chara.js:154` |
| `p_money` | 金錢 | |
| `p_win`, `p_lost` | 勝敗場數 | `js/wog_chara.js:124` |
| `s_property` | 屬性 1地 2水 3火 4木 5風 6毒 | `js/wog_data.js:119-127`、`js/wog_chara.js:305` |
| `p_birth` | 出生地：1中央大陸 2魔法王國 3熱帶雨林 4末日王城 | `js/wog_data.js:113-118` |
| `p_place` | 目前冒險地（對應 `sec[]`） | `js/wog_data.js:84-107` |
| `p_sat_name` | 必殺技名稱 | `class/wog_act_chara.php:69` |
| `ch_id` | 目前職業 id | `class/wog_act_job.php:196` |
| `act_num`, `act_num_time` | 行動力（上限 50） | `class/wog_fight_select.php:36-47`、`class/wog_act_chara.php:522-533` |
| `p_act_time`, `p_online_time`, `p_lock`, `p_npc`, `p_st`, `p_g_id`, `t_id`, `p_recomm` | 戰鬥冷卻、上線時間、鎖定、NPC 旗標、狀態、公會、隊伍、推薦人 | 各處 |

另有輔助表：
- `wog_ch_exp`：職業熟練度，欄位 `ch_<職業id>` 與 `sk_<職業id>`。
- `wog_player_arm`：裝備。
- `wog_item`：背包。
- `wog_skill_setup`：技能欄 `skill_1..5`、`time_1..5`。
- `wog_player_buffer`：商城增益。

### 1.2 衍生值公式（`forum_support/function.php`，主程式實際載入的版本）
主程式 `wog_act.php:15`、`wog_etc.php:16`、`wog_group.php:15`、`wog_fight.php:16` 都 require `function.php`。`function2.php` 是另一版（SP 係數是 5），**沒被引用**。

| 值 | 公式 | 出處 |
|---|---|---|
| 總能力（str/smart/agi/life/vit/au/be） | `base_X + 裝備d_g_X + 精煉d_X + 鑲嵌s_X` | `function.php:383-389` |
| `at` | `裝備d_at + 精煉d_at + 鑲嵌s_at + str(總)` | `:378,391` |
| `mat` | `… + smart(總)` | `:379,392` |
| `df` | `… + vit(總)` | `:380,393` |
| `mdf` | `… + be(總)` | `:381,394` |
| `hpmax` | `round(life總*25)+50 + 鑲嵌s_hpmax` | `:396`、`:426-430` |
| `spmax` | `round((smart總+be總)*3)+25` | `:397`、`:434-438` |
| 商城 buffer | `at/mat/df/mdf/hpmax/spmax` 乘上 buffer 倍率（未過期者） | `:399-411` |

`check_status` 在登入（`show_chara` → `check_buffer`）、升級、換裝、換職業後都會重算並寫回 `wog_player`（`:417-419`）。

**命中、閃避、爆擊（`class/wog_fight_m.php`）**
- 物理命中率：`agi_p_rag = p.agi/(p.agi+m.agi)*100 + p.rag`（`:566-568`），出手時再 +20，`agi_p_rag > rand(1,100)` 才命中（`:217,221`）。`rag` 是裝備或技能給的迴避加成，初始 0（`:476`）。
- 物理爆擊：基礎 `cri=10`、`cri_dmg=1.2`。若 `p.agi > m.agi`，則 `cri = min(20, p.agi/m.agi*10)`，`cri_dmg = min(p.agi/m.agi*1.2, 2*acrtdmg)`（`:222-236`）。爆擊判定 `rand(1,100) < cri + p.act`（`:152`）。
- 魔法攻擊**沒有命中判定**，一定命中。爆擊用 `au` 比值，同樣上限 20% 與 2 倍（`:262-282`）。
- 傷害 `fight()`（`:74-87`）：
  - `temp_at = f_a/d_d`。
  - `temp_d = (f_a*temp_at - d_d) * 屬性倍率`，最小 1。
  - 再加 `rand(temp_d/f_a/3, temp_d/f_a/2)`，乘 `def_dmg`，加 `get_dmg`。
  - 實質上近似 `atk²/def − def`。
- 屬性倍率 `s_check`（`:899-920`）：屬性差 −1 或 +5 為 ×1.2，差 +1 或 −5 為 ×0.8，其餘 ×1。這代表屬性 1→6 為循環相剋。
- 物理 / 魔法選擇 `m_type`（`:88-112`）：`at >= mat` 時 80% 物理，否則 80% 魔法，其餘 50/50（受 `temp_mtype` 影響）。
- 逃跑 `escape`（`:921-941`）：`agi` 較高 30%，否則 10%；`agi/3 > 對手` 再 +10，`agi/2 > 對手` 再 +10。
- 戰鬥回合上限 `f_count=40`（`wog_act_config.php:136`）。

### 1.3 其他影響數值的來源
- **能力藥水**（`wog_df_used.d_type=0`）：直接加 base_*、+exp（`class/wog_act_arm.php:1024-1030`）。
- **裝備需求**：`wog_df.ch_id`（0=不限職業）要等於玩家職業，且 `base_agi>=d_magl`、`base_str>=d_mstr`、`base_smart>=d_msmart`、`base_au>=d_mau`（`class/wog_act_arm.php:463`）。比較的是 **base 值**，不含裝備。
- 住宿 `hotel`：Lv≤100 免費回滿 HP/SP；Lv>100 收 `10*Lv*slv` 金幣，錢不夠就扣 20% exp 後回滿（`class/wog_act_store.php:18-60`；`wog_act_config.php:124`）。

---

## 2. 創角（`chara_make` / `chara_chk` / `chara_save`）

| 項目 | 規則 | 出處 |
|---|---|---|
| 總配點 | `$wog_arry["total_point"]=10` | `wog_act_config.php:97` |
| 可配點項目 | **只有 4 項**：力量 str、智力 smart、生命 life、敏捷 agi | `js/wog_chara.js:296,322-330` |
| 每項範圍 | 下拉選單 +1~+10，且每項必須 ≥1（`<=0` 報錯）；4 項總和 `> 10` 報錯。所以實務上是 4 項各 ≥1、合計 ≤10，也就是最多 6 點可自由分配（1+1+1+1 為底） | `class/wog_act_chara.php:110-117` |
| 基礎值 | 每項**固定 +10**：`str=$_POST[str]+10` 等 | `class/wog_act_chara.php:53-56` |
| 固定 10 的欄位 | `vit=10, au=10, be=10`（不可配）；`mdf=10` | `:66-68` |
| 初始 base_* | `base_str=str, base_life=life, base_smart=smart, base_agi=agi`，`base_vit/au/be=10` | `:71` |
| 初始 at/mat/df | `at=str, mat=smart`；**`df=life`**（注意是 life 不是 vit，這是原始碼的寫法，登入時 `check_status` 會用 `vit` 重算覆蓋） | `:66` |
| 初始 HP/SP | `hp=50, hpmax=50, sp=25, spmax=25`。登入時 `check_status` 會重算 hpmax/spmax，但 `hp/sp` 不變，所以 hp 起始 50 | `:69,74` |
| 初始等級 / 經驗 | `p_lv=1, p_exp=0, p_nextexp=1000` | `:69-70` |
| 初始金錢 | 2000 | `:69` |
| 起始職業 | 由玩家從 `wog_character where ch_mlv=1` 的清單挑；伺服器只擋 `ch>9`。**不是固定職業** | `:33,118` |
| 起始地點 `p_place` | INSERT 沒給，用資料庫預設，「未知」 | |
| 起始道具 / 裝備 | **無**。`wog_item` 全空字串、`wog_player_arm` 與 `wog_skill_setup`、`wog_ch_exp` 全用預設（一般為 0） | `:77-81` |
| 必選項 | 帳號、密碼、email、性別、屬性 `s`（1-6）、出生地 `birth`（UI 只開放 1-4，伺服器擋 >5）、圖像、必殺技名稱 | `:98-125`；`js/wog_chara.js:301-334` |
| 其他限制 | 帳號不可重複；同一 email 最多 `player_num=1` 個角色 | `:127-139`；`wog_act_config.php:98` |
| 推薦人 | 帶 `recommid` 時互加好友並發訊息 | `:82-89` |
| 初始行動力 | INSERT 沒給，資料庫預設，「未知」。補充規則：每 180 秒 +6，上限 50 | `wog_act_config.php:114-115` |

另有 SNS 創角 `sns_save` 沿用同一個 `chara_save`（`class/wog_act_chara.php:180-244`），規則相同。

---

## 3. 升級、經驗、金錢

### 3.1 升級（`lv_up`，`class/wog_fight_m.php:146-207`）
觸發：戰鬥結束後 `p_exp > p_nextexp`（嚴格大於，`:773`）才進 `lv_up`。

```
while (p_exp > p_nextexp):
    p_lv += 1
    p_exp -= p_nextexp            // 先扣舊 nextexp
    p_nextexp = 6*p_lv^2 + 1080*p_lv   // 以「新等級」重算（:164）
    對每項能力：加 rand(min,max)，min,max 取自 wog_character.ch_X 字串 "min,max"（:165-179）
```

- 經驗曲線：`p_nextexp(新等級 L) = 6L² + 1080L`。例：L2=2184、L10=11400、L50=69000、L100=168000。Lv1 的 nextexp 是創角寫死的 1000（`class/wog_act_chara.php:70`）。曲線在 `:162-163` 有被註解掉的舊版公式。
- **每級成長** 7 項（str、agi、life、vit、smart、be、au）各自 `rand(min,max)`，範圍由**當時的職業**（`$p[ch_id]`）在 `wog_character` 的對應欄位決定。欄位值是 `"min,max"` 字串（`explode(",")`；`js/wog_job_skill.js:159` 也把 `,` 顯示成 `-`）。具體數值「未知」。
- 升級同時把這些增量加到總值與 `base_*`（`:187-190`），也加 `at/df/mat/mdf`。之後 `check_arm_status`（`:206`）重算 hpmax/spmax。
- **升級不回血、不回 SP、無自由配點、無最大等級**。
- 成長取決於「升級當下的職業」，所以換職業會改變之後每級的成長率。
- UI 訊息：`parent.lv_up(str,agl,smart,life,vit,au,be)`（`js/wog_fight.js:610`）。

### 3.2 `get_exp`（`:113-138`）
符號：`mlv`=怪物等級，`plv`=玩家等級，`s`=是否擊殺（1=擊殺，0=超過回合數）。

```
base  = (m_at + m_mat) / 2
exp_at = rand(base, base + mlv)                 // PHP rand 會截斷成整數
if plv <= 100: exp_at *= 1.3
if mlv >= 450: exp_at *= 0.6
exp_at = mlv * exp_at * (mlv / plv)
exp = exp_at * (m_hp/m_hpmax) * 0.18            // 擊殺時 hp/hpmax=1
if s == 0: exp *= (m_hp/m_hpmax)                // 逾時時額外再乘一次
if plv <= 15: exp *= 2.5
return round(exp * 2 * mercenary_setexp * f_down)
```
- 常數 `temp_lv=2` 寫死（`:135-136`）。
- `f_down`：行動力用完時為 0.5，否則 1（`wog_act_config.php:134`；`class/wog_fight_select.php:51-66`）。
- `mercenary_setexp`：玩家自己打時預設 1；傭兵為 `me_get_exp/100`（`class/wog_fight_select.php:804`）。
- 擊殺時呼叫（`:807`）：`get_exp(m.hpmax, m.hpmax, m.at, m.mat, m.m_lv, p.p_lv, 1)`，之後再乘商城 buffer 的 `exp`（`:811-818`）。
- 逾 40 回合時（`:714-716`）：傷害總和上限為怪物 hpmax，再除 1.5，等級用 `m_lv*0.6`，`s=0`。這種情況算「敗」。
- 戰敗（HP 歸 0）exp 為 0（`lost_check` `:871`）。逃跑成功 exp、錢為 0（`:753-754`）。`act>=1000`（挑戰冠軍類）exp、錢為 0（`:799-803`）。

### 3.3 `get_money`（`:139-145`）
```
at_total  = (m_at + m_mat) / m_lv
at_total2 = (m_hp / (m_at + m_mat)) * 0.4
money = rand(m_lv/2.5, m_lv) * (rand(at_total, at_total2) * 0.4) * mercenary_money * f_down
```
- 擊殺時呼叫（`:808`）：`round(get_money(m.hpmax, m.at, m.mat, m.m_lv) * skill_money_up)`。
- 兩個 `rand` 的參數順序可能顛倒（at_total 可能大於 at_total2），依 PHP 版本行為會自動交換，結果仍在兩者之間。怪物數值「未知」，所以無法算出實際金額。
- 勝利當場金錢加到 `p_money`（`:780,188`）。

### 3.4 其他
- 職業熟練度（見 §4.3）在每次**擊殺**時加上 `m.m_job_exp`（`:827`）。
- 隊伍分經驗的段落整段被註解掉（`:838-856`）。

---

## 4. 職業系統（`class/wog_act_job.php` 全文）

### 4.1 `wog_character` 表欄位推論

| 欄位 | 語意 | 證據 |
|---|---|---|
| `ch_id` | 職業 id（同時是 `wog_ch_exp.ch_<id>` / `sk_<id>` 的欄位後綴） | `wog_act_job.php:52`、`class/wog_act_skill.php:24` |
| `ch_name` | 職業名稱 | `:29` |
| `ch_mlv` | **解鎖等級**。`job_view` 只列出 `ch_mlv <= p_lv` 的職業；創角只列 `ch_mlv=1`。**`job_setup` 伺服器端沒再檢查** | `:29-30`、`class/wog_act_chara.php:33` |
| `ch_str/smart/life/vit/agi/au/be` | **每級成長範圍** `"min,max"`（不是倍率、不是需求、不是一次性加成） | `class/wog_fight_m.php:158,165-179` |
| `ch_main` | **前置（父）職業 id**；0 表示無前置 | `wog_act_job.php:53,81` |
| `ch_map` | **需要的前置職業熟練度**（與 `wog_ch_exp.ch_<ch_main>` 比較） | `:83-84` |

`job_view` 輸出欄位（`:52`）：`id|名稱|ch_str|ch_smart|ch_life|ch_vit|ch_agi|ch_au|ch_be|玩家對該職業熟練度|ch_map|是否目前職業|父職業名稱`。前端表頭「轉職所需職業熟練度 / 目前職業熟練度」（`js/wog_job_skill.js:152`）。

### 4.2 轉職規則（`job_setup`，`:63-211`）

| 項目 | 規則 |
|---|---|
| 等級需求 | UI 列表過濾 `ch_mlv <= p_lv`（`:29-30`）。**`job_setup` 沒驗證等級，只靠 UI** |
| 前置職業 | 若 `ch_main > 0`：要求 `wog_ch_exp.ch_<ch_main> >= ch_map`，否則「轉職條件不符」（`:81-85`；`language/wog_act_traditional_chinese.php:80`） |
| 熟練度顯示 | `floor(熟練度/3500*100)%`，所以 **3500 = 100%**（`js/wog_job_skill.js:177-182`）。此 3500 只是前端顯示分母，`ch_map` 的實際門檻值「未知」 |
| 金錢 / 道具成本 | **無**。程式內沒有任何扣錢或扣道具 |
| 次數 / 冷卻 | 無 |
| 不存在職業 | `ch_id` 查不到則「錯誤職業」（`:206-208`） |
| 是否可換同職業 | 沒擋 |
| 能力值 | **重設為 base 值（吃掉所有裝備、精煉、鑲嵌加成）**：`at=base_str, mat=base_smart, df=base_vit, mdf=base_be, agi/au/be/vit/smart/str/life=base_*`（`:196-199`） |
| hpmax / spmax | `player_hp(base_life)`、`player_sp(base_smart+base_be)`；現有 hp/sp 超過就截斷（`:191-199`） |
| 裝備 | **全部卸下**放回背包：`wog_player_arm` 的武器/身/頭/手/腳/飾品/道具與鑲嵌(s_*)/精煉(p_*)欄位全清 0（`:201-203`），並寫回 `wog_item`（`:88-188`，用 `,` 分隔基本、`:` 接鑲嵌、`&` 接精煉的字串格式）。道具堆疊用 `item_in` 合併 |
| 技能 | **只清** `skill_4, skill_5, time_4, time_5`（`:204`）。依 `class/wog_act_skill.php:275-292`，`type=1` 的被動技能只能放第 4 欄以後，主動技能不能放第 4 欄以後。所以被動欄清空，主動欄 skill_1~3 保留（但安裝時會檢查技能所屬職業必須與目前職業一致，`:281-285`，`skill_er7`） |
| 已學技能 `wog_skill_book` | 不刪除 |
| 等級 / 經驗 / base_* | **不變**（轉職不重置等級，也不重算能力點） |
| 之後影響 | 只影響「之後每級」的成長範圍（§3.1）、可裝備的職業專屬裝備、可用的職業技能 |

### 4.3 熟練度（`wog_ch_exp`）
- 每個職業兩個計數器：
  - `ch_<id>`：**累積熟練度**，用於轉職條件（`wog_act_job.php:83`），只增不減。
  - `sk_<id>`：**可花用的技能點**，學技能時扣除（`class/wog_act_skill.php:143-157`）。
- 每次**擊殺**怪物時，對**目前職業**同時加上 `m.m_job_exp`（`class/wog_fight_m.php:827`）。`m_job_exp` 在遭遇時乘上 `f_down`（行動力用完減半，`class/wog_fight_select.php:122`），贏的時候再乘商城 buffer `skill_exp`（`:819-822`）。
- 不是擊殺（敗、逃、逾時、挑戰冠軍）都不加。
- 另一個來源：心得書（`wog_df_used.d_type=6`）只加 `sk_<ch_id>`，不加 `ch_<id>`（`class/wog_act_arm.php:1038-1042`）。
- 學技能需 `sk_<職業> >= 技能.ch_exp`，且金錢 ≥ `s_money`，同時要先學會前置技能 `need_sid`（`class/wog_act_skill.php:125-157`）。成功後扣金錢與 `sk_`（`ch_` 不扣）。
- 裝備的「熟練度不足」檢查被註解掉（`class/wog_act_arm.php:535-541`），釋出版不生效。

### 4.4 職業名稱清單
**確定有的（有 id）**：技能大師選單 `js/wog_data.js:264-267`。

| ch_id | 名稱 | 備註 |
|---|---|---|
| 6 | 戰士 | 技能選單第一列 |
| 7 | 術士 | |
| 8 | 盜賊 | |
| 9 | 樂手 | |
| 10 | 狂戰士 | 第二列（進階職業） |
| 11 | 騎士 | |
| 12 | 魔導師 | |
| 13 | 風水師 | |
| 14 | 刺客 | |
| 15 | 武術家 | |
| 16 | 舞者 | |
| 17 | 獸師 | |

**未知 / 推測**
- `ch_id` 1~5：選單沒列，名稱「未知」（可能是無技能的新手職業或 NPC 職業，但無證據）。
- 英雄稱號（`js/wog_chara.js:251-277`）：id 2→不敗英雄、3→幻魔英雄、4→神風英雄、5→鋼鐵英雄、6→結界英雄、7→魅心英雄、8→武術英雄、9→獸王英雄。這是 `wog_player_cp.hero_type`（冠軍/英雄類型，`class/wog_etc_hero.php:23`），**不等於職業 id**，也無法確認是否與職業 1:1 對應，不要當職業表用。
- 創角下拉來自 `ch_mlv=1`，且伺服器擋 `ch > 9`（`class/wog_act_chara.php:118`）。所以**起始職業 id ≤ 9**。結合選單，6~9（戰士、術士、盜賊、樂手）很可能是起始職業，但這是推測，真實資料「未知」。
- 父子關係：10~17 被當成進階職業只是依選單兩列的排版推測。`ch_main` 對應「誰是誰的前置」的資料在 DB，「未知」。
- 職業專屬怪物：`wog_monster.m_job` 與任務推薦用 `a.m_job=ch_id or 99`（`class/wog_act_chara.php:717`）。99 代表通用。

### 4.5 全域設定
`wog_act_config.php` 只有創角點數、行動力、戰鬥時間等，沒有轉職相關設定。

---

## 5. 配點 UI 與重置

| 項目 | 現況 | 出處 |
|---|---|---|
| 創角配點 UI | 4 個下拉（力量、智力、生命、敏捷），各 +1~+10，文案「你共有 10 點的加分點數…分配總和超出 10 以上則無法註冊」 | `js/wog_chara.js:320-332` |
| 創角上限 | 總和 ≤ `total_point`，每項 ≥ 1 | `class/wog_act_chara.php:110-117` |
| 升級後配點 | **不存在**。能力值全由職業成長範圍隨機決定 | `class/wog_fight_m.php:165-179` |
| 重置 / 重新配點 | UI 有「(配點)」連結，確認文案「重新分配升級能力點數，需消耗 3 遊戲點」（`js/wog_chara.js:151`，只在 `win_num` 為 0 或未定義且是本人時顯示）。結果頁 `restup(a1,a2)` 顯示「LV x 重新配點結果」與「現在基礎狀態」（`js/wog_shop.js:62-78`）。**後端 `mall_restup` 被砍成 `alertWindowMsg(wog_main_error4)`，釋出版不可用**（`class/wog_act_mall.php:27-31`） |
| 推測的原版行為 | 從 UI 結果頁看，「重新配點」是**重新擲一次升級成長**（顯示每項「+N」與等級），消耗「遊戲點」（商城貨幣，釋出版移除了商城）。這只是 UI 推測，細節「未知」 |
| 換職業會不會洗點 | 不會（§4.2），只是把顯示值重設為 base 值 |
| 其他永久加點途徑 | 能力藥水直接加 base_*（`class/wog_act_arm.php:1024-1030`） |

---

## 6. 重現時需要注意的陷阱
1. `p_nextexp` 是「該級升下一級所需經驗」，升級時先扣舊值再用新等級算新值，所以第 L 級升 L+1 實際所需是 `6L²+1080L` 的前一級值（Lv1→2 要 1000，Lv2→3 要 `6·2²+1080·2=2184`）。
2. `check_status` 的 `hp/sp` 不會被自動補滿，只有 `hpmax/spmax` 重算。
3. 面板顯示的能力值是 `base_*`，括號內才是裝備加成。
4. 轉職會卸光裝備，`at/df/…` 回到只含 base 的值；之後重新穿裝才會恢復（`check_arm_status`）。
5. `function2.php` 與 `function.php` 的 SP 係數不同（5 vs 3），以 `function.php`（3）為準。
6. 多處 PHP `rand(float, float)` 會截斷為整數，若重現要照做。

---

## 7. 未知清單（資料庫內容，程式看不出來）
- `wog_character` 全部資料：職業 id 1~5 的名稱、每個職業的 7 項成長範圍、`ch_mlv`、`ch_main`、`ch_map` 實際值、起始職業清單。
- `wog_player` 預設值：`p_place`、`act_num`、`act_num_time`、`p_win` 等。
- 怪物表 `wog_monster`（`m_at/m_mat/m_hp/m_lv/m_job_exp/...`），因此 exp、金錢、熟練度的實際數量級算不出來。
- 技能表 `wog_ch_skill` 與裝備表 `wog_df` 的數值。
- 「重新配點」後端原版邏輯與消耗（遊戲點）的細節。
- 是否真的有等級上限（程式沒擋，只可能由資料面的怪物等級或經驗曲線自然限制）。