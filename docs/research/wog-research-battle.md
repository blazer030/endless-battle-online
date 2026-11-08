**說明：我這次在唯讀模式，沒有寫檔工具，所以無法寫入 /private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-battle.md。完整報告直接放在下面（Markdown），請由主 agent 存檔。**

路徑簡寫：`FM`=class/wog_fight_m.php，`FS`=class/wog_fight_select.php，`FK`=class/wog_fight_skill.php，`FE`=class/wog_fight_event.php，`CFG`=wog_act_config.php，`FP`=wog_fight.php。

---
# WOG 4.5 單機戰鬥系統研究報告

## 0. 基礎數值（戰鬥前提）

**角色創建**（class/wog_act_chara.php:62-77）
- str、life、smart、agi = 10 + 配點（`total_point`=10，CFG:97）。
- vit、au、be 固定 10。
- 初始 hp=50、sp=25、金錢 2000、Lv1、p_exp=0、p_nextexp=1000。
- 屬性 `s_property` 1-6，由玩家選：1 地、2 水、3 火、4 木、5 風、6 毒（js/wog_chara.js:305，js/wog_data.js:120-126）。

**戰鬥用衍生數值**（forum_support/function.php:227-424 `check_status`）
```
at   = Σ裝備d_at + 鑲嵌s_at + 精煉d_at + str
mat  = Σ裝備d_mat + ... + smart
df   = Σ裝備d_df  + ... + vit
mdf  = Σ裝備d_mdf + ... + be
agi/life/au/be/vit/smart/str = 基礎(base_*) + 裝備d_g_* + 鑲嵌 + 精煉
hpmax = round(life*25)+50 + 鑲嵌s_hpmax        -- function.php:426
spmax = round((smart+be)*3)+25                  -- function.php:434
商城buff（wog_player_buffer，未過期）：at/mat/df/mdf/hp/sp 乘倍率
```
- 武器只提供 at/mat 的加成。沒有獨立的「武器攻擊力 × 倍率」，武器和 str 直接相加。
- 戰鬥直接讀 DB 的 `at, df, mat, mdf, agi, au, ...`（FS:26-33）。

**升級**（FM:146-208）
```
while p_exp > p_nextexp:                       -- 嚴格大於
    p_lv++ ; p_exp -= p_nextexp
    p_nextexp = 6*lv^2 + 1080*lv               -- FM:164
    各屬性 += rand(min,max)    -- 來源 wog_character.ch_str/ch_agi/ch_life/ch_vit/ch_smart/ch_au/ch_be，格式 "min,max"
```
- 升級後 at/df/mat/mdf 也加同值，最後由 `check_arm_status` 重算（FM:206）。
- HP/SP 不會因升級回滿（FM:188 寫回當前 hp、sp）。
- 各職業的 min,max 值在 SQL 內，**未知**。

---
## 1. 戰鬥流程（FM）

### 1.1 入口與參數（FP:84-111，js/wog_fight.js:13-90）
| 玩家設定 | POST | 規則 | 預設 |
|---|---|---|---|
| 回合上限 f_count | temp_id3 | >40 則用 CFG f_count=40（FP:84-90） | 40 |
| 自動喝藥 HP% f_hp | temp_id4 | 超出 0-100 則用 CFG f_hp=15（FP:91-97） | 15 |
| 自動喝藥 SP% f_sp | temp_id8 | 超出則用 CFG f_sp=100（FP:98-104） | 100 |
| 逃跑 HP% escape_hp | temp_id5 | 超出則用 CFG f_escape_hp=0（FP:105-111） | 0（不逃） |
| 自動住宿 HP%/SP% | temp_id6/7 | -1=略過條件（FS:372-392） | |
| 攻擊類型 | at_type | 1 物理，2 魔法，空則自動 | |

- `temp_id`：0=冒險、1=武鬥（act 1000=挑戰冠軍、1001=PK）、2=禁區（要鑰匙，FP:133-147,238-283）。
- 單機只需實作 `temp_id=0`（可加禁區）。

### 1.2 一場戰鬥（`fire`，FS:17-406）
1. 取玩家資料，條件 `p_act_time < now - f_time`（f_time=2 秒冷卻）且未鎖。
2. 行動力處理，見 §1.8。
3. `hp==0` 則拒絕戰鬥（FS:68-71，必須先住宿）。
4. 抽怪（§2）。
5. `fight_count` 跑完整場戰鬥（一次請求內自動跑完，無逐回合輸入）。
6. 勝利後處理掉落、戰鬥結束技能、事件（§4、§5）。
7. 判斷自動住宿。

