# 生圖提示詞範本

設計畫布（EBO DEMO，日系手繪）素材的提示詞另外整理在 [`prompts/demo/`](prompts/demo/README.md)。

提示詞用英文（生圖模型效果較穩），說明用中文。尺寸、構圖、色彩規則見 `README.md`。輸出請要求透明背景 PNG，再用 `tools/assets` 的 `pnpm cli import` 匯入（流程見 `README.md`）。下面的範本是舊的深藍夜色舞台風格，日系手繪的提示詞以 `prompts/demo/` 為準。

## 共用風格段

每個範本都以這段開頭：

```text
modern fantasy, semi-realistic painterly illustration, full body, isolated subject on transparent background, rim light in cool cyan or amber gold, readable silhouette at small size, no text, no frame, no ground shadow
```

## 負面提示

```text
text, watermark, signature, cropped feet, multiple subjects, background scenery, chibi, pixel art
```

## 玩家範本

一名冒險者，面向右，腳底貼齊下緣。預設檔 `player.webp` 只用一種裝束。

```text
{共用風格段}, a lone adventurer facing right, standing pose, {outfit}
```

| 裝束 | `{outfit}` |
|---|---|
| 戰士 | sturdy plate and leather armor, longsword at the hip, short cloak |
| 術士 | layered dark robes with glowing rune trim, carrying a crystal staff |
| 盜賊 | hooded light leather armor, twin daggers, scarf, agile stance |
| 樂手 | embroidered traveling coat, lute on the back, feathered hat |

## 怪物範本

面向左（朝向右邊的玩家），腳底貼齊下緣。

```text
{共用風格段}, a monster facing left, {name_en}: {species_description}, {element_motif}, {role_build}, {threat_tier}
```

| 欄位 | 說明 |
|---|---|
| `{name_en}` | 怪物英文名（id 轉詞，如 `meadow-slime` → Meadow Slime） |
| `{species_description}` | 物種外型描述，一兩句 |
| `{element_motif}` | 依下方屬性母題表 |
| `{role_build}` | 依下方角色體型表 |
| `{threat_tier}` | 依下方等級威脅帶 |

### 屬性母題

| element | 屬性 | `{element_motif}` |
|---|---|---|
| 0 | 無 | neutral tones |
| 1 | 地 | brown stone and moss |
| 2 | 水 | deep blue water and foam |
| 3 | 火 | crimson flame and embers |
| 4 | 木 | green leaves and bark |
| 5 | 風 | pale cyan wind wisps |
| 6 | 毒 | violet-green toxic mist |

### 角色體型

| 角色 | `{role_build}` |
|---|---|
| 普通 | balanced build |
| 坦 | heavy armored bulky build |
| 速 | lean agile build |
| 法 | robed, glowing runes |
| 精英 | larger, ornate |
| 任務怪 | boss-scale, crowned |

### 等級威脅帶

| 等級 | `{threat_tier}` |
|---|---|
| Lv1–15 | cute or mundane |
| Lv16–60 | dangerous |
| Lv61+ | mythic |

## 範例

### `meadow-slime`（Lv1、木、普通）

```text
modern fantasy, semi-realistic painterly illustration, full body, isolated subject on transparent background, rim light in cool cyan or amber gold, readable silhouette at small size, no text, no frame, no ground shadow, a monster facing left, Meadow Slime: a glossy round slime with a soft translucent body and two small dark eyes, green leaves and bark, balanced build, cute or mundane
```

### `treasure-goblin`（Lv5、無、普通）

```text
modern fantasy, semi-realistic painterly illustration, full body, isolated subject on transparent background, rim light in cool cyan or amber gold, readable silhouette at small size, no text, no frame, no ground shadow, a monster facing left, Treasure Goblin: a wiry goblin hunched under a small chest strapped to its back, sly grin, patched clothes, neutral tones, balanced build, cute or mundane
```

### `relic-core-colossus`（Lv75、地、任務怪）

```text
modern fantasy, semi-realistic painterly illustration, full body, isolated subject on transparent background, rim light in cool cyan or amber gold, readable silhouette at small size, no text, no frame, no ground shadow, a monster facing left, Relic Core Colossus: an ancient stone golem with a glowing crystal core in its chest and cracked carved plates, brown stone and moss, boss-scale, crowned, mythic
```

## 道具圖示（2026-10-23 畫面 redesign 起）

畫面 redesign 後，道具圖示（之後角色、怪物也會）改走**Q 版卡通**：粗深棕描邊、手繪柔和陰影、暖色高光，跟選單圖示同一套。提示詞分兩段，共用風格段放在 `prompts/items/_style.txt`，每個道具一個檔 `prompts/items/<items.json 的 id>.txt` 只寫一句主體描述；`scripts/art-gen-items.sh` 會把兩段接起來丟給 agy。先把所有道具的描述檔寫齊，再一次批次產，風格才會一致。

共用風格段（`_style.txt`）：

```text
a single fantasy RPG inventory item icon for a mobile game, cute chunky cartoon style, thick dark brown outline, soft hand-painted shading with warm highlights, slightly top-down three-quarter view, the object alone fills about 80 percent of the frame, centered, on a solid magenta background #FF00FF, no text, no frame, no shadow on the background, square 1024x1024.
```

主體描述的寫法：一句話、英文、只講形狀與材質顏色，不要再寫風格詞（風格段已經有），例如：

```text
an iron short sword with a straight grey steel blade, a simple dark iron crossguard, a brown leather-wrapped grip and a small red gem on the pommel, blade pointing up
```

產出後的處理：桃紅去背（含去溢色）→ 裁到不透明邊界 → 保留原始像素，顯示時用 CSS 縮到 44px 左右；不要事先縮圖。去背流程目前在個人 skill `preparing-ai-generated-ui-assets` 的 `magenta_assets.py`（key／trim）。

注意：上面「共用風格段」那節是 redesign 前的半寫實方向，舞台的玩家、怪物圖之後會改成同一套卡通風格再更新。

去背腳本：`python3 scripts/art-key-magenta.py <原圖.png> <輸出.png>`（純標準函式庫；色相判斷去桃紅＋去溢色、清孤立斑點、裁到不透明邊界）。agy 產出的是 JPEG，桃紅底帶壓縮雜訊，一般門檻式去背會留斑點，所以用這支。
