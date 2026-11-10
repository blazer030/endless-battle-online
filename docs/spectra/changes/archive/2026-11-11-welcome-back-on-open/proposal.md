## Why

已登入的玩家（瀏覽器存著 access token）打開網頁時，畫面先畫出標題背景（天空城鎮、招牌、木桌），讀到角色後直接切進遊戲；讀角色通常只要幾十毫秒，看起來就是標題頁一閃而過再跳畫面，使用者覺得不好（2026-11-11）。

第一輪改成讀到角色先播「歡迎回來」過場，但使用者實測後指出還是會先經過登入頁：打開網頁的讀取中仍畫在標題背景上（招牌、木桌），過場 0.3 秒後才淡入。使用者要的是打開網頁的第一個畫面就是過場背景，成功才出現歡迎詞，失敗回登入頁。

## What Changes

- 已登入打開網頁時，第一個畫面就是過場背景（依時段的滿版城鎮），一開始就有三個跳動的墨點；不出現標題招牌、木桌、登記證。
- 讀到角色：墨點消失，在同一個背景上淡入「歡迎回來＋角色名」，停 2.4 秒後進遊戲。
- 讀到帳號沒有角色：去建角（不變）。
- 打開網頁時讀角色失敗，不論是 401、斷線或伺服器錯誤：清掉 token，回到有登記證的登入頁、帳號空白、不顯示字條。斷線或伺服器錯誤先當成登入失敗（使用者決定，錯誤頁之後再說）。
- 「歡迎回來」下方的小字改成角色名：登入與已登入打開網頁都一樣（原本登入顯示帳號；打開網頁時前端沒記帳號，手上只有讀回來的角色）。
- 登入、註冊後的讀取流程不變：登記證收下、0.45 秒後桌上墨點、有角色先歡迎回來、401 回登記證、其他失敗字條＋重試。
- 讀完角色的去向不再因「從哪裡開始讀」而不同，App 裡記錄去向的 `Destination` 與 `TitleScreen` 的 `onSignedIn` 第二個參數（`login`／`reg`）移除（結構性變更，不改行為，已完成）。
- 墨點抽成共用元件，標題讀取中與打開網頁的過場背景共用（結構性變更）。

## Non-Goals

- 不做錯誤頁：打開網頁時的斷線、伺服器錯誤先回登入頁，之後再另外設計。
- 不改登入、註冊後讀取中的畫面與 0.45 秒的墨點延遲，也不改過場的 2.4 秒與建角後「踏上旅途＋角色名」。
- 不記住帳號、不新增 API；不呼叫 `GET /api/accounts/me` 取帳號。
- 不在前端檢查 token 是否過期，仍以讀角色的回應判斷。

## Capabilities

### New Capabilities

（無）

### Modified Capabilities

- `title-and-character-creation`: 「Load the character before entering」——已登入打開網頁改成先停在過場背景（墨點）、有角色淡入歡迎回來、讀取失敗一律清 token 回登記證；歡迎回來的小字從帳號改成角色名。

## Impact

- frontend：`frontend/src/ui/App.tsx`（打開網頁的讀取階段、讀完角色的去向、過場小字）、`frontend/src/ui/App.test.tsx`、`frontend/src/ui/title/Passage.tsx` 與 `Passage.css`（等待中的墨點、文字淡入）、新增共用墨點元件（`frontend/src/ui/title/` 下）、`frontend/src/ui/title/TitleScreen.tsx`、`TitleScreen.css` 與 `TitleScreen.test.tsx`。
- 規格：`docs/spectra/specs/title-and-character-creation/spec.md`。
- 設計稿：完成後請 EBO-design 在標題設計稿補上「打開網頁先停在過場背景」的狀態。