### 1.3 屬性（元素）系統（`s_check`，FM:899-920）
```
s_check(diff) 其中 diff = 攻方屬性 - 守方屬性
  diff == -1 → 1.2      diff == +1 → 0.8
  diff == +5 → 1.2      diff == -5 → 0.8
  其他       → 1.0
```
- 相剋環：地(1)→水(2)→火(3)→木(4)→風(5)→毒(6)→地(1)。
- 攻方剋守方（如地打水、毒打地）傷害 ×1.2，被剋 ×0.8。
- 範例：地打水 diff=-1 為 1.2；毒打地 diff=6-1=5 為 1.2；水打地 diff=+1 為 0.8。
- 其餘組合（含同屬性）都是 ×1.0。
- 戰鬥開始：`p[temp_s]=s_check(p屬性-m屬性)`，`m[temp_s]=s_check(m屬性-p屬性)`（FM:527-529）。
- 武器有 `wog_df.d_s` 時會覆蓋玩家屬性（FM:527，FS:96）。
- 怪物屬性 0（無屬性）會被當成 diff=±1 處理（0 對 1 為 1.2/0.8），這是怪物表有無 0 的**未知**。
- 技能 27/58-62「魔劍」可改變攻擊屬性，28/63-67「魔盾」可改變防禦屬性（FK:3593-3690）。技能值 1-6 對應屬性。

### 1.4 傷害公式（`fight`，FM:74-87）
```php
function fight($f_a,$d_d,$temp_s,$def_dmg=1,$get_dmg=0){
  if($d_d==0)$d_d=1; if($f_a==0)$f_a=1;
  $temp_d = (($f_a * ($f_a/$d_d)) - $d_d) * $temp_s;   // = (f_a²/d_d − d_d)·屬性倍率
  if($temp_d <= 0) $temp_d = 1;
  $temp_rd = $temp_d / $f_a;
  $temp_d += rand($temp_rd/3, $temp_rd/2);              // rand 的浮點參數會被截斷成整數
  return round($temp_d * $def_dmg + $get_dmg);
}
```
- 物理：`fight(at*暴擊倍率, 對方df, temp_s, 對方ddef_dmg, 自己d_dmg)`。
- 魔法：`fight(mat*暴擊倍率, 對方mdf, temp_s, 對方mdef_dmg, 自己m_dmg)`。
- `ddef_dmg/mdef_dmg` 是受方的減傷乘數（預設 1，技能 6/7/21 降到 0.9-0.78）。
- `d_dmg/m_dmg` 是攻方附加固定傷害，在減傷乘數**之後**才加（不受減傷影響）。
- 防禦大於攻擊時（f_a < d_d）基礎傷害固定 1，再套 `*def_dmg + get_dmg`。
- 攻防比例是平方關係，攻擊遠高於防禦時傷害暴增。
- 例：at=100、df=50 → (200−50)=150，再加約 150/100/3~150/100/2 的小隨機值（即 +0~+0.75，截斷為整數）。

### 1.5 攻擊方式選擇（`m_type`，FM:88-112）
```
if at>=mat : 80% 物理；其餘 20% 走 else 分支，temp>=5 恆成立 → 也是物理   ⇒ 實際恆為物理
if at< mat : 80% 魔法；20% 物理
```
- 玩家若 POST `at_type` 非空就用指定的（FM:495-502）；怪物每回合都呼叫 `m_type`（FM:610,627）。
- 這是原程式的怪異行為（else 分支內判斷恆真），重製時要決定是否保留。

### 1.6 回合流程（`fight_count`，FM:448-795）
```
迴圈 (hp 皆非 0)：
  sum++ （回合數）
  p_fast = (p.agi > m.agi) ? 70 : 30        -- 玩家先攻機率；agi 相等為 30
  agi_p_rag = p.agi/(p.agi+m.agi)*100 + p.rag      -- 物理命中判定用
  agi_m_rag = m.agi/(p.agi+m.agi)*100 + m.rag
  [冠軍/英雄 p_cp_st：3% 機率額外先打一次]  FM:576-592
  if rand(1,100) < p_fast:  玩家攻擊 → (怪死則勝) → 怪物攻擊 → (玩家死則敗)
  else:                     怪物攻擊 → (玩家死則敗) → 玩家攻擊 → (怪死則勝)
  隊友支援攻擊（機率 10+p_support_m %，且隊友 hp > 10% hpmax）FM:654-672
  寵物出擊（見下）
  if sum >= f_count: 回合超限結算（§1.9）
  自動喝藥判定（§1.7）
  逃跑判定（§1.9）
```
- 每次攻擊前呼叫 `skill_use(type,0)` 施放技能（§3.4）；攻擊後呼叫 `skill_use(type,2,dmg)`（吸血類）。
- 每個行動者的 HP/SP 在回合內持續更新。

