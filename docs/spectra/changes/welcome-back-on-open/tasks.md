## 1. 讀完角色一律先歡迎回來（Load the character before entering）

- [x] 1.1 已登入打開網頁、有角色時，先顯示「歡迎回來」過場、停 `PassageMilliseconds` 後才進遊戲（Load the character before entering）。先把 `frontend/src/ui/App.test.tsx` 的 `App_StoredTokenWithCharacter_OpensGameDirectly` 改寫成 `App_StoredTokenWithCharacter_WelcomesBackThenOpensGame`：存 token、角色名 青衣 → 不出現 入城 按鈕、出現 歡迎回來 與 青衣、還沒出現 冒險中：青衣；推進 `PassageMilliseconds` 後出現 冒險中：青衣。確認它先失敗，再改 `App` 讀完角色的處理，讓有角色時一律進 passage 階段。驗證：這條測試與 `App.test.tsx` 其餘測試全綠。
- [x] 1.2 「歡迎回來」下方小字改成角色名，登入與已登入打開網頁一致（Load the character before entering）。先把 `App_SignInWithCharacter_WelcomesBackThenOpensGame` 的斷言從帳號 ethan 改成角色名 青衣，並斷言不出現 ethan；確認失敗後把 passage 的 subtitle 改用讀回來的角色名。驗證：`cd frontend && pnpm test App` 全綠。[after: 1.1]

## 2. 移除不再影響結果的去向（結構性變更，單獨 commit）

- [x] 2.1 讀完角色的結果已不因起點不同，移除 `App` 的 `Destination` 型別與 phase 裡的 `destination` 欄位，`TitleScreen` 的 `onSignedIn` 只傳帳號（拿掉 `login`／`reg` 參數，`SignInRoute` 仍留給登記證切換模式用），並更新 `App.tsx`、`Passage.tsx` 描述過場內容的註解。行為不變：改前改後 `cd frontend && pnpm check` 都全綠，`TitleScreen.test.tsx` 只把 `toHaveBeenCalledWith("ethan", "login")`／`("newbie", "reg")` 改成只帶帳號。以結構性變更單獨 commit。[after: 1.2]

## 3. 驗證

- [x] 3.1 實機確認：在本機完整 stack（`docker compose --profile app up -d --build` 或 `pnpm dev`）登入一個有角色的測試帳號後重新整理頁面，畫面是標題背景（看不到登記證）→ 歡迎回來＋角色名 → 進遊戲，沒有標題背景一閃就切畫面；清掉 localStorage 的 `ebo.accessToken` 後重新整理，回到有登記證的標題頁。測試帳號只建在本機資料庫。[after: 2.1]
- [x] 3.2 `./scripts/check.sh` exit code 0。[after: 3.1]
