# 同伴總表（計畫 9）

寵物、傭兵、勳章所與冠軍用到的內容：勳章與捕捉道具在 `backend/content/items.json`，勳章掉落在 `monsters.json`，傭兵目錄在 `mercenaries.json`，勳章所在 `honour-shop.json`，NPC 冠軍種子在 `champion.json`。WOG 的傭兵、勳章所、冠軍資料表都已遺失，數值是 Claude 的設計（F 級傭兵取自 WOG 傭兵所的說明文字）。測試 `Tests/Content/CompanionTableTests.cs` 與 `MonsterRosterTests.Roster_EliteMonstersDropTheirAreaMedal` 釘住下列數值；改內容時本檔與內容檔一起改。

## 載入驗證

`JsonGameContent` 依序載入道具、怪物掉落、任務之後，再驗證傭兵、勳章所、冠軍種子；三個檔案都是必要內容檔。每條規則都有 `JsonGameContentTests` 的拒絕測試，錯誤訊息帶 id。

- 捕捉道具：分類必須是 `Consumable`、`consumable Capture`、`amount ≥ 1`，不得有 supplyHp／supplySp／bonus。
- 勳章：分類 `Medal`、`consumable None`，其餘同材料（不能有需求、孔、精鍊上限）。
- 傭兵：id 不重複且 1～32 字、name 1～8 字（WOG 輸入框 maxlength 8）、price 為 null 或 ≥ 1、fights ≥ 1、moneyPercent／dropPercent／expPercent 1～100、deathCost ≥ 1。
- 鑰匙（計畫 10）：規則見 `item-table.md` 的「鑰匙」。
- 勳章所：id 不重複且 1～32 字、itemId 與 mercenaryId 恰好一個且存在、道具數量 1～99、傭兵數量必須 1；cost 至少一項，每項是存在的勳章、數量 1～99、不重複。
- 冠軍種子：必須有一筆；name 1～12 字、specialAttackName 不可為空、level ≥ 1、element 0～6、hp ≥ 1、sp ≥ 0、at／mat／df／mdf／agi／au ≥ 0；載入後 `Hp = HpMax`、`Sp = SpMax`。

## 勳章

背包分類 `Medal`（15 格、不能擴充、可堆疊 99），名稱出自 `js/wog_data.js:197-202`；`price 0`、不在商店販賣。

| id | 名稱 | WOG id | 大區 |
|---|---|---|---|
| `continent-medal` | 大陸勳章 | 1304 | 中央大陸 |
| `kingdom-medal` | 王國勳章 | 1305 | 魔法王國 |
| `rainforest-medal` | 雨林勳章 | 1306 | 熱帶雨林 |
| `royal-city-medal` | 王城勳章 | 1307 | 末日王城 |
| `silver-medal` | 白銀勳章 | 2222 | 禁區 |

## 勳章掉落

每隻 `meet 60` 的非全域怪（14 隻地區精英＋3 隻任務怪）在 `drops` 最後加上所屬大區的勳章 `topr 60`（上表「大區」欄）；其他怪沒有勳章。WOG 的掉落資料遺失，研究稱勳章來自 Boss 掉落與任務（`wog-research-world.md`），這是 Claude 的設計。各怪的完整掉落見 `monster-roster.md`。

## 捕捉道具

放在補給欄（WOG 的道具欄 2），`ConsumableKind.Capture`，`amount` 是捕到的寵物 AT／MT／DEF 的倍率（`rand(1,10) × 倍率`）。勝場、沒有攜帶寵物、數量 > 0 時才擲：`rand(1,40) == 1` 道具損壞 −1；還有道具時 `rand(1,100) ≤ 12` 捕捉成功（寵物未滿 3 隻才建立並扣 1 個）。捕捉道具不會被當成補給品喝；傭兵戰鬥不捕捉。

| id | 名稱 | 倍率 | 價格 | 商店等級 | 取得 |
|---|---|---|---|---|---|
| `capture-net` | 捕獸網 | 1 | 200 | 1 | 商店 |
| `capture-cage` | 捕獸籠 | 8 | 3000 | 4 | 商店 |
| `capture-crystal` | 封印水晶 | 13 | 5000 | 6 | 勳章所（不在商店販賣） |