**物理攻擊 `fight_p_dm`（FM:209-299，怪物版 FM:300-404）**
```
命中：每下 if (agi_p_rag + 20) > rand(1,100) 才命中（共 at_count 下，預設 1）
暴擊 (物理)：
   cri=10, cri_dmg=1.2
   if 攻方agi > 守方agi:
        cri     = min(20, 攻agi/守agi*10)
        cri_dmg = min(攻agi/守agi*1.2, 2*acrtdmg)
   if rand(1,100) < cri + act:  傷害用 at*cri_dmg，並顯示 p_sat_name（必殺技名）
魔法攻擊（mat_count 下，預設 1）：
   不做命中判定（必中）
   cri 以 au（魅力）比較，公式同上（改用 au/m.au、mcrtdmg、mct）
```
- 玩家 act/mct 是技能提供的額外暴擊率（預設 0）；acrtdmg/mcrtdmg 預設 1（暴傷上限=2×該值）。
- **怪物版有兩個差異**（疑似原程式 bug，須決定是否沿用）：
  - 怪物物理暴擊傷害用 `(p.agi/m.agi)*1.2`（FM:327），比值反了，怪物比玩家快時暴擊倍率會 <1.2。
  - 怪物魔法暴擊判定固定 `rand(1,100) < 10+mct`，不使用算出的 cri（FM:379）。
- m.agi 為 0 時有除零風險（原程式有寫 log 檔的註解痕跡，FM:227-234）。

**寵物（FM:405-446,674-705,942-968）**
- `fight_pet_dm` 在 FM:434 被 `$temp_d=0;` 覆蓋，寵物**永遠打不出傷害**（死碼）。
- 寵物實際功能只有「代擋」：怪物每次命中時，有 `petact`% 機率由寵物承受。
  ```
  petact = min(35, pe_he/255*10 + pe_fi/255*10 + round(p.au/m.au))
  被擋時：pe_def -= 傷害；玩家該次傷害=0；pe_def<=0 則寵物消失
  ```
- 性格 pe_type 1-4 加成 pe_at/pe_mt/pe_def ×1.2、pe_fi ×1.5（FM:503-519）。單機可簡化或略過。

### 1.7 自動喝藥（FM:725-747）
```
條件（全部成立）：
  f_hp(=hpmax*f_hp比例) > p.hp   且   f_sp(=spmax*f_sp比例) > p.sp      ← 是「且」，不是「或」
  且 裝備欄2(d_item_id2)有道具，d_g_hp>0 或 d_g_sp>0，數量>0
  且 本場已喝次數 check_item < 9    且 非傭兵
效果：hp += d_g_hp ; sp += d_g_sp（上限 hpmax/spmax），道具數量 -1
```
- SP 預設門檻 100%，所以實際幾乎只看 HP 門檻（預設 15%）。
- 喝藥在回合末進行，且在回合超限判定之後。
- 結束時把道具數量寫回 DB（FM:765-768）。

### 1.8 行動力 act_num（FS:36-67，CFG:114-115,132-139）
```
每次戰鬥：
  temp = (now - act_num_time)/180            -- act_num_time_週期 180 秒
  if temp>=1: act_num += 6 ; if temp>=2: act_num += 6 ; 上限 50 ; act_num_time = now
  act_num -= 1
  if act_num <= 0:
      f_time = 12 秒（f_time1）
      act_num == -1 : 若距上次行動 < 12 秒則拒絕
      act_num ==  0 : f_down = 1（最後一點仍全額報酬）
      act_num <  -1 : f_down 維持 0.5（報酬減半）
  else: f_down = 1
```
- `f_down` 預設 0.5（CFG:134），影響：
  - 經驗 ×f_down（FM:137），金錢 ×f_down（FM:144）。
  - 職業經驗 `floor(m_job_exp*f_down)`（FS:122）。
  - 掉落 `m_topr *= (f_down+1)`，即分母變 1.5 倍，掉率下降（FS:349）。
- act_num 為負數的下限、是否可持續為負：**未知**（程式沒 clamp）。
- 註：f_down=0.5 下 act_num 消耗與回復細節見上，寫入 DB 在 FM:192/780。

### 1.9 勝/敗/超時/逃跑
**勝利 `win_check`（FM:796-859）**
```
exp   = get_exp(hpmax, hpmax, at, mat, m_lv, p_lv, s=1)      -- 用戰鬥開始快照 $temp_m_st
money = round(get_money(hpmax, at, mat, m_lv) * skill_money_up)   -- skill_money_up 恆為 1
商城 buffer：exp *= buffer.exp ; m_job_exp *= buffer.skill_exp
職業經驗：wog_ch_exp.ch_{ch_id} += m_job_exp ; sk_{ch_id} += m_job_exp
win=1, lost=0
```
- act>=1000（冠軍/PK）時 exp=0、money=0。

**戰敗 `lost_check`（FM:860-898）**：exp=0、money=0、`temp_lost=1`（p_lost+1）、hp=0。**沒有經驗或金錢懲罰**。

**回合超限（FM:706-723）**
```
temp_pd = min(累計傷害, 怪hpmax) ; temp_pd = round(temp_pd/1.5)
exp = get_exp(temp_pd, hpmax, at, mat, m_lv*0.6, p_lv, s=0)    -- act>=1000 則 0
money = 0 ; win=0 ; lost=1   （但不增加 p_lost 計數）
```

