# WOG 翻新版裁定彙整

- 日期：2026-10-22（計畫 10 Task 12 整理）
- 用途：把散在計畫 3～11 裡的〔裁定〕與〔Claude 的判斷／設計／簡化〕集中成一份，之後要推翻或調整時先查這裡，再回到出處看完整規則。
- 規則本文以出處為準；這裡每條只寫一行摘要。出處格式「計畫 N・段落（第幾行）」，行號對應 `docs/superpowers/plans/` 裡該計畫檔在本文件建立時的內容。
- 標記沿用設計文件：〔裁定〕是計畫撰寫或執行時對歧義的決定；〔Claude 的判斷〕可推翻；〔Claude 的設計〕是 WOG 資料遺失時自訂的數值或內容；〔Claude 的簡化〕是刻意比 WOG 少做的部分。
- 還沒處理的延後項目另見 `2026-10-23-deferred-backlog.md`。

## （一）計畫中標為〔裁定〕的規則

| # | 規則 | 出處 |
|---|---|---|
| 1 | 連打時每一下各一個 `AttackEvent`，平均分配在攻方的半回合內（間隔 = 0.4 秒 ÷ 下數，最後一下早於對手出手）；原訂每下 +0.1 秒會超出半回合 | 計畫 5・Global Constraints（第 48 行，執行時裁定） |
| 2 | 暈眩讓怪物跳過出手時產生 `AttackEvent(Hit=false, Damage=0, Kind=Physical, Stunned=true)`，前端文案「{怪物}暈眩中，無法行動。」 | 計畫 5・Global Constraints（第 48 行） |
| 3 | 選怪沒有候選時，退回該地區的全部怪物 | 計畫 6・Global Constraints（第 17 行，沿用計畫 5 的裁定） |
| 4 | 移動前先把舊地區推進到現在（`AdventureService.AdvanceAsync`），再寫入新位置，舊地區的戰鬥不會算到新地區 | 計畫 6・Task 5 世界 API（第 162 行） |
| 5 | 綁定物可以存取倉庫（WOG 的綁定只擋贈送） | 計畫 7・Task 4 商店、銀行、倉庫（第 156 行） |
| 6 | 推進摘要的 `Drops` 只計入真正放進背包的掉落，被丟棄的不算 | 計畫 7・Task 5 掉落（第 171 行） |
| 7 | 同一隻怪是多個進行中任務的目標時，每個任務都 +1（PHP 以怪物 id 為 key 只算到一個，當成實作缺陷） | 計畫 8・Global Constraints（第 19 行）；自我檢查（第 501 行） |
| 8 | 每日任務以 Asia/Taipei 09:00 重置（固定位移 +08:00，即每天 01:00 UTC）；WOG 用伺服器本地時間 `mktime(9,0,0)`，原服在台灣 | 計畫 8・Global Constraints（第 23 行） |
| 9 | 捕捉道具放在補給欄（WOG 的 `d_item_id2`＝道具欄 2，spec 說的「副手」就是這一格） | 計畫 9・Global Constraints（第 18 行） |
| 10 | 傭兵的寶物% 照 PHP 公式：掉落分母 ×`(2 − 寶物% / 100)`（寶物 30% 等於分母 ×1.7）；WOG 介面說「打寶率為原來的 30%」與公式不符，介面仍顯示 WOG 的欄位值 | 計畫 9・Global Constraints（第 23 行） |
| 11 | 冠軍被推翻後 7 天（604 800 秒）的鎖定放在被推翻的席位上；PHP 鎖在冠軍主人身上，單人版被推翻的永遠是自己的席位 | 計畫 9・Global Constraints（第 28 行） |

計畫 6 另有三條執行時或終審的數值裁定，一併列出：

- 怪物公式初版（at 10 + 3.2L、mat 6 + 2.6L、df 2 + 1.0L、法 mat ×1.5）讓 Lv1 戰士一下擊倒同級怪，微調為現行公式（計畫 6・Global Constraints 第 29 行，執行時裁定；校準見 `docs/research/monster-roster.md`）。
- 全域怪 `meet` 由 80 改 95，高等地區不該有約 14% 的 Lv1／Lv5 遭遇（同上，終審裁定）。
- 每個子地區至少有一隻 `meet 0` 的怪（同上，終審裁定）。