倍率取自 WOG 道具 258、1837、2236（`pet_stats`）。

## 餵食選單

同 WOG 寫死在程式裡的選單（`wog_act_pet.php`），放在 Domain 常數，不是內容檔。每次餵食飽腹先擲、親密度 +`rand(2,6)` 後擲。VIP 果實 2319～2322 是商城道具，不做。

| id | 名稱 | 效果 | 價格 | 飽腹 |
|---|---|---|---|---|
| `fruit-vigor-small` | 初級活力果實 | AT +2 | 20 | +`rand(10,20)` |
| `fruit-aroma-small` | 初級香氣果實 | MT +2 | 20 | +`rand(10,20)` |
| `fruit-sweet-small` | 初級甜味果實 | DEF +2 | 20 | +`rand(10,20)` |
| `fruit-wild-small` | 初級野性果實 | 出擊值 +2 | 20 | +`rand(10,20)` |
| `fruit-vigor-medium` | 中級活力果實 | AT +4 | 50 | +`rand(18,28)` |
| `fruit-aroma-medium` | 中級香氣果實 | MT +4 | 50 | +`rand(18,28)` |
| `fruit-sweet-medium` | 中級甜味果實 | DEF +4 | 50 | +`rand(18,28)` |
| `fruit-wild-medium` | 中級野性果實 | 出擊值 +4 | 50 | +`rand(18,28)` |

## 傭兵

同時只能僱用一位；`price` 為 null 的只能在勳章所兌換。金錢%、經驗% 是打折比例（`round(值 × % / 100)`）；寶物% 讓掉落分母 ×`(2 − 寶物% / 100)`；死亡消耗是敗北或超限時扣的次數。

| id | 名稱 | 價格 | 戰鬥次數 | 金錢% | 寶物% | 經驗% | 死亡消耗 |
|---|---|---|---|---|---|---|---|
| `mercenary-f` | F級傭兵 | 3000 | 100 | 30 | 30 | 65 | 5 |
| `mercenary-e` | E級傭兵 | 12000 | 150 | 40 | 40 | 70 | 4 |
| `mercenary-d` | D級傭兵 | 40000 | 200 | 50 | 50 | 75 | 3 |
| `mercenary-c` | C級傭兵 | 120000 | 300 | 60 | 60 | 80 | 2 |
| `mercenary-b` | B級傭兵 | 勳章所 | 400 | 70 | 70 | 85 | 1 |
| `mercenary-a` | A級傭兵 | 勳章所 | 500 | 80 | 80 | 90 | 1 |

## 勳章所

一筆兌換是「一件道具 × 數量」或「一位傭兵」，花費只能是勳章；目前每筆數量都是 1。WOG 的鑰匙類（h_type 2）是計畫 10 加入的四把禁區鑰匙，寶箱與書（計畫 10，見 `item-table.md`）也在這裡兌換；榮譽裝備（h_type 3～7）資料遺失，不做。

