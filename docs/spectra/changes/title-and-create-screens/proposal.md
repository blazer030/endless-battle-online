## Why

設計稿 EBO DEMO 定案後要逐頁換成新畫面；標題頁（登入、註冊）與建角頁跟遊戲頁關聯最小，先從這兩頁開始。功能對照時發現：建角的職業介紹冊需要「職業＋技能」的公開資料，但 `/api/content` 什麼都塞在一起、又沒有技能；設計稿的 390×844 太細長，跟前端「375×667 為主要尺寸」的規則也對不上，所以一併定下新畫面的版面基準。

## What Changes

- 新增公開資料的 API 前綴 `/api/public/`：不用登入就能讀的新資料一律放這裡。本次新增：
  - `GET /api/public/jobs`：全部職業的定義（名稱、階級、母職業、轉職條件、成長）與各自的技能（名稱、說明、最高等級、前置技能與等級、每一級的效果摘要）。
  - `GET /api/public/settings`：建角與顯示用的通用選項（性別、屬性、出生地）。
  - 新畫面不再使用 `/api/content`；`/api/content`、`/api/jobs`（角色的轉職狀態）保持不變。
- 新增「新畫面的版面基準」：寬 390、高 624～780 隨螢幕伸縮（高寬比 1.6～2.0 之間不補背景；原本 1.75～1.85，使用者 2026-11-09 放寬）；比 2.0 細長時停在最高、照寬度縮放、上下補背景；比 1.6 扁（例如電腦）時舞台盡量拉高（舞台高＝視窗高，限 624～780，倍率不低於 1，比 624 矮才縮小）、左右補背景（使用者 2026-11-10 改）；多出來的地方鋪所有新畫面共用的一個背景（先用深色，之後可換一張大的靜態圖）；不設倍率上限；基準內文 ≥13px、小標籤 ≥11px。
- 標題頁照新板子 TitleFit 重做：冒險者登記證登入（印章「入城」）、從一疊空白證抽一張註冊（印章「登記」，成功蓋蠟封）、依裝置時間的四個時段背景、招牌「無盡戰記」；錯誤照後端原文寫在證上；讀角色時登記證收下桌、桌上出現墨點；讀取失敗出現字條與「重試」印章；登入成功的「歡迎回來」過場後進遊戲。
- 已登入（瀏覽器有 token）的人打開網頁時，直接進入「讀角色」狀態，讀到就直接進遊戲，不顯示登入的登記證，也不顯示「歡迎回來」。
- 建角頁照 TitleFit 重做：公會櫃檯與接待員莉瑟，玩家自己點登記證的欄位來填（名字、性別、出身、屬性、職業、能力），每一欄打開對應的回答彈窗；職業欄攤開職業介紹冊（立繪依性別、定位、介紹、成長、技能）；六欄都填完「出發」才能按；名字重複時莉瑟說明並打開名字欄；送出失敗時莉瑟說明、內容保留可再按；「離開」先確認，確認後登出回標題、帳號留在登記證上；建角成功的「踏上旅途」過場後進遊戲。
- 職業立繪改由內容圖清單（R2 `portraits/<職業 id>-male|female`）取得；四個初階職業圖示匯入 R2 `job-icons`；招牌、木桌、登記證、印章、莉瑟、書籤、屬性徽章、四時段背景等轉成設計素材進 `frontend/src/assets/`。
- **BREAKING（規則）**：改寫前端硬規則 2（版面基準）、3（新增新畫面用的色彩變數）、4（字型加入 Noto Serif TC 900、Great Vibes 等）。推翻舊設計文件 `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §10 的深藍夜色色票與字型（裁定彙整 `docs/superpowers/specs/2026-10-22-wog-remake-rulings.md` 計畫 3 那條「主題與創角規則依 spec §3、§10」中 §10 的部分）；§3.2 的創角規則（10 點、每項至少 1、選項）不變。
- 舊的 `AuthScreen`、`CreateCharacterScreen` 由新畫面取代並刪除；遊戲頁（`AdventureScreen`）維持現狀。
- **BREAKING（規則）**：建角能力點數必須剛好分完 10 點才能建立（原本照 WOG 原版，沒分完的點數作廢）；使用者判斷沒分完沒有好處、多半是誤點。後端回 400「能力點數必須剛好分完 10 點」，建角畫面在還有點數時不能確定能力。舊設計文件 `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §3.2 只寫「可分配 10 點、每項至少 1」，這條補上「必須分完」。
- 補規格：這兩頁依賴的帳號規則（註冊、登入、token 期限）與建角規則（名字、選項、配點、起始狀態）在導入 Spectra 前就做好、沒有規格，這次寫成 `accounts`、`character-creation-rules`（除了上面的配點規則，都描述現況），逐條對照既有測試，缺的補上。

## Capabilities

### New Capabilities

- `public-game-data`: 不用登入就能讀的遊戲資料 API（`/api/public/` 前綴、職業與技能、通用選項）。
- `screen-layout`: 新畫面的版面基準：基準尺寸、伸縮範圍、等比縮放與補背景、電腦上的倍率上限、字級下限。
- `title-and-character-creation`: 標題頁（登入、註冊、讀角色、讀取失敗、自動登入、過場）與建角頁（欄位填寫、職業介紹冊、送出、離開）的行為。
- `accounts`: 既有的帳號規則（註冊條件、帳號不分大小寫不重複、登入失敗不透露帳號是否存在、token 30 天、需要登入的端點）。
- `character-creation-rules`: 建角規則（一個帳號一個角色、名字 2～12 字且不重複、選項範圍、10 點必須分完、起始狀態）。

### Modified Capabilities

(none)

## Impact

- Api：新增 `backend/EndlessBattle.Api/Endpoints/PublicEndpoints.cs`（`/api/public` 群組，不需驗證）與對應的回應型別；`backend/openapi/` 文件重新產生。
- Domain：`CharacterFactory` 的配點檢查改成必須剛好 10 點。
- Application：職業與技能公開資料的組裝（技能每級效果摘要沿用 `backend/EndlessBattle.Application/Skills/SkillSummary.cs`）。
- Tests：`backend/EndlessBattle.Tests` 新增公開 API 的測試；帳號與建角規則補缺少的測試。
- frontend：`frontend/src/api/`（client、`schema.ts` 重新產生、公開資料的轉換）、`frontend/src/game/`（建角草稿、版面計算、時段等純 TS 模型）、`frontend/src/ui/`（新的標題與建角畫面、`App.tsx` 流程、`theme.css`）、`frontend/index.html`（字型）、`frontend/src/assets/`（設計素材、`content-manifest.json` 加入職業圖示）；刪除 `AuthScreen`、`CreateCharacterScreen` 與其測試。
- 文件：專案 `CLAUDE.md` 前端硬規則 2、3、4；`docs/art/README.md` 設計素材清單。