## （二）計畫 3～9 的〔Claude 的判斷／設計／簡化〕

以 `grep -n "Claude 的\(判斷\|設計\|簡化\)" docs/superpowers/plans/2026-10-2*-plan-[3-9]*.md` 蒐集，共 22 行命中；一行裡有多條的分開列。絕大多數在 Global Constraints，少數在任務段落（出處有註明）。

### 計畫 3、3b

grep 沒有命中。計畫 3 的主題與創角規則依 spec §3、§10，判斷記在設計文件本身。
- 2026-11-09 Spectra 變更 `title-and-create-screens` 推翻 §10 的色票與字型（新畫面的變數與字型見專案 `CLAUDE.md` 前端硬規則 3、4，舊畫面在替換前沿用）；§3.2 創角規則不變，只有能力點數改成必須剛好分完 10 點（原本照 WOG 允許沒分完）。

### 計畫 4：WOG 戰鬥與成長

- 〔判斷〕推進 API 模擬到現在的所有戰鬥，只輸出最後 30 秒（`CatchUpWindowSeconds`）的事件、其餘彙總成 `AdvanceSummary`，單次最多模擬 24 小時；取代 v1 的「統計估算」，因為 WOG 戰鬥計算很便宜。— 計畫 4・Global Constraints（第 43 行）

### 計畫 5：職業與技能

- 〔判斷〕戰鬥中依技能欄位 1→5 的固定順序擲施放，而不是隨機順序，讓固定種子可以重現。— 計畫 5・Global Constraints（第 18 行）
- 〔設計〕技能每級數值：主動 `sp = 3·lv + 2`、發動率 `30 + 10·lv`、持續回合 1 或 3；被動常駐，sp／回合／發動率都是 0；學習金錢 `500·lv²`、熟練度 `300·lv`；WOG 的資料庫值已遺失。— 計畫 5・Global Constraints（第 40 行）；2026-10-24 改為主動固定發動率、被動常駐（原為被動發動率 `20 + 10·lv`、主動由玩家自訂）
- 〔裁定 2026-10-24〕被動技能常駐（不擲骰、不扣 SP、沒有持續回合），任何職業學過的被動都能放進欄位 4～5，轉職不再清被動欄；主動技能的發動率由技能等級固定，`SkillSlot` 不再存發動率（migration V13 移除 `character_skill_slots.chance`）。— 設計文件 §5.2、§6
- 〔裁定 2026-10-25〕技能加冷卻（每級 `cooldown`，玩家出手回合計、發動回合不算、每場歸零）與發動條件（技能層級 `trigger`：`chance` 或 `hpBelow` 門檻 1～99%，hpBelow 不擲骰、chance 必為 0）。治癒改為 hpBelow 50、冷卻 4。— 設計文件 §6
- 〔裁定 2026-10-25〕被動分常駐型（無 trigger，sp／rounds／chance 皆 0）與觸發型（時機＋chance 或 hpBelow，效果持續該級 rounds ≥ 1，不扣 SP、可冷卻）；主動技 SP 不足時不發動也不進冷卻；冷卻以回合計，連擊一回合只算一次出手。— 設計文件 §6

### 計畫 6：世界

- 〔設計〕21 個地區的推薦等級表（`wog_area_lv` 遺失），最大值兼作怪物等級上限。— 計畫 6・Global Constraints（第 18 行）
- 〔設計〕怪物生成公式與角色倍率（普通／坦／速／法／精英），依 WOG 傷害公式 `A·(A/D)−D` 校準為同級 3～6 下擊殺、每下受傷約 HP 的 3～6%。— 計畫 6・Global Constraints（第 29 行）
- 〔判斷〕自動住宿：開打前 `hp < 15%` 或 `sp < 15%` 就住宿，SP 條件只在有裝備技能時才看（計畫 10 改為玩家可設定，預設值不變）。— 計畫 6・Global Constraints（第 31 行）

### 計畫 7：道具與經濟

- 〔簡化〕打洞成功時改同一件裝備的 `Holes + 1`，不換成另一個道具 id。— 計畫 7・Global Constraints（第 21 行）
- 〔設計〕裝備（依商店等級 L 的 at／df／需求／價格／精鍊上限／孔數）、補給品、精鍊石的數值公式；WOG 資料遺失，測試釘住區間，見 `docs/research/item-table.md`。— 計畫 7・Global Constraints（第 24 行）

