# 美術資源

遊戲用到的圖分兩種，放的地方不同：

| | 是什麼 | 放哪裡 | 怎麼更新 |
|---|---|---|---|
| 內容圖 | 道具、怪物（含寵物）、技能、職業立繪、職業圖示、戰鬥背景、旅館房型徽章與房間背景 | Cloudflare R2 bucket `ebo-content`（`https://ebo-assets.blazer030.dev`），git 只存清單 `frontend/src/assets/content-manifest.json` | `tools/assets` 的 `import` 指令，跟著 release／hotfix |
| 設計素材 | UI 元件、場景、NPC、佔位圖 | `frontend/src/assets/<分類>/`，進 git、由 Vite 打包 | `tools/assets` 的 `design` 指令，跟著畫面改版 |

原檔（生圖的 PNG）上傳到私人 bucket `ebo-originals`，不進 git。

## 內容圖的規格

| 類別 | 鍵 | 畫布 | 構圖 | 透明 | 品質 | 上限 |
|---|---|---|---|---|---|---|
| `portraits` 職業立繪 | `<職業 id>-male`、`<職業 id>-female`（角色 `Sex` 1 男、2 女） | 800×1200 | 人物高 1100、腳底 y=1180、身體中線 x=400 | 是 | 80 | 160 KB |
| `job-icons` 職業圖示 | `<職業 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `skills` 技能圖示 | `<技能 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `items` 道具圖示 | `<道具 id>` | 160×160 | 圖案長邊 88% 置中 | 是 | 85 | 20 KB |
| `monsters` 怪物與寵物 | `<怪物 id>` | 540×540 | 縮到寬 513／高 486 以內、水平置中、腳底 y=529 | 是 | 80 | 60 KB |
| `battlefields` 戰鬥背景 | `<地點 id>`（`world.json` places 的數字） | 1024×1024 | 等比縮放後置中裁切填滿 | 否 | 75 | 150 KB |
| `room-badges` 房型徽章 | `<房型 id>`（`inn.json` rooms） | 128×128 | 圖案長邊 88% 置中 | 是 | 85 | 16 KB |
| `room-backgrounds` 房間背景 | `<房型 id>`（`inn.json` rooms） | 800×450 | 等比縮放後置中裁切填滿 | 否 | 75 | 60 KB |

- 鍵只能用小寫英文、數字與連字號，而且要是 `backend/content` 裡有的 id（見「看缺哪些圖」）；新內容要先加進內容檔再匯入圖。R2 鎖定後刪不掉，所以打錯的鍵會在上傳前被擋下，`--dry-run` 只提醒。
- alpha > 24 的像素算不透明；「圖案」指不透明像素的外框。
- 超過上限時品質每次降 5 重試，降到 60 還超過就拒絕。
- 原圖比規格小時照樣輸出、會印出「已放大」的警告；怪物原圖建議至少 600 高。
- 程式版本在 `tools/assets/src/process.ts`。

### 立繪對齊

立繪在匯入時就對齊好，畫面上只要放在固定位置，不用逐張調位移：

- 人物從頭頂到腳底縮放成 1100 高，頭頂在 y=80、腳底（最下面的不透明列）在 y=1180。
- 身體中線＝人物高度 35%～95% 區間內，每一列不透明像素中點的中位數；武器、披風不容易把它拉歪。中線對齊 x=400。
- 頭頂預設是最上面的不透明列。法杖、帽子比頭高時，用 `--head-y <原圖的 y>` 指定頭頂；這個值會記在清單，重新匯入結果一樣。
- 任何不透明像素超出畫布就拒絕，並說明是哪一邊。

## 匯入內容圖

1. 第一次使用：把 `.env.r2.example` 複製成 repo 根目錄的 `.env.r2`，填入 R2 API token 的 `R2_ENDPOINT`、`R2_ACCESS_KEY_ID`、`R2_SECRET_ACCESS_KEY`（Cloudflare → R2 → Overview → API Tokens，權限 Object Read & Write，只限 `ebo-content`、`ebo-originals`）。`.env.r2` 不進 git，也不要貼到任何地方。
2. 先試轉看預覽：

   ```sh
   cd tools/assets
   pnpm cli import portraits job-6-male ../../.design-work/assets/<檔名>.png --dry-run
   ```

   預覽圖在 `.asset-preview/<類別>/<鍵>.png`：棋盤底看得出透明，立繪有頭頂、腳底（紅）與中線（藍）的輔助線，怪物有腳底線，圖示有 88% 框。
3. 沒問題就拿掉 `--dry-run` 再跑一次：工具會上傳原檔與內容圖，兩者都成功才更新清單。
4. commit 清單（訊息例：`📄 docs: add art for job-6-male`），跟著 release 或 hotfix 上線。

### 換圖