**逃跑（FM:748-760,921-941）**
```
if escape_hp(=hpmax*比例) > p.hp:   -- 預設比例 0 所以不會觸發
    escape()：run = 10%（參數傳的是數值，$a[agi] 取不到 → 兩邊都 null，實際恆為 10%）
    成功：exp=0, money=0, win=0, lost=1（不加 p_lost）
```

### 1.10 死亡與住宿
- 死亡只是 `hp=0`；必須先住宿才能再戰（FS:68-71）。
- 自動住宿（FS:372-392）：若 temp_id6/7 > -1，則 `hp <= hpmax*id6% 或 sp <= spmax*id7%` 時住宿；否則只在 `hp<=0` 時住宿。
- 住宿 `wog_act_store::hotel`（class/wog_act_store.php:20-86）：
```
Lv <= 100：免費，hp=hpmax, sp=spmax
Lv >  100：
   slv = hp<=0 ? 2.1 : (hpmax/10 > hp ? 1.8 : 1.4*(hpmax-hp)/hpmax)
   mmoney = round(hotel_money(10) * p_lv * slv) ; 最低 hotel_money*p_lv
   金錢足夠：扣 mmoney，hp/sp 回滿
   金錢不足：免費回滿，但 p_exp = p_exp*0.8（損失 20% 當前經驗）
等待時間 hotel_time = (hp<=0) ? hotel_die_time(15 秒) : hotel_time(5 秒)   → _SESSION.act_time += 該秒數
```
- 註：原程式 `hpmax - p_au*2` 用了查詢中沒有的 `p_au` 欄位（讀到 0，class/wog_act_store.php:25），等於沒作用。
- class/wog_etc_well.php 只是「水井」冠軍展示頁，與死亡無關。

### 1.11 經驗與金錢公式
**`get_exp`（FM:113-138）**
```php
$exp_rand = ($m_at + $m_mat)/2;
$exp_at   = rand($exp_rand, $exp_rand + $m_lv);
if ($p_lv <= 100) $exp_at *= 1.3;
if ($m_lv >= 450) $exp_at *= 0.6;
$exp_at = $m_lv * $exp_at * ($m_lv / $p_lv);
$exp    = $exp_at * ($m_hp/$m_hpmax) * 0.18;     // 勝利時 m_hp=m_hpmax → 比例=1
if ($s == 0) $exp *= ($m_hp/$m_hpmax);           // 超時/部分傷害再乘一次
if ($p_lv <= 15) $exp *= 2.5;
return round($exp * 2 * mercenary_setexp(預設1) * f_down);
```
**`get_money`（FM:139-145）**
```php
$at_total  = ($m_at+$m_mat)/$m_lv;
$at_total2 = ($m_hp/($m_at+$m_mat)) * 0.4;
return rand($m_lv/2.5, $m_lv) * (rand($at_total, $at_total2) * 0.4) * mercenary_money * f_down;
// PHP rand(a,b) 在 a>b 時會對調；浮點參數會被截斷成整數
// win_check 再 round(... * skill_money_up(=1))
```
- 金錢進入 `p_money`（FM:780/186）。

---
## 2. 怪物（wog_monster）

**欄位（由程式推斷，SQL 缺失）**

| 欄位 | 用途 | 出處 |
|---|---|---|
| m_id / m_name / m_img | 識別與圖 | FS:115-234 |
| m_lv | 等級，用於 exp/money | FM:807 |
| hp, sp | 戰鬥用血量/魔力（`hpmax=hp`, `spmax=sp`） | FS:120-121 |
| at, mat, df, mdf | 物攻/魔攻/物防/魔防 | FM 全篇 |
| agi, au | 敏捷（命中、先攻、物暴）/魅力（魔暴、寵物代擋率） | FM:225,264,520 |
| s_property | 屬性 0-6 | FM:528 |
| m_job_exp | 擊殺給的職業經驗 | FM:122,827 |
| d_id, m_topr | 掉落物品 id 清單/掉落分母清單（逗號分隔，一一對應） | FS:347-350，FE:22-52 |
| m_meet | 遭遇門檻（0=必能出現，愈大愈稀有） | FS:115 |
| m_place | 出沒場所 id（0=全場所通用） | FS:110 |
| m_subplace | 子區域位元遮罩（1-4 區） | FS:111-113 |
| m_mission / m_npc / m_npc_message | 任務怪、NPC 怪（不戰鬥只顯示訊息） | FS:128-156 |
| skill_1..5, time_1..5 | 怪物技能 id 與發動機率 | FS:918-952 |

- 怪物 HP/SP 不另外乘以等級，直接取表值。
- 怪物沒有獨立的暴擊欄位。

