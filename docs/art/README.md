# 舞台美術規格

舞台上的玩家、怪物圖都是靜態 WebP。沒有圖也能玩，所以圖可以一張一張慢慢補。

## 用途與查找方式

- 圖放在 `frontend/src/assets/art/<內容 id>.webp`。`src/render/artIndex.ts` 以 `import.meta.glob` 建索引，用**檔名（不含副檔名）**當 id 找圖。
- `StageRenderer` 把圖縮放到框內：`scale = min(框寬 / 圖寬, 框高 / 圖高)`，錨點在**底部中央**。
- 框的尺寸（CSS px）：玩家 100×170、怪物 130×180、寵物 60×84。畫布以最高 3 倍 DPR 為準，所以怪物圖 720×1000 約等於 3 倍的框。

## 類別與 id

| 類別 | id | 畫布（px） | 面向 |
|---|---|---|---|
| 玩家 | `player`（所有職業共用一張預設圖） | 600×1020 | 右 |
| 怪物 | `backend/content/monsters.json` 的每個 `id`（含任務怪） | 720×1000 | 左 |

## 格式與大小

- 格式 WebP，透明背景（alpha）。
- 畫布固定：玩家 600×1020、怪物 720×1000，不是這個尺寸的圖測試會擋下。
- 單檔不超過 153 600 bytes（150 KiB）。
- 這些值的程式版本在 `frontend/src/test/artRules.ts`，匯入腳本與測試都以它為準。

## 構圖規則

- 全身入鏡，腳底貼齊畫布下緣。
- 主體高度佔畫布的 85～95%，左右置中。
- 不得有文字、邊框、地面陰影、背景。

## 色彩方向

依 spec §10：舞台是深藍夜色，主體要在上面讀得清楚。

- 邊緣加冷青或琥珀金的輪廓光。
- 不要用 `#121826` 附近的暗色當主體的大面積色。

## 沒有圖與載入失敗

- 沒有圖：怪物顯示橫排名牌，寵物顯示小名牌。
- 圖載入失敗：退回同樣的名牌。
- 不需要任何佔位檔，也不要放空白圖。

## 匯入流程

1. 用 `docs/art/prompts.md` 的範本生圖，存成 PNG（透明背景）。
2. `scripts/import-art.sh <來源圖> <id>`（需要 ImageMagick 7 的 `magick`）。腳本會裁邊、縮放、貼齊下緣補成規格畫布並轉成 WebP；超過大小上限會自動降畫質再試一次。
3. `cd frontend && pnpm vitest run src/render/artAssets.test.ts`，確認 id、畫布、大小都合規。
4. commit，訊息格式 `📄 docs: add art for <id>`。

## 缺圖清單怎麼查

比對 `docs/research/monster-roster.md` 的怪物 id 與 `frontend/src/assets/art/` 的檔名；roster 有、資料夾沒有的就是缺圖。例如：

```sh
grep -o '"id": "[a-z0-9-]*"' backend/content/monsters.json | sed 's/"id": "\(.*\)"/\1/' | sort > /tmp/all.txt
ls frontend/src/assets/art | sed -n 's/\.webp$//p' | sort > /tmp/have.txt
comm -23 /tmp/all.txt /tmp/have.txt
```