### 計畫 8：任務

- 〔判斷〕同怪多任務全部都算（即（一）第 7 條）。— 計畫 8・Global Constraints（第 19 行）
- 〔簡化〕擊殺任務只有一個目標；WOG 支援 `id*n,id*n` 多目標，但現存 8 個模板都是單一目標。— 計畫 8・Global Constraints（第 19 行）
- 〔判斷〕任務經驗獎勵加完立刻升級；PHP 只加 `p_exp`，下一場戰鬥才升級。— 計畫 8・Global Constraints（第 22 行）
- 〔判斷〕技能獎勵遇到已會的技能時略過技能、其餘照給，回應 `SkillGranted = false`；PHP 會報錯卡死任務。— 計畫 8・Global Constraints（第 22 行）
- 〔設計〕一般任務內容（WOG 主線全部遺失）：金錢 `50 × level`、經驗 `190 × level`，見 `docs/research/mission-table.md`。— 計畫 8・Task 1（第 97 行）

### 計畫 9：寵物、傭兵、榮譽、冠軍

- 〔判斷〕同時只攜帶 1 隻寵物，攜帶另一隻時原本攜帶的自動放回獸欄；WOG 是拒絕。— 計畫 9・Global Constraints（第 17 行）
- 〔判斷〕捕捉道具因損壞歸零時不再擲捕捉；PHP 會以 −1 個繼續捕捉。— 計畫 9・Global Constraints（第 18 行）
- 〔判斷，來自 spec〕恢復寵物傷害；WOG 在 `wog_fight_m.php:434` 把寵物傷害歸零。— 計畫 9・Global Constraints（第 19 行）
- 〔設計〕推進時先跑傭兵時間軸、再跑角色自己的冒險，同一筆交易寫入；掉落「傭兵的在前、冒險的在後」。— 計畫 9・Global Constraints（第 24 行）
- 〔設計〕勳章來源：各地區 `meet 60` 的非全域怪加掉所屬大區的勳章 `topr 60`；WOG 資料遺失，研究稱來自 Boss。— 計畫 9・Global Constraints（第 25 行）
- 〔簡化〕榮譽裝備（`h_type 3～7`）資料遺失、不做。— 計畫 9・Global Constraints（第 26 行）
- 〔判斷〕任務寵物獎勵在寵物已滿時回 400 `petsFull`、整筆不寫；PHP 不檢查上限。— 計畫 9・Global Constraints（第 27 行）
- 〔設計〕每個角色一個冠軍席位；多人共用的冠軍是非同步 PvP，屬 spec §1 非目標。— 計畫 9・Global Constraints（第 28 行）
- 〔簡化〕冠軍沒有技能，因為怪物側沒有技能引擎。— 計畫 9・Global Constraints（第 28 行）
- 〔設計〕可以挑戰自己過去的快照；WOG 以名字禁止挑戰自己，單人版禁止就等於只能打一次。— 計畫 9・Global Constraints（第 28 行）
- 〔判斷〕前端：底部第 6 個分頁「同伴」（子頁寵物、傭兵），勳章所是背包的子頁，冠軍是冒險頁地點標題旁的按鈕。— 計畫 9・Global Constraints（第 31 行）
- 〔設計〕傭兵目錄數值（F 級取自 WOG 傭兵所的說明文字），見 `docs/research/companion-table.md`。— 計畫 9・Task 1（第 116 行）
- 〔設計〕每日交寵物任務的報酬池：WOG 道具 1422 換成捕獸網。— 計畫 9・Task 8（第 633 行）

## （三）計畫 10 的判斷與裁定

收錄規則：A 只收計畫 10 Global Constraints 裡有〔…〕標記的條目；B 只收 SDD 帳本記為裁定（`Ruling(s):` 行，以及審查接受的實作者裁定）的條目，理由與代價取自各任務報告。未加標記的計畫規則（例如平衡基準角色）以計畫本文為準，不重複列。

### A. Global Constraints 中標註的判斷