**選怪（FS:110-117）**
```sql
-- 預設
select * from wog_monster
 where m_place in (<act>,0) and m_meet <= rand(0,100) and m_mission in (<任務怪id>,0 或 =0)
 order by rand() limit 1
-- 有選子區域時：(m_place=0 or (m_place=act and m_subplace & (1<<(sub-1)) ))
```
- `rand(0,100)` 每次查詢只抽一次，所有符合 `m_meet` 的怪（m_meet 愈小愈常入選）再均勻隨機取一隻。
- 所以出現率 = 該怪是否通過 m_meet 門檻 × 在候選池內均勻分佈；**沒有權重欄位**。
- 無怪時顯示「無資料」（FS:395-397）。

**場所列表（CFG:195-202，共 21 個）**
1 中央平原 2 試鍊洞窟 3 黑暗沼澤 4 迷霧森林 5 古代遺跡 6 久遠戰場 7 王者之路 8 幻獸森林 9 星河異界 10 灼熱荒漠 11 無淵洞窟 12 天空之城 13 水晶之間 14 失落古船 15 最果之島 16 冷峰寒地 17 廢棄洞窟 18 日沒碉堡 19 靜止之城 20 黑曜神廟 21 血之魔域。
- 17-20 是「禁區」（js/wog_data.js:108-112），需持有鑰匙（`wog_key_list`/`wog_key_main`，FP:238-283）。
- 每個場所有 4 個子區域（js/wog_data.js:84-106，如中央平原：香茅綠地、綠風草地、白沙海灣、眠獸洞穴）。
- 場所分四大區的下拉選單見 js/wog_fight.js:123-161（中央大陸 1,3,7,13；魔法王國 2,8,12,15；熱帶雨林 4,11,16,9；末日王城 10,5,6,14,21）。
- `wog_area_lv` 只用於登入提示「推薦場所」（class/wog_act_chara.php:743），**不影響選怪**。各場所的建議等級範圍在 SQL 內，未知。

**冠軍/英雄/PK**
- 冠軍（act=1000）：玩家花 `cp_mmoney`=500 挑戰 `wog_cp` 表唯一冠軍；勝利取代冠軍；敗則冠軍得 70% 挑戰金；冠軍 7 天內不可再被挑戰（FS:407-537，CFG:131,138）。戰鬥沒有 exp/金錢。
- 單機通常可略過。
- 英雄 `hero_type`：FS:159-175 的 10% 觸發判斷 `switch($m[hero_type])` 用到不存在的 `$m` 欄位，**實際無效**（死碼）。
- 傭兵（fire_mercenary，FS:711-883）：本研究未深入，**未知**細節。

---
## 3. 技能系統

### 3.1 資料表（欄位由程式推斷）
**`wog_ch_skill`**（所有技能，每個等級一列）：
`s_id`（該等級的唯一 id）、`main_sid`（技能主 id，對應 `skill_<main_sid>()` 函式）、`s_name`、`s_lv`、`s_sp`（消耗 SP）、`s_count`（持續回合數）、`s_st`（施放時機槽：0=攻擊前，2=攻擊後帶傷害；1 未被使用）、`stime`（被動技能的發動機率%）、`type`（0 主動/1 被動）、`need_sid`（前置技能 s_id）、`s_money`（學習費）、`ch_exp`（需要的技能熟練度）、`ch_id`（所屬職業）、`s_master`（0=一般可學）、`s_text`（說明）。
**`wog_skill_book`**：玩家已學技能 `(p_id, s_id, main_sid, s_lv)`。
**`wog_skill_setup`**：玩家裝備欄 `skill_1..5` 與 `time_1..3`（主動技能發動機率）。
**`wog_ch_exp`**：每個職業兩個欄位 `ch_<職業id>`（職業熟練度，用於轉職）、`sk_<職業id>`（可花費的技能熟練度）。

### 3.2 學習與裝備（class/wog_act_skill.php）
- 學習 `skill_get`（:108-175）條件：
  - 尚未擁有該 s_id。
  - 前置技能 `need_sid` 的等級已達標（`skill_book.s_lv >= 前置.s_lv`）。
  - `wog_ch_exp.sk_<ch_id> >= ch_exp`，且金錢 ≥ `s_money`。
  - 成功：扣金錢、**扣除 sk_ 熟練度（`sk -= ch_exp`，是消耗不是門檻）**。
  - 同 main_sid 已有則升級覆蓋（`s_lv`、`s_id`），27、28 號例外可並存多筆（:159）。
  - 學習列表只列「未學過且 `need_sid=0 或 s_lv=1`」或「已學但更高等級」（:32-35）。
- 熟練度顯示換算：`floor(exp/3500*100)%`，即 3500 點=100%（js/wog_job_skill.js:177-182）。
- 裝備 `skill_setup`（:249-328）：
  - 欄位 1-3 只能放主動（type 0），欄位 4-5 只能放被動（type 1，且必須是自己當前職業的技能）。
  - 同 main_sid 不可重複裝備。
  - 發動機率 `time_1..3` 由玩家自訂（可變更發動機率 :226-248）；被動技能用 `stime`。
