## Why

已登入的玩家（瀏覽器存著 access token）打開網頁時，畫面先畫出標題背景（天空城鎮、招牌、木桌），讀到角色後直接切進遊戲；讀角色通常只要幾十毫秒，看起來就是標題頁一閃而過再跳畫面，使用者覺得不好（2026-11-11）。登入時已經有「歡迎回來」過場銜接，同一個背景拉近進城，打開網頁時也用它就不會突兀。

## What Changes

- 已登入打開網頁、讀到角色時，改成跟登入一樣先顯示「歡迎回來」過場（整個舞台拉近城鎮，停 2.4 秒），再進遊戲；不再直接切進遊戲。
- 「歡迎回來」下方的小字改成角色名：登入與已登入打開網頁都一樣（原本登入顯示帳號；打開網頁時前端沒記帳號，手上只有讀回來的角色）。
- 讀角色時的其他結果不變：讀取中（背景、隱藏登記證、0.45 秒後墨點）、沒有角色去建角、401 清掉 token 回登記證、其他失敗顯示字條與重試。
- 讀完角色的去向不再因「從哪裡開始讀」而不同，App 裡記錄去向的 `Destination` 與 `TitleScreen` 的 `onSignedIn` 第二個參數（`login`／`reg`）不再有用，行為改完後另外移除（結構性變更，不改行為）。

## Non-Goals

- 不改讀取中的畫面：不換成純深色背景、不調整墨點出現的 0.45 秒（曾提出「有 token 時先只顯示深色背景、慢才淡入標題」，使用者選了沿用過場背景）。
- 不改過場本身的畫面與 2.4 秒的長度，也不改建角後「踏上旅途＋角色名」。
- 不記住帳號、不新增 API；不呼叫 `GET /api/accounts/me` 取帳號。
- 不在前端檢查 token 是否過期，仍以讀角色的回應判斷。

## Capabilities

### New Capabilities

（無）

### Modified Capabilities

- `title-and-character-creation`: 「Load the character before entering」——已登入打開網頁、有角色時改成先顯示歡迎回來；歡迎回來的小字從帳號改成角色名。

## Impact

- frontend：`frontend/src/ui/App.tsx`（讀完角色的去向、過場小字）、`frontend/src/ui/App.test.tsx`、`frontend/src/ui/title/TitleScreen.tsx` 與 `frontend/src/ui/title/TitleScreen.test.tsx`（移除 `onSignedIn` 的 route 參數）、`frontend/src/ui/title/Passage.tsx`（註解）。
- 規格：`docs/spectra/specs/title-and-character-creation/spec.md`。
