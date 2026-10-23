# 生圖提示詞範本

提示詞用英文（生圖模型效果較穩），說明用中文。尺寸、構圖、色彩規則見 `README.md`。輸出請要求透明背景 PNG，再用 `scripts/import-art.sh` 匯入。

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