- 〔使用者要求〕每個任務完成後都經過 test-runner（獨立跑 `./scripts/check.sh`）與 reviewer。— Global Constraints 第 1 條
- 〔判斷〕補給與住宿門檻範圍 −1～100，−1 表示略過該條件（WOG 住宿欄的寫法，這裡也用在補給）；WOG 補給 SP 預設 100% 時 SP 全滿永遠不喝，給玩家關掉 SP 條件的方法。— 冒險設定
- 〔判斷〕門檻超出範圍回 400 `invalidPercent`；WOG 是靜默改回預設。— 冒險設定
- 〔設計〕四把禁區鑰匙都是 21 600 秒；WOG `wog_key_main.area_time` 遺失。— 禁區鑰匙
- 〔簡化〕通行證到期後推進只模擬到到期時刻，不自動再扣鑰匙；WOG 每次出戰都會自動再扣，放置版會把離線時間一口氣吃掉多把鑰匙。— 禁區鑰匙
- 〔設計〕新消耗品（魔石寶箱、精鍊寶盒、寵物蛋、心得書、經驗膠囊、密藥）的名稱與數值；WOG `wog_df_used` 遺失。內容表在 Task 5（第 287 行）、Task 6（第 355 行），見 `docs/research/item-table.md`。— 新消耗品、密藥
- 〔簡化〕心得書只加**目前職業**的技能點；WOG 綁定 `ch_id`。— 新消耗品
- 〔判斷〕經驗膠囊加完立刻升級；WOG 只加 `p_exp`，下一場勝利才升級。— 新消耗品
- 〔判斷〕密藥只作用於角色自己的冒險，傭兵與冠軍挑戰不吃。— 密藥

### B. 執行中的裁定

每條一行：規則 — 理由 — 若判斷錯的代價。

**Task 1：docker compose**

- 健康檢查的資料庫狀態經 Application 的 `IDatabaseStatus`＋Infrastructure 的 `EfDatabaseStatus` — Api 直接注入 DbContext 會遇到 EF Core 版本不一致而 500，也維持 Api 不碰 EF 型別的分層 — 多一層介面，沒有行為代價。
- 多寫一條資料庫連不上時回 503 的測試 — 503 分支是介面的一部分 — 多一條測試。
- `web` 的 healthcheck 打 `127.0.0.1` 而不是 `localhost` — 容器裡 `localhost` 先解析成 `::1`，nginx 只聽 IPv4 — 若主機只有 IPv6 則 healthcheck 失敗（目前沒有這種部署）。
- README 的備份指令加 `--no-tablespaces` — 遊戲帳號沒有 PROCESS 權限，MySQL 8 的 mysqldump 不加會報錯 — 無。

**Task 2：測試穩定**

- 測試容器的 label 用 `ebo.tests=true`（依 brief） — 清理腳本只刪這個 label — 無。
- 前端測試共用模組層級的 `userEvent` 實例 — 減少 `CreateCharacterScreen` 在負載下逾時 — 測試之間可能共享狀態而互相干擾。
- 容器啟動逾時的訊息照 brief — 讓卡住時一眼看出原因 — Testcontainers 自己的逾時也會被改寫成同一句，除錯時可能誤導。

**Task 3：冒險設定與逃跑**

- 補給門檻放在 `SupplyState` 帶進戰鬥 — 補給判斷只在 Domain 一處 — 無。
- 逃跑在第 2 回合起、每回合開頭喝完補給後判定；`EscapeEvent` 的時刻是該回合開頭，推進時間計入那一回合 — WOG 在回合尾檢查，結果等價，時間計法與敗北一致 — 時間軸差一回合（0.8 秒）。
- 未定義的攻擊方式回 400 `invalidAttack` — JSON 可以送任意整數 — 多一個錯誤碼。
- 改設定時先以舊設定結算再寫入，兩筆交易、以版本鎖保護（同 `WorldService.MoveAsync`） — 沿用既有模式 — 兩步之間有並行寫入時回 409，玩家重送即可。
- 逃跑時舞台加 `monsterFade` 效果 — 既有敗北效果沒有淡出 — 無。

**Task 4：禁區鑰匙**

- 「何時扣鑰匙」寫成 Domain 的 `AreaKeys.NeedsKey` — 規則留在 Domain — 無。
- 推進回應帶 `areaPassExpiresAt`（可能已過期），前端以時間比較判斷 — 不另加 `passExpired` 旗標 — 客戶端時鐘偏差時，結束提示可能早或晚幾秒出現。
- 載入時強制每個禁區恰好一把鑰匙 — 一把以上會讓「用哪一把」有歧義 — 之後要做多把鑰匙時得先放寬驗證。
- 扣鑰匙前在交易內、取得角色列鎖之後重讀背包，鑰匙不見就 409 整筆回滾 — 避免用到推進或並行請求改過的舊資料 — 無。
- 開打時刻早於通行證到期的那一場會打完並發獎勵 — brief：最後一場的開打時刻 < 到期 — 最多多算一場。