再匯入一次同一個鍵就好。內容圖的檔名是 `<類別>/<鍵>.<內容雜湊>.webp`，換圖＝上傳新檔＋清單改指向新檔；舊檔留在 R2（bucket 設了永久鎖定，不能覆寫或刪除），所以舊版本永遠指得到當時的圖，換錯了把清單改回去即可。

### 看缺哪些圖

```sh
cd tools/assets && pnpm cli missing          # 八類全部
cd tools/assets && pnpm cli missing monsters # 只看一類
```

應有的鍵從 `backend/content/*.json` 推出來。清單裡有、內容沒有的鍵會列在「多：」那一行。

## 遊戲怎麼找圖

- 一律透過 `frontend/src/render/contentAssets.ts` 的 `contentAsset(類別, 鍵)`：清單有就回 R2 網址，沒有就回該類的佔位圖。
- R2 的圖載入失敗時也退回佔位圖（`contentArtCandidates`）。
- 佔位圖在 `frontend/src/assets/placeholders/<類別>.svg`，是灰階剪影。SVG 是美術檔不是程式碼，裡面的色碼不受「顏色只寫在 `theme.css`」的規則約束。
- 道具另有各分類的線條佔位圖 `frontend/src/assets/placeholders/items/<分類>.svg`（`weapon`、`head`、`body`、`hand`、`foot`、`accessory`、`consumable`、`stone`、`refine-stone`、`material`、`medal`、`key`，產生器 `.design-work/work/placeholders/build.py`）；畫面用 `ContentImage` 的 `placeholder` 指定，目前只有勳章所用，背包、鐵舖、商店換新設計時再改用。
- 冒險頁舞台：怪物與寵物用 `monsters/<怪物 id>`；怪物名字在血條上，寵物保留名牌。

## 設計素材

```sh
cd tools/assets
pnpm cli design ui paper-strip ../../.design-work/assets/<檔名>.png --max-width 600
```

輸出 `frontend/src/assets/ui/paper-strip.webp`：品質 85、保留透明、不裁切，比 `--max-width` 寬才等比縮小；超過 300 KB 就拒絕。`--max-width` 設成畫面上顯示寬度的 2 倍（原圖比這小就照原圖）。

### `title/`：標題與建角（設計板 TitleFit）

來源是 `.design-work/assets/<blob id>.*`，括號裡是 `--max-width`。

| 檔名 | 用途 | blob id |
| --- | --- | --- |
| `sign` | 招牌（600） | `a02aafe6` |
| `sky-dawn`、`sky-day`、`sky-dusk`、`sky-night` | 標題的城鎮遠景，依時段換（972，原圖 848） | `e6eb4eb9`、`815cd4a2`、`67e2bf51`、`6a33dc7e` |
| `desk`、`desk-edge` | 木桌與前緣木條；建角的櫃檯也用（940／216） | `6884370a`、`d137517f` |
| `card-sealed`、`card-blank` | 有蠟封的登記證、空白登記證（650，原圖 476） | `e5a31933`、`3a7feeba` |
| `seal` | 紅印章（入城、登記、出發、就職、重試）（128） | `759746ed` |
| `note` | 讀取失敗的字條、建角的回答彈窗紙（439；`border-image` 切 `84 126 42 36`） | `75aeed6c` |
| `hall`、`clerk`、`bubble` | 公會大廳、接待員莉瑟、對話框（940／372／432） | `12b6be86`、`a32a504a`、`b97d9abd` |
| `element-earth`～`element-poison` | 屬性徽章：地、水、火、木、風、毒（52） | `374bc9f4`、`9bc2ed53`、`df05ace4`、`d85235a7`、`e8f93fd9`、`9f653c0d` |
| `book-page` | 職業介紹冊的紙（780，原圖 720） | `dd89ce59` |
| `bookmark-warrior`、`-sorcerer`、`-thief`、`-bard` | 介紹冊的書籤（88） | `d702e92f`、`d371eec9`、`88b2eedf`、`2ccfd7be` |

職業圖示不是設計素材：`job-icons/job-6`～`job-9` 用 `import` 上傳 R2（戰士 `62fa0017`、術士 `707f3b3a`、盜賊 `eb722b44`、樂手 `36314e2f`）。

### `town/`：城鎮（設計板 TownFit）

| 檔名 | 用途 | blob id |
| --- | --- | --- |
| `panorama-dawn`、`-day`、`-dusk`、`-night` | 城鎮三段全景，依時段換（原圖 1376×768，不縮） | `b1b11a9c`、`cbcc9c3b`、`75b36d0b`、`44221259` |
| `paper` | 紙籤、名牌、換段紙籤的紙條（356） | `35548540` |
| `compass` | 出發羅盤（296） | `d55573c6` |
| `purse` | 金錢的錢袋（48） | `c85813b2` |
| `icon-character`、`icon-bag`、`icon-companions` | 城門口角色、背包、同伴紙籤的圖示（92） | `1b66a608`、`4ca152c1`、`e0b38181` |