| id | 獲得 | 花費 |
|---|---|---|
| `honour-mercenary-b` | B級傭兵 | 大陸 ×3、王國 ×3、雨林 ×3、王城 ×3 |
| `honour-mercenary-a` | A級傭兵 | 白銀 ×5、大陸 ×3、王國 ×3、雨林 ×3、王城 ×3 |
| `honour-capture-crystal` | 封印水晶 ×1 | 白銀 ×1、雨林 ×2 |
| `honour-attack-stone` | 攻擊魔石 ×1 | 大陸 ×2 |
| `honour-sorcery-stone` | 魔力魔石 ×1 | 王國 ×2 |
| `honour-guard-stone` | 守護魔石 ×1 | 雨林 ×2 |
| `honour-ward-stone` | 結界魔石 ×1 | 王城 ×2 |
| `honour-vitality-stone` | 生命魔石 ×1 | 大陸 ×1、雨林 ×1 |
| `honour-gale-stone` | 疾風魔石 ×1 | 王國 ×1、王城 ×1 |
| `honour-chaos-stone` | 混沌魔石 ×1 | 白銀 ×2 |
| `honour-prism-stone` | 虹彩魔石 ×1 | 白銀 ×2 |
| `honour-iron-refine-5` | 鋼鐵精鍊石 Lv5 ×1 | 大陸 ×1、王國 ×1、雨林 ×1、王城 ×1 |
| `honour-arcane-refine-5` | 秘法精鍊石 Lv5 ×1 | 大陸 ×1、王國 ×1、雨林 ×1、王城 ×1 |
| `honour-vigor-refine-5` | 活力精鍊石 Lv5 ×1 | 大陸 ×1、王國 ×1、雨林 ×1、王城 ×1 |
| `honour-key-abandoned-cave` | 廢棄洞窟鑰匙 ×1 | 大陸 ×1、王國 ×1、雨林 ×1、王城 ×1 |
| `honour-key-sunset-fort` | 日沒碉堡鑰匙 ×1 | 大陸 ×2、王國 ×2、雨林 ×2、王城 ×2 |
| `honour-key-still-city` | 靜止之城鑰匙 ×1 | 白銀 ×1、大陸 ×2、王國 ×2、雨林 ×2、王城 ×2 |
| `honour-key-obsidian-temple` | 黑曜神廟鑰匙 ×1 | 白銀 ×3、大陸 ×2、王國 ×2、雨林 ×2、王城 ×2 |
| `honour-stone-box` | 魔石寶箱 ×1 | 大陸 ×1 |
| `honour-large-stone-box` | 大魔石寶箱 ×1 | 白銀 ×1 |
| `honour-refine-box` | 精鍊寶盒 ×1 | 王國 ×1、雨林 ×1 |
| `honour-pet-egg` | 寵物蛋 ×1 | 白銀 ×3 |
| `honour-job-notes` | 職業心得書 ×1 | 大陸 ×1、王城 ×1 |
| `honour-advanced-job-notes` | 高級心得書 ×1 | 白銀 ×2 |
| `honour-exp-capsule` | 經驗膠囊 ×1 | 雨林 ×1、王城 ×1 |

## 冠軍種子

每個角色一個冠軍席位；沒有紀錄時是這位 NPC 冠軍（滿 HP／SP）。數值依戰士 Lv30 期望值＋商店 4 級裝備估算。冠軍沒有技能，`SpecialAttackName` 只在暴擊時顯示。

| 名稱 | 等級 | 屬性 | 必殺 | hp | sp | at | mat | df | mdf | agi | au |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 王都冠軍 格雷德 | 30 | 1 地 | 王者之劍 | 1400 | 175 | 180 | 25 | 150 | 40 | 30 | 25 |

## 與研究報告的差異

- **捕捉道具在道具欄 2**：spec 說的「副手」就是 WOG 的 `d_item_id2`（道具欄 2），這裡是補給欄，不是另一個裝備格。
- **傭兵寶物% 是懲罰**：PHP `get_item` 把掉落分母乘上 `(2 − 寶物% / 100)`，寶物 30% 等於分母 ×1.7；WOG 介面說「打寶率為原來的 30%」與公式不符，照公式，介面仍顯示 WOG 的欄位值。
- **傭兵戰鬥條件**：傭兵用角色當下的能力滿 HP／SP 出戰、不用補給；遇敵只看該地區自己的怪（不含全域怪）；職業熟練度全額（PHP `win_check` 不打折）；40 回合超限算敗（PHP 超限 `win=0`），扣死亡消耗。
- **飢餓**：只扣攜帶中的寵物，每次推進（WOG 的線上心跳）最多一次，離線一整天回來也只扣一次。
- **餵食上限**：DEF 上限取主人 df、mdf 較大者；出擊值沒有上限（照 PHP）。
- **冠軍鎖定**：PHP 把 7 天鎖在被推翻的冠軍主人身上；單人版被推翻的永遠是自己的席位，所以鎖在自己的席位上。
- **任務寵物獎勵上限**：PHP 給任務寵物不檢查 3 隻上限；這裡寵物已滿就整筆拒絕（`petsFull`）。
- **交寵物要攜帶中的**：每日任務交寵物時，攜帶中的寵物必須是指定物種（PHP `pe_st=0 and pe_m_id`）。
- **勳章名稱**：出自 `js/wog_data.js:197-202`；勳章的掉落來源資料遺失，改由精英怪掉落（見「勳章掉落」）。