**Task 5：新消耗品與異類合成**

- `ConsumableKind.Elixir` 留到 Task 6 — 依計畫的型別順序 — 無。
- `useResultLines` 改名 `itemUseResultLines` — `use` 開頭會被 react-hooks lint 當成 hook — 無。
- 異類合成以道具 id 判斷不同種類，結果種類在候選中均勻隨機 — WOG 的 `rand(1,100)` 區間加權把本作三種精鍊石都放在同一區間，均勻就是等價結果；目前只有 3 種，結果固定是第三種 — 之後新增精鍊石種類時，機率分布要重新對照 WOG 的區間。
- 新增 `InventoryRules.TryAddEach` — 寶箱一次放多顆魔石 — 無。
- 測試內容只有一種魔石，「魔石分類滿」用 15 格都是 99 顆的同一種模擬 — TestContent 沒有 15 種魔石 — 無。
- 拆成三個 commit（後端、schema、前端）而非一個 — 依協調者指示 — 無。

**Task 6：密藥**

- 喝密藥時先在同一筆交易裡以舊密藥把經過的時間結算到現在，再覆蓋同種密藥 — 只靠時間窗判斷時，覆蓋舊密藥會讓尚未結算的場次失去加成 — `InventoryService` 依賴 `AdventureService`；那段結算的戰鬥事件不回給前端。
- 介面命名 `IBuffRepository`／`BuffRepository`（依 brief） — 無 — 無。
- 角色分頁改成打開時才掛載 — 每次打開都要重讀密藥 — 失去角色頁捲動位置。
- `itemUseResultLines` 帶入 `now` — 結果行要算剩餘時間 — 無。
- 傭兵與冠軍挑戰不吃密藥 — 同（三）A — 無。

**Task 8：穩定性**

- `Transaction_UsesReadCommitted` 改成驗證行為（第二次讀取看得到另一條連線剛提交的寫入） — MySql.Data 只把隔離等級套在下一筆交易，session 變數不變，查真正的隔離等級需要測試帳號沒有的權限 — 無。
- 死鎖轉 409 由 `Transaction_DeadlockVictimGetsConflict` 釘住；`JobChange_TwoCharactersAtOnce_BothSucceed` 修改前就綠，只當回歸保護 — brief 的情境在 MySQL 上重現不出死鎖 — 無。
- 任務 JSON「缺 name」解讀為缺 `title` 與巢狀必填 id — 任務沒有 `name` 欄位 — 無。
- 「已強化」判斷放在 Domain 的 `IsEnhanced(entry, item)` 多載 — Api 不做遊戲計算 — 無。
- 內容移除道具後的孤兒列：被隱藏的魔石在裝備改寫時會遺失、隱藏列不計入容量，延到待辦 — brief 只承諾孤兒列不再 500，觸發條件是營運者移除內容而不是正常遊玩，完整修正橫跨三層 — 內容熱修後玩家可能少一顆魔石。

**Task 9：整理與延後的測試**

- 不新增 `FailingInventoryRepository` — `InMemoryInventoryRepository.FailWrite` 已有同樣能力 — 無。
- TestContent 新增測試用任務 `test-stone-reward` — 需要一個發魔石的任務來測分類已滿 — 無（只是測試資料）。
- 有經過時間時在已移除的地區推進、以及 `/me` 仍會 500，延到待辦 — 要先決定「被移除的地區怎麼模擬」 — 營運者移除地區後，停在那裡的角色無法遊玩。

**Task 10：前端**