- 第 6 欄：武器自帶技能 `wog_df.skill`/`stime`，SP 消耗為 0（FS:96-99,908）。
- 熟練度成長：每次**勝利**加 `m_job_exp`（怪物欄位 × f_down × 商城 buffer）到 `ch_<id>` 和 `sk_<id>` 兩個欄位（FM:827）。使用特定道具也能加 sk（class/wog_act_arm.php:1041）。
- 技能等級不會因使用而成長，只能花熟練度買下一級。

### 3.3 `skill_use` 調度（FM:969-1112）
```
type1=玩家 / type2=怪物；type2參數 0=攻擊前、2=攻擊後
1) 先處理進行中的技能（start>0）：start >= s_count → 結束（還原快照 temp_p_st，重套仍有效的 buff）；否則 start++
2) 依序嘗試每個已裝備技能（順序是 SQL ORDER BY RAND()）：
     條件：SP >= s_sp 且 rand(1,100) <= stime 且 該技能 start==0 且 未被 fight_break/skill_break
     扣 SP，呼叫 skill_<main_sid>(&a, lv, uid, 0, dmg)，start=1
     s_count>1 的技能會登記進 status_buffer 供之後每回合重套
     （skill_26 回傳 true 代表失敗但 SP 仍被扣）
3) 重算 p_fast / agi_p_rag / agi_m_rag（因敏捷可能被技能改動）
```
- `$trun`：0=初次發動、1=持續中（有害，對敵方 dot）、2=持續中（增益）（FK:16-21）。
- 持續效果以「快照還原 + 重套」實現：結束時 `$p=$temp_p_st`（回合開頭快照）再重套其他仍在作用的技能。
- 怪物技能的 s_lv 被寫成 `"3LV3"` 字串（FS:936）、部分欄位引用了未定義的 `$p`（FS:942-943），怪物被動技能（skill_4/5）實際上等於失效。

**戰鬥結束技能（class/wog_fight_end_skill.php，FS:259-271）**：79、80、81、84 號在技能發動時被移出戰鬥迴圈，改為勝利後執行。

### 3.4 技能目錄（`skill_<id>`；名稱僅取自程式註解，無註解者標「未知」）
通用：lv 1~N 對應數值表；`a`=施放者。

| id | 名稱 | 效果（FK 行號） |
|---|---|---|
| 0 | 爆氣 | at ×(1+0.01·lv)，lv1-10（:22-63） |
| 1 | 魔力增幅 | mat ×(1+0.01·lv)（:64-105） |
| 2 | 未知（連擊） | at_count=lv+1（2~11 下），at ×(1−0.01·lv)，rag −lv（:106-173） |
| 3 | 急影 | agi ×(1+0.01·lv)（:174-215） |
| 4 | 硬甲之樂 | df ×(1+0.01·lv)（:216-257） |
| 5 | 魔甲之樂 | mdf ×(1+0.01·lv)（:258-299） |
| 6 | 未知（物理減傷） | 受物傷乘數 ddef_dmg=0.98~0.90（lv1-5）（:300-326） |
| 7 | 未知（魔法減傷） | mdef_dmg=0.98~0.90（:327-353） |
| 8 | 未知（攻擊提升） | at ×1.05~1.30（lv1-6）（:354-383） |
| 9 | 嗜血 | 物理攻擊後回復 實際傷害×(0.02·lv)（lv1-5），s_st=2（:384-424） |
| 10 | 未知（治癒） | 回復 smart×(1~2.5)（:425-457） |
| 11 | 未知（物理暴擊率） | act +2·lv（:458-490） |
| 12 | 煙遁 | escape=5·lv（逃跑率，實際戰鬥邏輯未使用，:491-517） |
| 13 | 靡靡之音 | 敵 agi ×(1−0.01·lv)，持續（dot，:518-574） |
| 14 | 未知（附加傷） | d_dmg、m_dmg += au×0.08·lv（:576-606） |
| 15 | 未知（治癒） | 回復 au×(0.4+0.2·lv)（:608-640） |
| 49 | 未知 | act +2·lv，df ×(1−0.03·lv)（:642-684） |
| 16 | 戰神之怒 | rag −75+5·lv（降命中，lv1-6），at += str×0.45（:686-716） |
| 17 | 未知 | at、agi ×(1+0.05·lv)，at_count=2（:718-757） |
| 18 | 未知（獻祭強攻） | at ×1.5，hp 降為 (0.5+0.05·lv)倍，rag +30（:759-793） |
| 19 | 未知（暈眩） | 機率 3·lv%，使對方 fight_break=1（跳過行動），持續（:795-856） |
| 20 | 未知 | 把 df 的 2%·lv 轉為 d_dmg 附加傷害（:858-889） |
| 21 | 反擊 | ddef_dmg=0.92−0.02·lv，at ×(1+0.01·lv)（:891-933） |
| 22 | 仁義 | 損失 hpmax×(2~5%) 換取 sp += sp×(3%·lv)（:935-984） |
| 23 | 未知（魔連擊） | mat_count=lv+1（2~7）（:986-1015） |
| 24 | 未知（冥想） | 本回合不攻擊，回復 spmax×2%·lv SP（:1017-1077） |
| 25 | 未知（減益） | 敵 at、mat ×(1−0.02·lv)，持續（:1079-1144） |
| 26 | 未知 | hp<=30% 時 mat += sp×0.1·lv 並清空 sp，否則失敗（:1146-1191） |
| 27/58-62 | 魔劍 | 把己方攻擊屬性改為 1~6（f_property，:1193-1222,3593-3634） |
| 28/63-67 | 魔盾 | 把己方防禦屬性改為 1~6（d_property，:1224-1253,3636-3688） |
| 29 | 未知 | mct（魔法暴擊率）+lv（:1255-1284） |
| 30 | 劍雨 | 附加魔法傷害 (str+smart)×0.02·lv，需己方 mat>敵方 mat（:1286-1339） |

