---
name: ebo-design-rules
description: Use when designing or reviewing an EBO screen or canvas board — writing UI copy, NPC lines or error text, laying out for the 390×624～780 stage, adding buttons or disabled states, picking fonts, or deciding how a new portrait, item, UI piece or background should be generated
---

# EBO 設計規則

使用者在看畫布時定下的規則。違反這些規則的地方，使用者都退回過。畫布工具見 `ebo-canvas`。

## 動手前

- 先對照現有功能：`backend/EndlessBattle.Domain/` 的規則（格數、分類、限制）、`frontend/src/ui/*Screen.tsx` 的分頁、動作與狀態、`docs/research/*-table.md`。畫面上的數字、分類、按鈕都要對得上程式。列出要畫的狀態（空、選中、鎖定、滿、錯誤）再畫。
- 技能、職業的名稱和描述直接讀 `backend/content/*.json`，不要自己編。

## 文字

- **不寫操作說明**：不寫「點…」「滑…」「選…」開頭的句子，也不寫功能介紹句。只留名稱、數值（成功率、花費、持有數）和必要的規則數字。看不出能怎麼操作，就改版面（露出一半、狀態變化），不要補文字。
- **台詞用角色口吻**，不寫成系統說明；畫面被彈層或整頁內容蓋住時，台詞泡泡要藏起來。
- **錯誤訊息**：放在出錯的那張卡或欄位旁，跟原本的錯誤同位置、同樣式；要在那個寬度內一行放得下，放不下就縮短字句，不要讓最後一兩個字折到下一行。
- 遊戲名「無盡戰記」（思源宋體 900），副標 Great Vibes「Endless Battle Online」；不用縮寫 EBO 當大標。

## 版面

- 舞台寬 390、高 624～780，縮放規則見 repo `CLAUDE.md` 遊戲前端第 2 條。每個畫面標出「貼上緣／伸縮／貼下緣」各是哪一區。
- 要檢查最矮 624 和最高 780 兩種高度；有捲動清單的，還要看捲到最底的樣子。
- 字級在基準尺寸下內文 ≥ 13px、小標籤 ≥ 11px。句子、描述算內文；數字標籤（含「剩 3 點」這種數字加單位的短語）、角標、欄位名稱算小標籤。元素會縮放的話，用最小倍率回推（例：登記證最小 1.13 倍，證上的小標籤寫 10px，放大後約 11.3px）。
- 舞台外不畫東西，只露共用背景（`--stage-backdrop`，目前是 #2A1A0C）。
- 停用的按鈕不能有按下回饋：`:active` 的縮放、位移寫成 `:not(:disabled):active`。
- 固定在角落的按鈕（印章等）不能蓋到可捲動的內容。

## 字型

思源黑體（Noto Sans TC）內文、Cinzel 數字與拉丁字、思源宋體 900 標題與按鈕、印章字、Great Vibes 英文副標。不要用自託管的子集字型（古馬路口缺字太多，已停用）。

## 產圖：用哪個工具

| 素材 | 工具 | 備註 |
|---|---|---|
| 職業立繪、戰鬥精靈圖 | Gemini（`agy`，附戰士參考圖） | `.design-work/work/create-jobs/gen-job.sh`、`gen-job-f.sh`；不要用 grok |
| NPC 立繪 | Gemini | 沒有固定腳本：參考 `docs/art/prompts/demo/` 裡相近 NPC 的提示詞，用 `agy` 附參考圖產；或請使用者用 Gemini 網頁版產 |
| 道具、介面物件 | grok `gen.sh … item` | 要看 check 圖的邊緣 |
| 場景 | grok `gen.sh … scene` | |
| 戰鬥背景 | grok `gen.sh … stage` | 要拿掉的細節逐項寫明；不能太鮮豔，會搶角色焦點 |
| 同一張背景的不同時段 | Gemini 附原圖，只換時間與光線 | 構圖要完全相同，熱點座標才能共用 |

- 畫風範本、每張素材的提示詞：`docs/art/prompts/demo/README.md`。
- `agy` 不加 `--dangerously-skip-permissions`。
- 女性版立繪是另外設計的角色：服裝、身形、姿勢都重新設計，只共用畫風與髮色、瞳色。世界觀是中世紀奇幻，不要現代剪裁（短褶裙＋褲襪被退過）。
- 使用者在 Gemini 網頁版產的圖，請使用者把提示詞貼過來，一起存檔。

## 素材庫

每次上傳素材後都要整理進素材庫：DEMO 用到的放對應的 `AssetsJ*` 板子，標「現用」；試產但沒用、被否決的放 `AssetsJSpare`，標「備用」，寫一句為什麼不用和日期。收尾前比對 HomeJ 用到的 `/_blob/` id 都在 `AssetsJ*` 裡。