- `discardQueued` 之後顯示值等於伺服器確認的角色 — 被丟掉的項目，其增量已含在確認的角色裡 — 無。
- 分頁重載節流：打開時的載入算一次，間隔內的訊號延到間隔滿合併重載；背景重載失敗保留畫面；技能頁背景重載不覆蓋未儲存的草稿 — 避免最後一筆掉落永遠沒反映 — 最多延遲 5 秒。
- 密藥持續分鐘四捨五入 — API 沒有回傳秒數，不擴充契約 — 顯示差 1 分鐘以內。
- 商店的兩組 tab 巢狀放在 tabpanel 內 — 讓 `aria-controls` 都指得到元素 — 無。
- 「只能僱用一位傭兵」只在目錄下方顯示一次 — 避免每張卡重複 — 無。
- 設定面板沒有名稱欄位，不加名稱長度檢查 — 面板只有百分比欄位 — 無。
- `summary` 不觸發技能頁重載 — 技能不會因摘要而改變，而且技能頁有未儲存的草稿 — 看不出代價。

**Task 11：美術規格**

- 不安裝 ImageMagick 7；實作者只驗證缺 `magick` 時的提示與 WebP 讀取、驗證測試，實際匯入一張 PNG 留給使用者 — 不在使用者未同意時安裝系統套件 — 使用者第一次跑之前，`magick` 參數沒有實測過。

**流程上的裁定**：多個任務的 commit 依協調者的派工訊息拆分（例如 Task 3、4 拆成四個、Task 5 拆成三個），而不是 brief 寫的單一 commit；Task 1 的 commit 訊息依派工訊息。

## （四）明確不做

以下照計畫 10 文末「明確不做」原文保留，附理由。

- `BuyAsync` 不看 `shopLevel`：WOG 商店 7 個等級都直接可買（`shop.php` 只以 `d_lv` 分頁），商店等級只是分頁。
- 移動時捨棄已結算的事件：結算結果已寫入角色與背包，只是少播最後幾場，屬外觀。
- 傭兵單獨一批時時間軸多 1 秒：外觀，不影響結算。
- 玩家自訂每日重置時刻：WOG 固定 09:00（`mktime(9,0,0)`），計畫 8 已裁定。
- 禁區任務：WOG 任務資料遺失，屬內容擴充；禁區開放後可用既有任務格式追加。
- NPC 對話任務（`m_npc`）：本作沒有 NPC 對話系統。
- 多目標擊殺任務（`id*n,id*n`）：現有任務內容都是單一目標（YAGNI）。
- VIP 果實 2319～2322、`wog_df_vip`：商城道具，spec §1 非目標。
- 寵物技能 45～48、捕捉加成技能 84：不在 spec §6 的技能目錄。
- 冠軍技能與 3% 額外攻擊 `p_cp_st`：屬英雄榜系統（多人色彩）。
- 英雄榜、排行榜：spec §1 多人內容，單機只剩自己。
- 傭兵戰報歷史表：WOG 保留 1 小時是給線上查看，推進回應已帶本次戰果。
- 榮譽裝備（`h_type 3～7`）：WOG 資料遺失，沒有可依據的內容。
- 密藥的 HP／SP 上限倍率（`d_lv 7、8`）：會牽動住宿、補給與面板的上限計算，WOG 的 SP 密藥本身寫錯欄位（`ARM:1186`）。
- 技能書（`d_type 13`）：WOG 的技能書對應只能從書學的特殊技能，本作技能目錄全部靠熟練度學習，沒有對應技能。
- 甜粽、鹹粽、紅包（`d_type 4、5、11`）：節慶活動道具，甜粽給的是交易所資源（多人）。
- 戰鬥回合數上限設定（`f_count`）：調低只會讓戰鬥更早超限，沒有單機價值。
- ~~地區背景圖、道具圖示、依職業換玩家圖：程式沒有使用處，美術規格只涵蓋舞台現有的玩家、怪物、寵物圖。~~ 已由 Spectra 變更 `content-asset-pipeline` 推翻：內容圖涵蓋道具、怪物、技能、職業立繪與圖示、戰鬥背景，規格見 `docs/art/README.md`。

## （五）計畫 10 之後的使用者決定

- 行動力整個移除（2026-10-25）：沒有消耗、回復、耗盡減半與 12 秒間隔，行動力藥水一併刪除；每場獎勵全額、間隔固定 2 秒，數值平衡不靠行動力。設計文件 §3.4；資料庫由 `V000000014__remove_stamina.sql` 刪除 `act_num`、`act_num_time` 與背包、倉庫裡的行動力藥水。

## （六）計畫 11：旅館

設計文件 `2026-10-29-inn-design.md`；內容檔 `backend/content/inn.json`。