密藥圓章的圖是道具圖，不是設計素材：`items/item-2257`（神力秘藥 `8a7c67f6`）、`items/item-2255`（戰鬥熟練手冊 `e233c863`）用 `import` 上傳 R2，其他密藥用道具佔位圖。

### `inn/`：旅館（設計板 InnFit）

| 檔名 | 用途 | blob id |
| --- | --- | --- |
| `lobby` | 旅館大廳（800，原圖 1280） | `c95d61d5` |
| `innkeeper` | 老闆娘瑪莎（440，原圖 490） | `047c012e` |
| `counter-wood` | 木台的木紋（650） | `6884370a` |
| `counter-edge` | 木台上緣的櫃檯木條（360，原圖 1200） | `d137517f` |
| `door-plate` | 客房／自動住宿的門牌分頁（360，原圖 600） | `7d42c264` |
| `parchment` | 入住詳情的羊皮紙（731） | `75aeed6c` |
| `pin` | 羊皮紙上的圖釘（64） | `59eb4a17` |
| `close` | 羊皮紙的關閉鈕（80） | `f1386931` |

紅印章、對話框沿用 `title/seal`、`title/bubble`，錢袋沿用 `town/purse`。房型徽章與房間背景是內容圖：`room-badges/<房型 id>`、`room-backgrounds/<房型 id>`，用 `import` 上傳 R2。

### `companions/`：同伴（設計板 CompanionsFit）

| 檔名 | 用途 | blob id |
| --- | --- | --- |
| `barn` | 入口上門與獸欄的穀倉背景（768） | `3c3ccab8` |
| `guild` | 入口下門與傭兵所的公會牆背景（768） | `9de43cfb` |
| `mira` | 獸欄管理員米菈（471） | `b70e140f` |
| `barton` | 傭兵所長巴頓（480，原圖 645） | `cd486224` |
| `stall-gate` | 獸欄柵門（260，原圖 420） | `49cac5a2` |
| `name-plate` | 柵門上的黃銅名牌（200，原圖 600） | `c368fe3d` |
| `fruit-vigor`、`-aroma`、`-sweet`、`-wild` | 果實槽的活力（AT）、香氣（MT）、甜味（DEF）、野性（出擊值）果實（80） | `6660c17b`、`4c5d9e53`、`bbe61dc3`、`da18363b` |
| `pet-silhouette` | 通用寵物剪影：寵物沒有怪物內容圖時用（260，原檔 `.design-work/out/pet-silhouette-a.png` 752） | `c7c7e874` |

門牌、木台、羊皮紙、圖釘、關閉鈕沿用 `inn/`，印章、對話框沿用 `title/`，錢袋沿用 `town/purse`。名冊上的勳章是道具內容圖 `items/item-1304`（大陸勳章 `1e8f0f15`）、`items/item-1305`（王國勳章 `88ffe03e`）。

### `honour/`：勳章所（設計板 HonourFit）

| 檔名 | 用途 | blob id |
| --- | --- | --- |
| `tag` | 陳列板上的麻繩吊牌（原圖 189） | `13deafb3` |
| `rack-frame` | 陳列板木框（`border-image` 切 110，不縮，564） | `3677319e` |
| `felt` | 陳列板的綠絨布（375） | `c85cc481` |
| `medal-case` | 櫃檯上的絲絨勳章盒（750，原圖 900） | `fed58e66` |
| `mercenary-helmet` | 傭兵吊牌與詳情的紅羽頭盔（80，原檔 `.design-work/out/hn-merc-a.png` 640） | `9ae2440a` |

榮譽廳背景、莉瑟、對話框、印章沿用 `title/hall`、`title/clerk`、`title/bubble`、`title/seal`；櫃檯與木條沿用 `inn/counter-wood`、`inn/counter-edge`；羊皮紙、吊牌圖釘、關閉鈕沿用 `inn/parchment`、`inn/pin`、`inn/close`；僱用中紙條沿用 `title/book-page`；分區銘牌沿用 `companions/name-plate`。五枚勳章是道具內容圖 `items/item-1304`、`1305`、`1306`（雨林，重產的 199×420 `.design-work/work/honour/hn-medal-rainforest.png`）、`1307`（王城 `644c74e7`）、`2222`（白銀 `f2285d8c`），用 `import` 上傳 R2。

## 提示詞

日系手繪（設計畫布 EBO DEMO）的提示詞在 [`prompts/demo/`](prompts/demo/README.md)；道具描述檔在 `prompts/items/`。`prompts.md` 是舊的深藍夜色舞台範本，只留作參考。
