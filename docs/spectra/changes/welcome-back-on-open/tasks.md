## 1. 讀完角色一律先歡迎回來（Load the character before entering）

- [x] 1.1 已登入打開網頁、有角色時，先顯示「歡迎回來」過場、停 `PassageMilliseconds` 後才進遊戲（Load the character before entering）。先把 `frontend/src/ui/App.test.tsx` 的 `App_StoredTokenWithCharacter_OpensGameDirectly` 改寫成 `App_StoredTokenWithCharacter_WelcomesBackThenOpensGame`：存 token、角色名 青衣 → 不出現 入城 按鈕、出現 歡迎回來 與 青衣、還沒出現 冒險中：青衣；推進 `PassageMilliseconds` 後出現 冒險中：青衣。確認它先失敗，再改 `App` 讀完角色的處理，讓有角色時一律進 passage 階段。驗證：這條測試與 `App.test.tsx` 其餘測試全綠。
- [x] 1.2 「歡迎回來」下方小字改成角色名，登入與已登入打開網頁一致（Load the character before entering）。先把 `App_SignInWithCharacter_WelcomesBackThenOpensGame` 的斷言從帳號 ethan 改成角色名 青衣，並斷言不出現 ethan；確認失敗後把 passage 的 subtitle 改用讀回來的角色名。驗證：`cd frontend && pnpm test App` 全綠。[after: 1.1]

## 2. 移除不再影響結果的去向（結構性變更，單獨 commit）

- [x] 2.1 讀完角色的結果已不因起點不同，移除 `App` 的 `Destination` 型別與 phase 裡的 `destination` 欄位，`TitleScreen` 的 `onSignedIn` 只傳帳號（拿掉 `login`／`reg` 參數，`SignInRoute` 仍留給登記證切換模式用），並更新 `App.tsx`、`Passage.tsx` 描述過場內容的註解。行為不變：改前改後 `cd frontend && pnpm check` 都全綠，`TitleScreen.test.tsx` 只把 `toHaveBeenCalledWith("ethan", "login")`／`("newbie", "reg")` 改成只帶帳號。以結構性變更單獨 commit。[after: 1.2]

## 3. 驗證

- [x] 3.1 實機確認：在本機完整 stack（`docker compose --profile app up -d --build` 或 `pnpm dev`）登入一個有角色的測試帳號後重新整理頁面，畫面是標題背景（看不到登記證）→ 歡迎回來＋角色名 → 進遊戲，沒有標題背景一閃就切畫面；清掉 localStorage 的 `ebo.accessToken` 後重新整理，回到有登記證的標題頁。測試帳號只建在本機資料庫。[after: 2.1]
- [x] 3.2 `./scripts/check.sh` exit code 0。[after: 3.1]

> 2026-11-11 使用者實測第一輪後修正需求：打開網頁時第一個畫面要是過場背景，而不是標題背景。3.1 的實機確認只記錄了程式所在的階段，沒有檢查畫面上實際出現的招牌與木桌；由 4.5 依新需求重新驗證。打開網頁的讀取失敗改成一律清 token 回登記證（見 design.md「打開網頁讀取失敗一律清 token 回登記證」）。

## 4. 打開網頁先停在過場背景（Load the character before entering）

- [x] 4.1 墨點抽成共用元件（design.md 同名決策）：把標題讀取中的三個墨點（`TitleScreen` 的 `.title-wait` 內容與 `title-dot` 動畫）抽成 `frontend/src/ui/title/` 下的共用墨點元件，`TitleScreen` 改用它，位置與 0.45 秒出現延遲仍由 `TitleScreen.css` 決定。行為不變：改前改後 `cd frontend && pnpm check` 全綠，以結構性變更單獨 commit。[after: 3.2]
- [x] 4.2 有 token 打開網頁的第一個畫面是過場背景＋墨點（Load the character before entering）。先寫失敗測試 `App_StoredToken_OpensOnPassageBackdropWithInkDots`：存 token、`getMyCharacter` 不回應 → 有 `role="status"`「讀取中」、沒有招牌標題 `無盡戰記 Endless Battle Online`、沒有 入城 按鈕、沒有 歡迎回來。再在 `App` 新增 opening 階段渲染 `Passage`（省略 title／subtitle 時顯示墨點，背景立即顯示）。驗證：這條測試與 `App.test.tsx` 全綠。[after: 4.1]
- [x] 4.3 讀到角色時在同一個過場背景上把墨點換成「歡迎回來＋角色名」並淡入，之後進遊戲（Load the character before entering）。先在 `App_StoredTokenWithCharacter_WelcomesBackThenOpensGame` 加斷言：出現歡迎回來時沒有「讀取中」、沒有招牌標題，確認失敗後實作 opening → passage 沿用同一個 `Passage` 元素、只有文字淡入；登入後的過場維持整層淡入。驗證：`App.test.tsx` 全綠，`App_WelcomePassage_LastsPassageMilliseconds` 仍通過。[after: 4.2]
- [x] 4.4 打開網頁時讀角色失敗（斷線、伺服器錯誤、讀建角資料失敗）一律清 token、回到有登記證的登入頁、帳號空白、沒有「讀取失敗」字條（Load the character before entering）。先把 `App_FailedLoad_ShowsNoteWithRetry` 改成從登入開始（字條＋重試只留在登入／註冊之後），再新增參數化失敗測試 `App_StoredTokenLoadFails_ClearsTokenAndShowsRegistryCards`（500、斷線、建角資料斷線三種），確認失敗後實作。`App_UnauthorizedLoad_ReturnsToRegistryCards` 維持通過。驗證：`App.test.tsx` 全綠。[after: 4.3]
- [x] 4.5 實機逐格確認：本機完整 stack 重建後，用本機測試帳號的 token 重新整理頁面，在每一個 animation frame 記錄 DOM 是否出現 `.title-sign`、`.title-desk`、`.title-card`——從載入到進遊戲都必須沒有；截圖第一格（過場背景＋墨點）與歡迎回來；改存一個無效 token 重新整理，回到有登記證的登入頁且 localStorage 沒有 `ebo.accessToken`。測試帳號只建在本機資料庫。[after: 4.4]
- [x] 4.6 `./scripts/check.sh` exit code 0。[after: 4.5]
- [x] 4.7 用 SendMessage 通知 EBO-design 在標題設計稿補上「已登入打開網頁：過場背景＋墨點 → 歡迎回來 → 遊戲；失敗回登記證」的狀態，附 4.5 的截圖路徑；驗證：訊息已送出。[after: 4.6]
- [x] 4.8 依 EBO-design 回覆的 TitleFit v465 同步樣式（樣式不寫測試，截圖確認）：清晨、白天、黃昏的過場背景上，墨點改成 `var(--umber)` 並加 `rgb(var(--glow-rgb) / 0.8)` 的陰影（夜晚維持奶油色）；「歡迎回來」下方小字字型改成 `"Great Vibes", var(--font-serif)`、粗細 700，中文角色名用思源宋體。驗證：白天時段截圖墨點清楚、角色名的 computed font-family 為 Great Vibes＋Noto Serif TC、`cd frontend && pnpm check` 全綠。[after: 4.7]