- 房型共六間，數值方向來自使用者（2026-10-29），名稱與價格係數是〔Claude 的設計〕：

  | id | 名稱 | 加成 | 倍率 | 價格係數 |
  |---|---|---|---|---|
  | `apprentice-loft` | 學徒閣樓 | `Exp` | 1.10 | 2 |
  | `training-chamber` | 修練廂房 | `JobExp` | 1.10 | 2 |
  | `fighters-bunk` | 鬥士通鋪 | `Attack` | 1.05 | 1 |
  | `stargazer-tower` | 星見塔房 | `Magic` | 1.05 | 1 |
  | `bulwark-room` | 鐵壁客房 | `Defense` | 1.05 | 1 |
  | `quiet-chapel` | 靜謐禱室 | `MagicDefense` | 1.05 | 1 |

  持續 28 800 秒（8 小時）；價格 = 等級 × 50 × 價格係數。
- 手動入住一律收費，不比照自動住宿 Lv ≤ 100 免費：〔Claude 的判斷〕，使用者於 2026-10-30 確認。
- 房型加成跟密藥相乘，相乘後只取整一次；同時只能有一間房，入住會取代目前的房並重新計 8 小時（不延長）：〔使用者決定〕，2026-10-29。
- 入住時先結算尚未結算的冒險時間，再用結算後的角色重算價格、重新檢查錢夠不夠（結算可能花錢或升級）：〔Claude 的判斷〕，使用者於 2026-10-30 確認。
- 角色頁的「密藥效果」卡片也顯示目前的房型加成：〔使用者決定〕，2026-10-30。
- 不做住宿紀錄，留給之後的「戰鬥歷程」功能一起做，自動住宿會自然出現在歷程裡：〔使用者決定〕，2026-10-29。
- 死亡沒有經驗懲罰；只有 Lv > 100 且沒錢的自動住宿，會把當前等級經驗乘 0.8，與原版相同，使用者選擇維持不變：〔使用者決定〕，2026-10-30。

## 附錄：計畫 10 的延後項目分派

計畫 6～9 延後的項目原本只記在記憶檔 `ebo-plan-10-backlog.md`，計畫 10 逐條分派如下（照計畫 10 原文保留）。執行後仍未完成的項目見 `2026-10-23-deferred-backlog.md`。

**排入任務**：自動補給 SP 全滿不喝 → T3；寶箱哥布林 → T7；拆石滿包靜默消失 → T8；強化裝備賣出無確認 → T8（標記）＋T10（確認）；異類精鍊石合成、遺漏消耗品 → T5／T6；禁區鑰匙 → T4；舊地區佇列事件在「你前往」之後播出 → T10；全域怪漏寫 `placeId` 被當全域 → T8；移除道具 id 500 → T8；不同角色並行轉職死鎖 → T8；`!` null-forgiving（Socket、Refine、WogBattle、ForgeService、InventoryService、PetService、MercenaryService、AdventureService、HonourShop／HonourEndpoints、任務載入與 AcceptAsync）→ T9；交易包覆未被測試釘住、沒有真正的並行交易測試、回滾失敗蓋掉原例外 → T8／T9；Testcontainers 卡住 → T2；`CreateCharacterScreen` 逾時 → T2；背包分頁 tabpanel／方向鍵、背包與任務頁不隨掉落更新 → T10；`MissionBoard` 的 `Known`、`MissionRewards` 重複 `DropApplier`、擊殺達成判斷重複、`Delivery` 與 `InventoryRules` 強化定義不同 → T8／T9；任務 JSON 缺字串丟 ArgumentNullException → T8；計畫 8 測試缺口 → T9；React key 用文字 → T10；傭兵「僱用」按鈕、地區選單沒有失敗原因 → T10；寵物出擊率 → T10；`MercenaryRun.MissionKills` 未使用 → T9；`ChampionRepository` 首次寫入競態 → T8；名稱長度 UTF-16 與 rune 不一致 → T10；計畫 9 測試缺口 → T9。

**已完成（不需排入）**：裝備頁補給數量預設（計畫 7 Task 10）；交寵物每日任務 223／225 與勳章報酬（計畫 9 Task 8）；冠軍戰報以冠軍名稱稱呼（計畫 9 Task 10，`champion.test.ts` 的 `duelLog` 測試已釘住）。
