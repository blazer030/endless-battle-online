# DEMO 畫布素材的產圖提示詞

設計畫布（EBO DEMO，日系手繪）素材庫裡每張素材當時實際送出的提示詞。之後要重產或補同系列的圖，從這裡複製原文，畫風才不會跑掉。

[`../../prompts.md`](../../prompts.md) 是導入畫布前的舊畫風範本（semi-realistic），跟這裡不同。

## 檔案

| 檔案 | 內容 |
|---|---|
| `AssetsJ.md` 等 | 一塊素材庫板子一份，依板子上的順序列出素材、產圖工具、日期、比例、參考圖與提示詞全文 |
| `generations.jsonl` | 本機紀錄裡找得到的全部產圖（含沒採用的），一行一筆：`id`、`date`、`tool`、`aspect`、`refs`、`assets`（用到它的畫布素材 blob id）、`prompt`、`request` |

- **grok**：`prompt` 就是送給 image_gen 的原文。
- **Gemini（agy）**：image-generator 子代理會把我們的需求改寫成自己的提示詞；`prompt` 是它實際送出的版本，`request` 是我們送出的需求。重產時兩段都可以參考。
- **對應方式**：`縮圖`、`裁切比對`、`形狀比對` 是把畫布素材跟原圖比對找出來的；註明「人工確認」「人工比對」的是看圖確認過的。一組素材從同一張圖切出來時，會共用同一筆紀錄。
- 2026-11-08 起，`gen.sh` 每產一張都會在 `.design-work/raw/名稱.prompt.txt` 存提示詞。

## 現行畫風範本

### grok：道具、介面物件（`gen.sh … item`）

`{主體}` 換成要畫的東西：

```text
{主體} hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, flat front view, centered, the object fills about 80 percent of the frame, on a solid flat magenta background #FF00FF, no text, no letters, no drop shadow
```

### grok：場景（`gen.sh … scene`）

```text
{主體} hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, no people, no text
```

### grok：戰鬥背景（`gen.sh … stage`，2026-11-08 定案）

`{場景}` 只寫場景本身；21 個地點各自的場景描述見 [`AssetsJBg.md`](AssetsJBg.md) 的戰鬥背景：

```text
Battle stage background for a side-view 2D RPG: {場景}, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

### Gemini：職業立繪（2:3，附戰士立繪當參考圖）

只換【】裡的職業、服裝與武器：

```text
Using the attached warrior illustration as the exact style and character reference, draw the SAME young man as a 【job】. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, the same face, same short messy dark teal-green hair, same amber eyes, same proportions and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit: 【outfit and weapon; any glowing item: "no glow, no sparkles, no light effects around it"】.

Full body, head to boots fully visible, standing three-quarter view, confident relaxed pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

### Gemini：15 格戰鬥精靈圖（3:2，附戰士 15 格精靈圖當參考圖）

```text
Using the attached chibi warrior sprites as the exact style and character reference, draw ONE complete battle sprite sheet of the SAME chibi 【job + outfit + weapon】: same head-to-body ratio, same slim chibi proportions, same painted shading and outline weight, same face, dark teal-green hair, amber eyes. Every frame faces right.

Layout: 15 frames on a grid of 3 rows x 5 columns, evenly spaced with clear empty gaps between all frames, nothing touching or overlapping, nothing crossing into the row above or below. Every frame at exactly the same scale, full body visible, feet on the same baseline within each row.

Row 1:
1. Idle: relaxed battle stance, 【weapon】 held low in front
2. Attack wind-up: 【weapon raised high, body coiled】
3. Dash: running forward fast, leaning in, 【weapon】 held low behind, scarf streaming back
4. Slash: 【weapon swung down and forward through the swing】, front foot planted, full follow-through
5. Spell wind-up: 【weapon】 lowered at his side, free hand pulled back near his chest with fingers curled, gathering power
Row 2:
6. Spell cast: free arm thrust forward with open palm, as if releasing a spell
7. Power-up wind-up: crouching low with knees bent, both fists drawn in close, head slightly down
8. Power-up: feet planted wide, both fists clenched at waist, determined shout
9. Hit reaction: knocked back, upper body bent backward, eyes squeezed shut, arms flung out, 【weapon】 still in hand
10. Dodge: quick sidestep hop backward, body twisted, light on his toes
Row 3:
11. Taking out a potion: holding a small round glass bottle in front of his chest, 【weapon】 resting on his shoulder
12. Drinking: tilting the bottle to his mouth
13. Staggering: stumbling backward, knees buckling, one hand clutching his side, 【weapon】 tip dragging down
14. Defeated: down on one knee, head bowed, leaning on his 【weapon】 planted in front of him
15. Victory: 【weapon】 raised overhead, confident grin

No effects: no glow, no sparkles, no motion lines, no impact stars, no aura, no dust.
Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no noise, no speckles, no checkerboard, no shadow, no ground. No text, no numbers, no frame borders. Aspect ratio 3:2, highest resolution available.
```

## 沒有提示詞的素材

使用者另外提供的提示詞（Gemini 網頁版產的）會直接補進各板子的文件，例如旅館的瑪莎、鐵匠洛岡與商店店員。

下列素材沒有提示詞，也不需要補：

- 墨線花紋框・只留墨線、花紋框・米白線、小地區卡選中：用其他素材改色或修圖做的，沒有另外產圖。
- 戰士 15 格戰鬥精靈圖：做法還沒定案。
- 同伴的交叉旗：小圖示，畫風差一點不明顯。

地圖、背景與立繪全部都有提示詞；這幾類的畫風最需要一致，新增時一定要記下來。