其餘 31-86 摘要分類：
- **閃避/命中修正類**：31 心眼（己 rag −lv）、33 背襲（對手 rag）、34（暴擊率+40、敏捷系附加傷、rag）、36（5 連擊，rag −70~−20）。
- **控場（使對方該回合不動）**：32（比較 smart，機率 2·lv%）、44 鋒針陷阱（附加傷+暈眩）、55（封鎖對方技能 skill_break）、57（比較 smart 的暈眩+附加魔傷）、68 斯巴達（比較 str）。
- **增益**：35（附加傷 at×0.02·lv）、37（at += vit×0.03·lv）、39 力量之舞（at += au×0.05·lv）、40 魔力之舞（mat += au…）、41 敏捷之舞（agi += au…）、42 毀滅之舞（act、mct +lv）、43 友情之舞（隊友支援率 +2·lv）、52 精靈之力（mat ×1.03~1.26）、53 屬性迫害（強化屬性相剋倍率）、73 豪鬼、74 煉獄、75 熱情律動、76 華爾滋（皆為暴擊率/暴傷相關）、77 毒傷陷阱、82 石人陣（df + 固定值 150~2430）、86 獸騎（agi + 固定值 150~2430）。
- **減益/DoT**：50（敵 hp 降為 (0.9−0.01·lv)倍並降防）、51 暴雪（附加魔傷+降敵 agi）、54（敵 sp 扣 20~2000）、69 壓制（敵 agi、df 降）、70 施毒、71 明王（降敵 df）、78 正義火雷（敵 agi −200~4900）、83 黑渦（敵 au −200~4900）。
- **回復/犧牲**：38 犧牲（扣 10% hpmax 換 d_dmg、rag+60、act+10）、56（耗寵物 def 回復己方 HP）、72 復仇（落後時回 vit×… HP 並加 rag/at）。
- **寵物相關**：45 物攻/魔攻命令、46 戰意驅使、47 狂性連結、48 魔性連結、85 閃電痛擊（寵物 pet_act）。寵物不輸出傷害（§1.6），這些在單機可忽略。
- **戰鬥結束技能**：79 神頌曲（隊友回血 au×0.1·lv）、80 地獄禮讚（吸取隊友 HP 回自己）、81 詐騙（偷隊友金錢）、84 精密陷阱（提高捕寵率）；單機無隊友可忽略。84 號的 switch 缺 break，任何等級都得到 56（class/wog_fight_end_skill.php:220-255）。
- 技能 31-86 的完整數值表需逐個查看 FK 對應行（範圍見 `grep -n "^function skill_" class/wog_fight_skill.php`）。各技能所屬職業、SP、s_count、stime 全在 DB，**未知**。

---
## 4. 掉落（FE:22-128，FS:347-350）

```
for 每個 (item_id, topr) 配對：
   top_get = topr * topr_vip             -- topr_vip：預設 0.9；p_st=1→0.8；p_st=3→0.7（VIP 狀態）
   (傭兵加成：top_get = ((topr*(100-x))/100 + topr)*topr_vip)
   if rand(1,top_get) <= 1 → 掉落          -- 機率≈1/top_get（浮點會被截斷；top_get<1 時 rand 對調）
行動力不足(f_down<1)：m_topr ×1.5（FS:349）
```
- 道具進背包限制：裝備類上限 `item_limit`=15；道具類（d_fie>4）上限 `item_limit + p_bag`；滿了則丟失並提示（FE:77-111）。
- 掉落以 `wog_item.d_item_id` 等逗號清單儲存，由 `wog_item_tool::item_in` 處理疊加（上限 99，CFG:106）。
- 其他勝利後事件（FS:241-370）：
  - 市場漲跌：勝利時 `rand(1,10500-x)==1`，x 為 `(日%3 ? 20 : -190)`，75% 機率漲/25% 跌，變動幅度 0.xx~5.99%（FE:129-212）；失敗時 `rand(1,310-x)==1`。
  - 單機可略。
  - 捕寵：持有道具 258/1837/2236 且無寵物：1/40 機率道具損耗；`rand(1,100)<=12+get_pet` 時抓到寵物（最多 3 隻）。
  - 任務擊殺計數（`wog_mission_book`）。

