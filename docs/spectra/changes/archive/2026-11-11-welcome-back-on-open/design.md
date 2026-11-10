## Context

第一輪（任務 1～3）讓已登入打開網頁讀到角色後先播歡迎回來過場。實測時畫面仍是：標題背景（`TitleBackdrop`：天空、招牌、木桌）＋讀取中 → 0.3 秒後 `Passage`（自帶滿版城鎮背景）淡入 0.6 秒 → 進遊戲。前 0.3～0.9 秒看得到招牌與木桌，等於先經過登入頁。第一輪的手動驗證只記錄了程式所在的階段，沒有逐格檢查畫面上實際出現的元素，所以沒抓到。

## Goals / Non-Goals

**Goals:**

- 已登入打開網頁，從第一個畫面起就看不到招牌、木桌、登記證，只有過場背景與墨點。
- 讀到角色後在同一個背景上淡入歡迎回來，不換背景、不閃。
- 打開網頁時讀取失敗一律回登入頁並清 token。

**Non-Goals:**

- 錯誤頁、登入／註冊後的讀取流程、過場長度都不改（見 proposal Non-Goals）。

## Decisions

### 打開網頁的讀取改用獨立的 opening 階段

**Supersedes**: welcome-back-on-open / 第一輪沿用標題讀取中畫面（proposal 第一版 Non-Goals：「不改讀取中的畫面」）

`App` 的 phase 新增 `opening`：有 token 時的初始 phase，畫面只渲染 `Passage` 的背景與墨點，不渲染 `TitleScreen`／`TitleBackdrop`。讀到角色後進 `passage` 階段；兩個階段渲染相同的元素結構（`StageFrame` > `Passage`），React 沿用同一個 DOM 元素，背景不重畫，只把墨點換成文字並淡入。

替代方案：沿用 `title` 的 loading 狀態、只把招牌與木桌藏起來——會讓 `TitleScreen` 多一種模式，且登記證、桌面等元素仍在 DOM 裡，不採用。

### 打開網頁讀取失敗一律清 token 回登記證

**Supersedes**: welcome-back-on-open / 第一輪「其他失敗字條＋重試」對打開網頁也適用

使用者決定先把斷線、伺服器錯誤當成登入失敗：`opening` 階段任何失敗都 `clearAccessToken()` 並回 `titlePhase()`（帳號空白、登記證可用、沒有字條）。登入、註冊後的失敗處理不變。

### 墨點抽成共用元件

標題讀取中的三個墨點（`TitleScreen` 的 `.title-wait`）與打開網頁的過場背景共用同一個元件與動畫；位置、出現延遲由使用的地方決定（標題：桌上、0.45 秒後；打開網頁：過場背景上、立即）。先以結構性變更抽出，行為不變，再用在 `Passage`。

### 打開網頁沒角色或讀取失敗時，過場背景淡出再露出下一個畫面

使用者 2026-11-11 決定照設計稿 TitleFit v465 補上：opening 讀完後要去建角或回登記證時，下一個畫面直接渲染在底下，過場背景留在上面淡出（0.6 秒、延遲 0.3 秒，與設計稿 `.tt-go` 的 `transition: opacity .6s .3s` 相同），墨點 0.2 秒淡出；淡出總長 `PassageLeaveMilliseconds`（900 毫秒，放 `src/motion/timings.ts`）後移除這層。淡出中的這層不攔點擊、對輔助技術隱藏，不再宣告「讀取中」。

替代方案：等淡出結束才切 phase——下一個畫面要多等 0.9 秒才出現，也跟設計稿「底下已經是登記證」不同，不採用。用 `animationend` 移除——jsdom 不觸發動畫事件，測試要另外模擬，改用計時器。

## Implementation Contract

- **行為**：有 token 打開網頁 → 第一格就是過場背景＋三個跳動墨點（`role="status"`、名稱「讀取中」）；DOM 中沒有招牌標題（`無盡戰記 Endless Battle Online`）、木桌、登記證。讀到角色 → 墨點消失、「歡迎回來」＋角色名淡入 → `PassageMilliseconds` 後進遊戲。沒角色 → 建角。任何失敗 → token 清掉、登記證可用、帳號空白、沒有「讀取失敗」字條。
- **介面**：`Passage` 接受可省略的 `title`／`subtitle`；省略時顯示墨點。由 opening 接續的過場背景立即顯示（不做整層淡入），只有文字淡入；登入後的過場維持整層淡入、背後有 `TitleBackdrop departing`。
- **淡出**：opening → 建角／登記證時，過場背景疊在新畫面上淡出（0.6 秒、延遲 0.3 秒；墨點 0.2 秒），`PassageLeaveMilliseconds` 後從 DOM 移除；淡出中不攔點擊、`aria-hidden`、沒有「讀取中」status。讀到角色的歡迎回來與登入後的流程不受影響。
- **失敗模式**：opening 失敗不顯示錯誤文字；401 由 client 通知登出，與一般失敗走同一個結果。
- **驗收**：`App.test.tsx` 的 opening 測試（初始畫面、成功、失敗三種）、`TitleScreen` 既有測試全綠；本機逐格記錄重新整理後每一格是否出現 `.title-sign`／`.title-desk`／`.title-card`（必須從頭到尾都沒有），並截圖第一格與歡迎回來；`./scripts/check.sh` exit 0。
- **範圍**：只改已登入打開網頁的流程與墨點共用；登入／註冊、建角、遊戲畫面不動。

## Risks / Trade-offs

- 斷線時也會清 token，玩家要重新登入；使用者接受，之後做錯誤頁時再調整。
- opening 與 passage 共用 DOM 元素依賴兩個 phase 渲染相同結構；若之後在其中一邊加外層元素，背景會重新掛載並閃一下。以逐格手動驗證把關。