---
## 5. 隨機事件（FE）

**沒有寶箱、也沒有問答題事件。** `event_ans`（CFG:216）實際是**防外掛驗證碼**：
```
每場戰鬥開始：if rand(1,310)==1 (event_ans_max=310) → event_creat：p_attempts=1，前端跳出驗證碼圖
若 p_attempts 已有值：event_start：p_attempts>6 → 鎖帳號(p_lock=1)，48 小時解鎖；否則 p_attempts++ 並再次要求
玩家答對（wog_act.php:692-734）→ p_attempts=0
```
- 驗證碼是 5 碼英數（無數字 0），js/wog_etc_view.js:160-175。
- 單機重製可整個略過。
- 其餘「事件」只有勝利後的市場漲跌（§4）與捕寵。

---
## 6. 重製要點與未知/疑似 Bug 清單
1. 單場戰鬥是「一次請求跑完整場」，所以單機可用純函式模擬：輸入玩家/怪物狀態，回傳戰鬥日誌與結算。
2. 疑似原程式 bug，需決定是否保留：
   - m_type 的 else 分支恆為物理（at>=mat 時永遠物理）。
   - 怪物物理暴擊倍率比值反向（FM:327）。
   - 怪物魔法暴擊判定忽略算出的 cri（FM:379）。
   - 寵物不輸出傷害（FM:434）。
   - 英雄加成無效（FS:161）。
   - 逃跑機率恆 10%（FM:921-941）。
   - 住宿 `p_au` 為 0（act_store:25）。
   - end_skill_84 缺 break。
   - 怪物技能 lv/stime 錯誤（FS:936,942）。
3. 未知：SQL 缺失所以以下全未知——
   - 所有怪物資料（數值、掉落、m_meet、場所分配）。
   - 各職業升級成長範圍 `ch_*`。
   - 技能完整名稱/SP/s_count/s_st/stime/學習費用/需求熟練度。
   - wog_df 武器/防具數值。
   - `wog_area_lv` 各場所等級區間。
   - act_num 下限。
   - 怪物 s_property=0 的處理方式。
4. 命中判定只有物理攻擊有（`agi比例×100 + rag + 20 > rand(1,100)`）；魔法必中。

---
**最終訊息摘要（10 行）**
1. 報告路徑：無法寫入檔案（唯讀、無寫檔工具），全文見上方，請主 agent 存到 /private/tmp/claude-501/-Users-ethan-Project-WOG4-5-Releas-CT/003ddd97-3a93-4c5f-8bfb-54308c7fa626/scratchpad/wog-research-battle.md。
2. 傷害：`((A²/D) − D)×屬性倍率`（≤0 則 1）＋微小隨機，再 ×受方減傷乘數、＋攻方附加傷（FM:74-87）。
3. 衍生值：at=武器+str、df=防具+vit、mat=武器+smart、mdf=防具+be；hpmax=life×25+50、spmax=(smart+be)×3+25（function.php:227-434）。
4. 先攻：玩家 agi 較高則 70% 先攻、否則 30%；物理命中=(agi 比例×100+rag+20)>rand(1,100)；魔法必中（FM:565-575,221,259）。
5. 暴擊：基礎 10%、×1.2；agi（魔法為 au）較高時機率最高 20%、倍率最高 2×；怪物暴擊公式有反向 bug。
6. 屬性：地→水→火→木→風→毒→地，剋制 ×1.2、被剋 ×0.8（FM:899-920）。
7. 經驗：`m_lv²/p_lv × (at+mat)/2(隨機) × 0.18 × 2 × f_down`，Lv≤100 ×1.3、Lv≤15 再 ×2.5（FM:113-138）；金錢見 FM:139-145。
8. 回合上限 40；死亡無經驗/金錢懲罰，但必須住宿（Lv≤100 免費、Lv>100 收 10×Lv×係數，付不起則損 20% exp，等待 5/15 秒）。
9. 行動力每 3 分鐘 +6（上限 50），歸零後報酬 ×0.5、冷卻 12 秒；掉率=1/(m_topr×0.9)。
10. 最大未知：怪物表、職業成長、技能完整名稱與 SP/機率、武器數值（SQL 缺失）。隨機事件其實只是驗證碼，沒有寶箱或問答。