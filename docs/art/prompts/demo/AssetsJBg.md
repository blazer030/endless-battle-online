# 素材庫・背景：產圖提示詞

畫布素材庫這塊板子裡每張素材的產圖紀錄。提示詞是當時實際送給產圖模型的原文；
Gemini 會把需求改寫成自己的提示詞，所以另外附上我們送出的需求。說明見 [README](README.md)。

## 世界地圖（現用）

- 素材：`/_blob/aed12da42cdbb82403b225c7f00950ef`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 3:4，紀錄 g052
- 對應方式：縮圖 0.1

```text
A hand-painted watercolor fantasy world map viewed from a top-down cartographic perspective, rendered on lightly aged warm cream parchment paper. Japanese storybook illustration style with delicate fine ink linework, soft watercolor gradients, and gentle pastel washes. Surrounded by tranquil pale blue ocean waters with subtle painted waves. There are five clearly separated distinct landmasses: 1. In the CENTER, a large lush green temperate continent featuring open grassy rolling plains, a murky dark swamp with cypress trees, a winding stone-paved road leading towards a majestic tall wizard tower, and sparkling crystal caves glowing faintly at its southern coast. 2. To the NORTH-WEST, a floating magical kingdom island with glowing enchanted forest trees, a mysterious dark cave entrance, and an ornate fairytale castle floating suspended in the sky above fluffy white clouds. 3. To the EAST, a vast tropical jungle continent covered in dense misty rainforest canopies, a dramatic deep chasm gorge, and crowned by towering snow-capped mountain peaks. 4. To the SOUTH, a vast scorched golden sand desert featuring crumbling ancient stone ruins, an abandoned battlefield with scattered relics, a rugged coastline with wooden shipwrecks, and an ominous crimson-hued demon realm with glowing volcanic embers. 5. In the far SOUTH-EAST corner, an ominous small isolated forbidden island shrouded in eerie swirling violet and purple mist, harboring a sinister black stone temple fortress. Beautiful whimsical fantasy cartography, purely pictorial illustration, absolutely no text, no letters, no labels, no compass rose, no decorative border frame.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-worldmap'. Image brief: A hand-painted watercolor fantasy world map on aged cream parchment, top-down view, portrait aspect ratio 3:4, Japanese storybook illustration style with delicate ink outlines and soft pastel washes, surrounded by blue sea. Five clearly separated landmasses: CENTER a green temperate continent with open grassland plains, a dark murky swamp, a long stone road leading to a tall tower, and glittering crystal caves in its south; NORTH-WEST a magical kingdom island with an enchanted forest, a cave entrance and a castle floating in the sky above clouds; EAST a tropical jungle continent with misty rainforest, a deep chasm cave and snowy mountain peaks; SOUTH a scorched sandy desert with ancient ruins, an old battlefield, a shipwreck coast and a crimson demon realm; far SOUTH-EAST corner a small dark forbidden island shrouded in purple fog with a black temple. No text, no labels, no compass rose, no border frame. Reply with only the saved file path.
```

</details>

## 城鎮全景（首頁＋市集＋公會前）（現用）

- 素材：`/_blob/cbcc9c3b5b663f0bb9b4fea579506a1e`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 16:9，紀錄 g036
- 對應方式：裁切比對 19.2（人工確認）
- 參考圖：`jp_pano_1792899763993.jpg`

```text
Based on this image, remove all text and lettering completely. Every signboard and banner must be completely free of words, letters, and text (clean blank wooden surfaces or pictorial symbols only). Remove the cut-off building on the far left edge so the left side is a completely open stone plaza with the fountain and the stone town gate leading out to rolling green hills. Keep the same Japanese fantasy RPG watercolor and gouache painterly art style, gentle pastel tones, and soft morning lighting. Ensure no characters, no text, no typography, and no UI elements.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-pano'. Image brief: A wide panoramic hand-painted Japanese fantasy RPG background, landscape aspect ratio about 2:1, soft watercolor and gouache texture, pale airy pastel palette, gentle morning light, no characters, no text, no UI. One continuous scene of a small medieval European-style town seen from street level. LEFT HALF: a quiet open stone plaza with a small fountain and an arched town gate opening onto green fields and distant hills. RIGHT HALF: a lively street lined with distinct shops side by side: a cozy two-story inn with a hanging bed-and-mug signboard, a general store with a striped awning and crates of goods, a blacksmith forge with an open front, anvil and chimney smoke, a stone guild hall with banners and a notice board. Lower part of the image is simple cobblestone ground with little detail. Reply with only the saved file path.
```

</details>

## 裝備間（背包頁）（現用）

- 素材：`/_blob/2a4145eedb3c24a4c3210db280a8f652`
- 產圖：grok，2026-10-29，比例 9:16，紀錄 g107
- 對應方式：縮圖 0.1

```text
Vertical background illustration for a fantasy RPG mobile game equipment screen: the interior of a cozy adventurer's armory and dressing room in an inn, wooden plank walls, a weapon rack with swords and a spear on the left, an empty armor stand and a hanging cloak on the right, a sturdy wooden chest and leather bags on the floor, a small window with soft morning light, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm sepia and faded teal palette, light and airy with lots of pale low-contrast space in the center for UI overlay, no people, no text, no border, no frame
```

## 公會大廳（轉職列表）（現用）

- 素材：`/_blob/152ce6e1a3cb1b5a8104d495d49dcc8e`
- 產圖：grok，2026-10-26，比例 9:16，紀錄 g078
- 對應方式：裁切比對 20.0（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette. The interior of an old adventurers' guild hall seen from inside: tall stone walls hung with faded cloth banners bearing class emblems (crossed swords, a staff, a dagger, a lute), wooden beams, a large arched window letting in soft golden light, a notice board, quiet and atmospheric, no people, no characters, no text. Vertical composition, the lower two thirds are calm and low in detail (plain stone wall and floor) so UI can sit on top.
```

## 轉職場景（12 職業）・戰士（現用）

- 素材：`/_blob/ccb8c45552ef4dca695ebdbd5dee651c`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g080
- 對應方式：裁切比對 9.5（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a sunlit stone fortress training yard with racks of swords and shields, red banners.
```

## 轉職場景（12 職業）・術士（現用）

- 素材：`/_blob/a1287278a7ca7309531b1c9cb9bd0c77`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g085
- 對應方式：裁切比對 10.8（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a quiet arcane study inside a stone tower, shelves of spellbooks, a lectern with an open tome, blue twilight through a round window.
```

## 轉職場景（12 職業）・盜賊（現用）

- 素材：`/_blob/1d95f98103d774bdc34fde350870b367`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g083
- 對應方式：裁切比對 10.7（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: moonlit tiled rooftops of a medieval town at night, lanterns in narrow alleys below.
```

## 轉職場景（12 職業）・樂手（現用）

- 素材：`/_blob/92694d06fe8b43d4dcc2e829cf1e687b`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g082
- 對應方式：裁切比對 7.2（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a cozy tavern stage with a lute on a stool, warm lantern light, wooden beams.
```

## 轉職場景（12 職業）・狂戰士（現用）

- 素材：`/_blob/001f3ffa99fd5552a2b78405f6195284`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g074
- 對應方式：裁切比對 6.1（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a scorched battlefield at dusk, broken spears, smoke and ember-red sky.
```

## 轉職場景（12 職業）・騎士（現用）

- 素材：`/_blob/d614f3175105f1a98886c7a9adb510ed`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g073
- 對應方式：裁切比對 8.5（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a grand castle gate with a drawbridge, heraldic shields and blue banners, clear morning sky.
```

## 轉職場景（12 職業）・魔導師（現用）

- 素材：`/_blob/448dea108accdc76d200fe14e1a1c9ba`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g075
- 對應方式：裁切比對 6.2（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: the top of a wizard tower above the clouds, a starry violet night sky, floating stone rings.
```

## 轉職場景（12 職業）・風水師（現用）

- 素材：`/_blob/432e298e178e30302a2af4fcf1b110e9`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g084
- 對應方式：裁切比對 6.8（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: ancient standing stones on a green misty hillside, carved runes, flowing river in the valley.
```

## 轉職場景（12 職業）・刺客（現用）

- 素材：`/_blob/cd49fb23c5ae47add2ad55cf400b8cd9`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g077
- 對應方式：裁切比對 11.7（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a dark gothic cathedral interior lit by a single shaft of moonlight, shadows, purple-grey tones.
```

## 轉職場景（12 職業）・武術家（現用）

- 素材：`/_blob/913688361d5b6efe308b0cab33517e7e`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g081
- 對應方式：裁切比對 11.1（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a mountain monastery courtyard with wooden training posts, cherry blossoms, distant peaks.
```

## 轉職場景（12 職業）・舞者（現用）

- 素材：`/_blob/364ca49932cdda150df13cfd66dba084`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g079
- 對應方式：裁切比對 6.8（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a desert festival plaza at sunset, silk drapes and lanterns, sand-colored arches.
```

## 轉職場景（12 職業）・獸師（現用）

- 素材：`/_blob/4682791a35a5df8ed939c85f0dc4c93e`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g076
- 對應方式：裁切比對 9.6（人工確認）

```text
Fantasy RPG background painting, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm natural palette, gentle light, no people, no characters, no text, wide panoramic composition with the main subject in the middle horizontal band. Scene: a deep green forest clearing with giant mossy trees and paw prints, dappled sunlight.
```

## 商店街（商店列表頁）（現用）

- 素材：`/_blob/76238397fc55dd9fe559257f1cb22a7b`
- 產圖：grok，2026-10-29，比例 9:16，紀錄 g125
- 對應方式：縮圖 0.1

```text
Vertical background illustration for a fantasy RPG mobile game shopping street menu: a lively cobblestone market street in a medieval fantasy town seen from street level, rows of small shop fronts on both sides with wooden hanging signboards without letters, awnings, lanterns, flower boxes, crates and barrels, a distant clock tower, soft morning light, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, light and airy with low contrast so UI cards can sit on top, no people, no text, no letters, no border, no frame
```

## 商店內景（7 家，Lv1～7）・轉角雜貨鋪（現用）

- 素材：`/_blob/6759ff98b81463893430d3e88cc02dc8`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g118
- 對應方式：裁切比對 10.0（人工確認）

```text
A cozy cluttered village general store: wooden shelves of jars, barrels, sacks of grain, hanging lanterns, rope coils, a small counter, warm afternoon light. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・鐵砧鍛冶屋（現用）

- 素材：`/_blob/b27556a0d1a283ce71ee44bebf4dac8f`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g117
- 對應方式：裁切比對 9.8（人工確認）

```text
A blacksmith armory shop: iron swords and steel helmets displayed on a stone wall, an anvil and a glowing forge in the back, wooden weapon racks. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・月銀細工坊（現用）

- 素材：`/_blob/adcf1d1f30e619957eed9f568cf10186`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g116
- 對應方式：裁切比對 11.8（人工確認）

```text
An elegant silversmith boutique: silver armor and engraved rune staffs in glass display cases, moonlight through tall arched windows, pale blue accents. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・冒險者補給站（現用）

- 素材：`/_blob/fbc2c13fb1ddccedd8fcc1c14bbb0e73`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g115
- 對應方式：裁切比對 11.2（人工確認）

```text
An adventurer supply depot: maps pinned on the wall, backpacks, potion bottles on shelves, rope and lanterns, a big notice board, sturdy wooden counter. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・魔導具工房（現用）

- 素材：`/_blob/fe71ba955657b0bc3e5d97b4c4d4c2da`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g122
- 對應方式：裁切比對 8.9（人工確認）

```text
A magic tool workshop: floating spell books, glowing potion flasks, crystal orbs, brass instruments, purple and teal glows, alchemy table. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・星鐵商會（現用）

- 素材：`/_blob/1b8eab339843fe54dafc90fa50e0d370`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g121
- 對應方式：裁切比對 11.3（人工確認）

```text
A grand merchant guild trading hall: chandeliers, marble floor, star-steel armor on mannequins, velvet curtains, gold trims, luxurious display. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 商店內景（7 家，Lv1～7）・龍鱗秘庫（現用）

- 素材：`/_blob/1d419394600f707991374e247e2a0709`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g123
- 對應方式：裁切比對 11.6（人工確認）

```text
An ancient dragon treasure vault shop: dragon scale armor, legendary weapons on pedestals, piles of gold, ancient stone arches, ember glow. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG shop interior, wide view, empty space in the left third for a shopkeeper to stand, no people, no text, no signs with letters
```

## 戰鬥舞台背景・中央平原（現用）

- 素材：`/_blob/11673b81fb1e9aeb6efab8f3752f0b46`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g256
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a green meadow with gentle rolling hills and light blue sky, horizon at about 45 percent height, eye-level view. Keep it simple: the grass ground is painted as broad soft washes with only a few short grass strokes near the bottom edge, no individual grass blades elsewhere; only three or four trees, each a rounded fluffy blob with just a few soft leaf dabs, no detailed foliage; distant mountains as two soft color layers with very little shading; two or three simple puffy clouds; only a few small flower dots; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・試鍊洞窟（現用）

- 素材：`/_blob/0f08fe1bae8f0bfa359f944a452f641a`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g258
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: inside a cave tunnel lit by a few warm hanging lanterns on the rock walls, a few small glowing crystals, an earthen rocky floor, the far tunnel fading into soft shadow, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・黑暗沼澤（現用）

- 素材：`/_blob/8cee66f05cb76d339b03e9fba839c6cd`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g260
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a gloomy swamp with murky green water pools, a few twisted bare trees with hanging moss, soft low mist, muted olive and dusky violet tones, muddy ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・迷霧森林（現用）

- 素材：`/_blob/94987ca0a2fe362b99ad2577a9f54292`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g259
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a misty forest clearing, tall straight tree trunks fading into pale fog behind, soft diffused light, mossy ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・古代遺跡（現用）

- 素材：`/_blob/83ffdec1bfb65eb2654251baa8825e90`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g261
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: ancient stone ruins with a few broken pillars and a crumbling arch overgrown with vines, cloudy grey-blue sky, old paving stones mixed with grass on the ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・久遠戰場（現用）

- 素材：`/_blob/cd7a21019b275b02e6eabc6114129d12`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g262
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: an old abandoned battlefield on a reddish ochre wasteland, a few broken spears and tattered banners stuck in the ground, worn boulders, hazy dusty sky, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・王者之路（現用）

- 素材：`/_blob/579d514abb712749938125647e19503d`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g265
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a long ancient stone road leading toward a tall distant tower, rocky cliffs on both sides, a faint orange lava glow in a far chasm, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・幻獸森林（現用）

- 素材：`/_blob/870f1f530f3b72cba809e760f2168835`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g263
- 對應方式：縮圖 0.2

```text
Battle stage background for a side-view 2D RPG: an enchanted forest with large round trees and a few giant mushrooms, soft glowing light motes floating in the air, magical green and teal tones, soft grassy ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・星河異界（現用）

- 素材：`/_blob/faf370aad55888fceb52bdd5de9015d1`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g264
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: an otherworldly hilltop under a starry night sky with a soft pale galaxy band and a few falling meteors, small floating rocks, gentle lavender and blue tones that are not too dark, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・灼熱荒漠（現用）

- 素材：`/_blob/4e406d5d3260bc30f108fec13ef25e37`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g267
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a hot desert with soft rolling sand dunes, a faint mirage shimmer on the horizon, a few rocks and dry shrubs, pale warm hazy sky, sandy ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・無淵洞窟（現用）

- 素材：`/_blob/437894eb4e9d053106fd9df514309d5e`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g266
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a deep cavern with stalactites, a large crack in the ground in the far background, faint green poisonous mist, dim but soft lighting, rocky floor, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・天空之城（現用）

- 素材：`/_blob/52e3d9ac1b2a03ded606d7f3399f76b6`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g268
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a city in the clouds, white marble terraces and slender spires on floating islands, bright sky with soft clouds below, a white stone plaza as the ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・水晶之間（現用）

- 素材：`/_blob/77fdb43dfe95954ee274f8abf2b4d1b6`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g269
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a crystal cavern with large pale blue and lavender crystal clusters, soft shimmering light, a smooth crystal and stone floor, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・失落古船（現用）

- 素材：`/_blob/352ca3ad8451a769981bc447194eab24`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g272
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a shoreline with a few old wrecked wooden ships lying on the sand, shallow tide water, dusky evening sky, sandy beach ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・最果之島（現用）

- 素材：`/_blob/c88b356840751bcbf616488a7bea995e`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g271
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a remote tropical island, a golden beach with a few palm trees, dense jungle and a rugged mountain peak behind, sandy ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・冷峰寒地（現用）

- 素材：`/_blob/2eb0c352a11da303b79fd1333b0b4d11`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g273
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a snowy tundra, snow covered ground, icy mountain peaks in the distance, light snowfall, pale blue and white tones, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・廢棄洞窟（現用）

- 素材：`/_blob/da6f1d0e7baa4b17e5ac1c9cd9c44cb3`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g270
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: an abandoned mine tunnel with old wooden support beams, rusty minecart rails, wet rock walls, a dim lantern, rocky floor, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・日沒碉堡（現用）

- 素材：`/_blob/f87ed4f17b237cc9d0582cc1bd01c3c0`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g276
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a massive stone fortress wall with spiky battlements and a tall gear tower, warm orange and purple sunset sky, stone courtyard ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・靜止之城（現用）

- 素材：`/_blob/ece5ecdc36deb078491835b8aac64517`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g275
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: an abandoned city frozen in time, a large stopped clock tower, a few pieces of debris floating still in the air, faded sepia tones, cobblestone street ground, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・黑曜神廟（現用）

- 素材：`/_blob/7ed1d843f53265e010a9780bc331d378`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g274
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a temple of black obsidian with tall dark glossy pillars and soft purple torch flames, polished dark stone floor, kept soft and not too dark, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 戰鬥舞台背景・血之魔域（現用）

- 素材：`/_blob/900424847cba132afb8ec727f585f054`
- 產圖：grok，2026-11-08，比例 1:1，紀錄 g277
- 對應方式：縮圖 0.1

```text
Battle stage background for a side-view 2D RPG: a demonic realm under a dark red sky, a few cross-shaped gravestones, a crimson temple ruin in the distance, scorched dark ground, kept soft and not too dark, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text
```

## 城鎮全景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/b1b11a9cc368272acf36109f3972f896`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 16:9，紀錄 g037
- 對應方式：縮圖 0.2
- 參考圖：`jp_pano_1792899787950.jpg`

```text
On the wooden notice board frame on the far right, remove the text 'Guild Hall' so the wooden top header is completely blank wood with no lettering or text. Keep every other detail identical: the same hand-painted watercolor style, soft pastel palette, empty cobblestone plaza, fountain, arch gate opening to distant hills, inn, store with awning, blacksmith forge with anvil and smoke, guild hall with banners, and notice board. Absolutely no words, no letters, no characters, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-pano'. Image brief: A wide panoramic hand-painted Japanese fantasy RPG background, landscape aspect ratio about 2:1, soft watercolor and gouache texture, pale airy pastel palette, gentle morning light, no characters, no text, no UI. One continuous scene of a small medieval European-style town seen from street level. LEFT HALF: a quiet open stone plaza with a small fountain and an arched town gate opening onto green fields and distant hills. RIGHT HALF: a lively street lined with distinct shops side by side: a cozy two-story inn with a hanging bed-and-mug signboard, a general store with a striped awning and crates of goods, a blacksmith forge with an open front, anvil and chimney smoke, a stone guild hall with banners and a notice board. Lower part of the image is simple cobblestone ground with little detail. Reply with only the saved file path.
```

</details>

## 城鎮全景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/75b36d0b0e5bac92b49c21a521db2fcd`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 16:9，紀錄 g228
- 對應方式：縮圖 0.1
- 參考圖：`ref-town.png`

```text
Redraw the exact scene from the reference image with identical composition, camera perspective, and layout. Every element must be preserved in the exact same placement: the round stone fountain in the cobblestone square, the ancient stone archway gate on the left opening onto the winding dirt path through rolling hills, pine trees and distant mountains, the row of charming medieval half-timbered houses, the market stall with striped awning, the blacksmith forge with anvil and stone chimney, and the large stone guild hall on the right with hanging heraldic shield banners and a wooden notice board in front. Maintain the clean hand-drawn ink linework and pastel watercolor illustration style. Change the time of day to a beautiful early evening sunset / dusk: the sky is filled with glowing warm orange, peach, and soft pink tones with gentle purple clouds; the distant mountains are in soft violet silhouettes; long soft shadows stretch across the cobblestone plaza; windows in the houses and the forge hearth glow with warm golden interior light. Clean, cozy, peaceful storybook fantasy atmosphere, no people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'town-dusk'. Pass this file to it as the reference image: .design-work/work/town/ref-town.png. Image brief: Redraw the reference with EXACTLY the same composition and layout — every building, the stone archway gate, the fountain, the market awning, the forge, the guild hall with banners and the notice board in the same position and size — and the same clean hand-drawn ink linework and fresh pastel watercolor style. Change only the time of day and lighting: early evening sunset, the sky glowing warm orange and soft pink with purple clouds, long soft shadows, the first windows and the forge furnace starting to glow warmly, mountains in soft violet silhouette. Keep it clean, tidy and pleasant, not gritty or realistic. No people, no text. Wide aspect ratio 16:9. Reply with only the saved file path.
```

</details>

## 城鎮全景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/442212596f103b9aba2d93da431eb43f`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 16:9，紀錄 g229
- 對應方式：縮圖 0.2
- 參考圖：`ref-town.png`

```text
Maintain the exact composition, building architecture, perspective, and hand-drawn ink linework with soft watercolor storybook art style from the reference image ref-town.png. Redraw the exact same scene at night time:
- Layout and elements are identical: on the left, the stone archway gate opening to the winding path, distant hills and mountains; in the lower left, the small round stone fountain; on the right side, the row of timber-framed medieval buildings including the inn with hanging sign, the general store with blue-and-white striped awning, the blacksmith forge with chimney and brick furnace, and on the far right the stone guild hall with heraldic banners and wooden notice board.
- Night atmosphere: clear deep indigo and navy blue night sky sprinkled with gentle stars and a luminous glowing crescent or full moon casting soft pale blue moonlight across the town square and cobblestones.
- Warm illumination: glowing golden light shining from all the building windows, an illuminated warm lantern hanging next to the inn sign, a cozy warm orange glow radiating from the blacksmith forge furnace with faint embers and soft smoke rising from the chimney.
- The fountain pool glimmers with pale moonlight and blue water reflections.
- Soft cool blue and violet shadows across the cobblestone plaza, contrasted by warm amber lantern and window light.
- A peaceful, cozy, storybook nocturnal atmosphere, illuminated and clearly visible, not pitch dark or gloomy.
- Clean ink line art, charming watercolor wash, no people, no text on signs.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'town-night'. Pass this file to it as the reference image: .design-work/work/town/ref-town.png. Image brief: Redraw the reference with EXACTLY the same composition and layout — every building, the stone archway gate, the fountain, the market awning, the forge, the guild hall with banners and the notice board in the same position and size — and the same clean hand-drawn ink linework and fresh pastel watercolor style. Change only the time of day and lighting: a calm night, deep blue sky with stars and a bright moon, soft blue moonlight on the cobblestones, warm golden light glowing from the windows, a lantern by the inn sign, the forge furnace glowing orange, the fountain water reflecting moonlight. Keep it bright enough to read clearly, a gentle storybook night rather than pitch dark. Keep it clean, tidy and pleasant, not gritty or realistic. No people, no text. Wide aspect ratio 16:9. Reply with only the saved file path.
```

</details>

## 標題城鎮遠景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/e6eb4eb9925d12391e20e223b393b625`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g226
- 對應方式：縮圖 0.1
- 參考圖：`ref-grok.png`、`ref-town.png`

```text
An anime watercolor landscape illustration combining the exact composition and scene of Reference A with the clean, bright storybook art style of Reference B. High angle portrait view: in the foreground, a winding dirt and stone path descends down a gentle grassy hill towards a fortified medieval fantasy town. The town is surrounded by a tidy stone curtain wall with a central arched gatehouse. Inside the town are clustered charming half-timbered buildings with neat terracotta and slate tile roofs. On a lush green hill rising in the background to the right sits a grand fairytale castle with slender towers and spires. The vast dawn sky above is filled with soft watercolor clouds, a gentle warm golden sunrise glow rising from the left horizon, and distant birds flying. Art style strictly follows Reference B: clean hand-drawn fine ink linework, soft smooth transparent watercolor washes, bright fresh pastel color palette, cheerful and airy storybook atmosphere, tidy and well-kept architecture and foliage, not gritty, not dark, not weathered, not realistic. No people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'tt-town-mix'. Pass these two files to it as reference images: .design-work/work/title/ref-grok.png (reference A) and .design-work/work/title/ref-town.png (reference B). Image brief: Redraw the scene of reference A — the same composition: a hillside dirt road winding down to a walled medieval town with a stone gate, a castle with towers on the hill behind, birds in a big dawn sky, the same sense of scale and grandeur and warm golden sunrise glow — but in the art style of reference B: clean hand-drawn ink linework, soft light transparent watercolor washes, bright fresh pastel palette, tidy and well-kept buildings, airy and cheerful, not gritty, not weathered, not realistic. Keep the golden sunrise as a gentle warm glow rather than heavy haze. No people, no text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 標題城鎮遠景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/815cd4a2c965260bb6e9c7cddc272feb`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g230
- 對應方式：縮圖 0.2
- 參考圖：`ref-mix.png`

```text
In the exact same composition, layout, framing, and clean hand-drawn ink linework with fresh pastel watercolor style as the reference image: A vertical portrait view of a winding dirt and stone path in the foreground bordered by lush green grass, wildflowers, and stones, leading up toward a fortified medieval town wall with an arched stone gate flanked by round twin towers in the center. Inside the stone walls are charming densely clustered medieval half-timbered houses with reddish-terracotta tiled roofs and small chimneys with faint smoke curls. On the high green forested hill on the right behind the town stands an elegant fairy-tale medieval castle with slender spires, stone turrets, and battlements. Rolling green hills and a gentle valley stretch into the distance on the left. The time of day is a clear, bright late morning with clean crisp sunlight illuminating the scene: a vibrant, fresh cerulean blue sky overhead with a few soft, fluffy white cumulus clouds, vivid saturated green grass and lush hillsides, pleasant sunlit stone textures and bright warm roof tiles. Beautiful, clean, tidy, whimsical storybook aesthetic, pure watercolor and ink drawing, no people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'tt-day'. Pass this file to it as the reference image: .design-work/work/title/ref-mix.png. Image brief: Redraw the reference with EXACTLY the same composition and layout — the hillside road, the town walls and gate, the houses, the castle on the hill, the valley and the sky in the same position and size — and the same clean hand-drawn ink linework and fresh pastel watercolor style. Change only the time of day and lighting: a clear bright late morning, fresh blue sky with a few soft white clouds, crisp clean sunlight, vivid green hills. Keep it clean, tidy and pleasant, not gritty or realistic. No people, no text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 標題城鎮遠景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/67e2bf513a1fedf9ac051dfccb80a9d4`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g231
- 對應方式：縮圖 0.2
- 參考圖：`ref-mix.png`

```text
In the exact same composition, perspective, framing, and clean hand-drawn ink linework with pastel watercolor style as the reference image: a fairytale medieval walled town with half-timbered houses and gatehouse, viewed from a winding hillside path in the foreground, with a grand multi-towered stone castle perched on the wooded hill on the right, rolling hills in the distance. Modify the lighting and time of day to early evening sunset at dusk: the sun rests low on the horizon casting a warm radiant orange, apricot, and soft rose-pink glow across the sky with soft purple and mauve evening clouds. The rolling hills, rooftops, and landscape cast soft lavender and violet dusk shadows. The first few cottage windows and gatehouse windows in the town begin to glow with warm golden lantern light. Soft chimney smoke rises into the tranquil twilight sky. Clean, tidy, whimsical storybook aesthetic, fresh watercolor wash, delicate ink lines, charming and pleasant. No people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'tt-dusk'. Pass this file to it as the reference image: .design-work/work/title/ref-mix.png. Image brief: Redraw the reference with EXACTLY the same composition and layout — the hillside road, the town walls and gate, the houses, the castle on the hill, the valley and the sky in the same position and size — and the same clean hand-drawn ink linework and fresh pastel watercolor style. Change only the time of day and lighting: early evening sunset, the sky glowing warm orange and soft pink with purple clouds, the sun low on the horizon, the first windows in town starting to glow warmly, hills in soft violet shade. Keep it clean, tidy and pleasant, not gritty or realistic. No people, no text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 標題城鎮遠景・清晨／白天／黃昏／夜晚（現用）

- 素材：`/_blob/6a33dc7e4c91ab8b388f66b2b62ac23e`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g232
- 對應方式：縮圖 0.2
- 參考圖：`ref-mix.png`

```text
A night-time version of the reference image, maintaining the exact same composition, perspective, camera angle, and illustration style. Clean delicate hand-drawn ink linework with soft watercolor washes, charming storybook fantasy aesthetic. In the foreground, a winding dirt path leads through grassy hills toward a medieval walled town with stone gate towers and an arched gate. Inside the walls are clustered medieval half-timbered houses with gabled roofs and smoking chimneys. On the prominent hill to the upper right stands the majestic fairytale castle with spires and stone towers, overlooking the valley. Deep blue starry night sky with a luminous bright moon above the castle. Soft blue moonlight bathes the landscape, hills, and castle. Warm golden light glows from the windows of the houses in the town and the castle windows. A warm glowing lantern hangs at the town gate entrance. The scene is bright and clear like a magical storybook night, not pitch dark or gritty. No people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'tt-night'. Pass this file to it as the reference image: .design-work/work/title/ref-mix.png. Image brief: Redraw the reference with EXACTLY the same composition and layout — the hillside road, the town walls and gate, the houses, the castle on the hill, the valley and the sky in the same position and size — and the same clean hand-drawn ink linework and fresh pastel watercolor style. Change only the time of day and lighting: a calm night, deep blue starry sky with a bright moon above the castle, soft blue moonlight over the hills, warm golden lights glowing from the town windows and the castle, a lantern at the town gate. Keep it bright enough to read clearly, a gentle storybook night rather than pitch dark. Keep it clean, tidy and pleasant, not gritty or realistic. No people, no text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 旅館大廳（現用）

- 素材：`/_blob/c95d61d5a5fe8351d58874f6e13c6929`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g135
- 對應方式：縮圖 0.1

```text
A warm cozy fantasy village inn common room: wooden beams, a stone fireplace with glowing fire, a reception counter with a guest ledger and a brass bell, room keys hanging on a pegboard, a staircase leading up to the guest rooms, soft evening lamplight. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn interior, wide view, empty space in the left third for an innkeeper to stand, no people, no text, no signs with letters
```

## 鐵舖工坊（現用）

- 素材：`/_blob/d3201d8fe36b91b129c738abad4975e6`
- 產圖：grok，2026-10-30，比例 16:9，紀錄 g143
- 對應方式：縮圖 1.4

```text
Interior of a cozy fantasy blacksmith forge workshop seen from the counter: a glowing stone furnace and a heavy iron anvil on the right side, hammers, tongs and finished swords hanging on a timber and stone wall, a quench barrel, warm orange firelight with soft sparks, calm open floor space on the left third, hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, no people, no text
```

## 榮譽廳石牆（現用）

- 素材：`/_blob/12b6be86803b2deaf72e3952f1e7d3ce`
- 產圖：grok，2026-11-06，比例 9:16，紀錄 g196
- 對應方式：縮圖 0.1

```text
Interior wall of a royal honour hall in a cozy fantasy town, warm cream stone wall, tall heraldic banners hanging on both sides (griffin, crown), dark wooden wainscot along the bottom, iron candle sconces, soft afternoon light, a large plain empty area in the middle of the wall hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, no people, no text
```

## 任務板走廊（現用）

- 素材：`/_blob/dbffd3c8f4e0fe17b07238bf70c0fcac`
- 產圖：grok，2026-10-31，比例 9:16，紀錄 g178
- 對應方式：縮圖 0.1

```text
Interior wall of a cozy medieval fantasy adventurers guild hall, timber beams and warm plaster wall, a lantern on the side, wooden floor at the very bottom, warm evening light, plenty of empty wall space in the middle hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, no people, no text
```

## 客房（現用）

- 素材：`/_blob/10caa783e7b368a93cc28f604e1f56d5`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 9:16，紀錄 g209
- 對應方式：縮圖 2.4
- 參考圖：`ref-room.png`

```text
Hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, in the artistic style of the reference image. A cozy, tidy, bright, and welcoming adventurer's private room in a fine inn. Honey-colored wooden walls, a large bright window with afternoon sunlight streaming through and soft light curtains. On the wall is a neat polished weapon rack holding a sword and a spear, and a small heraldic crest banner. A wooden bookshelf with books and a potted plant. A soft woven rug rests on the clean wooden floor. The upper half of the composition focuses on the wall and the window. Peaceful, neat, well cared for. No people, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'ch-room-a'. Pass this file to it as a style reference image: .design-work/work/char/ref-room.png. Image brief: Using the attached room illustration only as the art style reference (hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette), draw a DIFFERENT room that looks cozy, tidy, well cared for and welcoming, not shabby or poor. Scene: A private adventurer's room in a fine inn: warm honey-colored wooden walls, a large bright window with afternoon sunlight and light curtains, a neat polished weapon rack holding a sword and a spear, a small crest banner on the wall, a bookshelf with books and a potted plant, a soft woven rug on a clean wooden floor. The upper half of the picture shows the wall and the window. No people, no text. Portrait aspect ratio 9:16. Reply with only the saved file path.
```

</details>

## 獸欄穀倉（現用）

- 素材：`/_blob/3c3ccab85ded926e13633f043e05bd55`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 9:16，紀錄 g211
- 對應方式：縮圖 0.1
- 參考圖：`ref-scene.png`

```text
In the exact art style of the reference image (hand-drawn ink linework with delicate, visible pen hatching and outlines, soft transparent watercolor washes, subtle paper grain texture, warm muted gentle palette, storybook illustration aesthetic): Interior of a cozy, tidy, well cared for wooden animal stable in a fantasy town, viewed straight on (front-facing symmetrical composition). A back wall of warm rustic wooden planks with neatly hanging leather harnesses, horseshoes, and two warm glowing oil lanterns mounted on the posts. Golden hay bales neatly stacked at both sides framing the room. Fresh clean golden straw covering the wooden plank floor. The center/middle of the back wall and floor is left open, clean, and uncluttered. Soft warm afternoon sunlight streaming in gently, creating warm highlights and soft shadows. Charming fantasy RPG tavern/stable vibe. No people, no characters, no animals, no text, no words.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'cmp-barn'. Pass this file to it as a style reference image: .design-work/work/companions/ref-scene.png. Image brief: Using the attached room illustration only as the art style reference (do not copy its content), draw: Interior of a cozy, tidy, well cared for wooden animal stable in a fantasy town, seen straight on: a back wall of warm wooden planks with hanging leather harness, horseshoes and two oil lanterns, hay bales stacked at both sides, clean straw covering the floor, the middle of the wall left open and uncluttered, soft warm afternoon light hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette. No people, no text. Aspect ratio 9:16. Reply with only the saved file path.
```

</details>

## 傭兵所大廳（現用）

- 素材：`/_blob/9de43cfb29ebc2259ae6f925fe5c0add`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 9:16，紀錄 g218
- 對應方式：縮圖 0.1
- 參考圖：`ref-scene.png`

```text
In the exact art style of the reference image: fine delicate ink linework with visible pen strokes, crosshatching, soft transparent watercolor washes, and subtle paper texture with a warm muted earth-tone palette. A straight-on eye-level view of the interior of a sturdy, well-kept mercenary guild hall in a fantasy town. The back wall is constructed of rough-hewn stone and timber beams. Mounted in the upper center of the back wall is a large, empty dark wooden board (blank, clean dark wood grain, no text or notices). Around the board on the wall are crossed steel swords, round wooden shields bearing simple heraldic crests, and a wooden weapon rack holding spears and swords. Across the bottom foreground stretches a long, thick wooden tavern counter. A warm iron lantern sits on the counter emitting a gentle golden glow. Clean composition, cozy fantasy RPG background art, no people, no characters, no text, no letters.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'cmp-hall'. Pass this file to it as a style reference image: .design-work/work/companions/ref-scene.png. Image brief: Using the attached room illustration only as the art style reference (do not copy its content), draw: Interior of a sturdy, well kept mercenary guild hall in a fantasy town, seen straight on: a stone and timber back wall with crossed swords, round shields bearing crests and a weapon rack, a large empty dark wooden board mounted in the upper middle of the wall, a long thick wooden counter across the bottom, warm lantern light hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette. No people, no text. Aspect ratio 9:16. Reply with only the saved file path.
```

</details>

## 旅館房型（6）・學徒閣樓（現用）

- 素材：`/_blob/0657d0f08b8e3786d56e9b69ee5531fd`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g137
- 對應方式：縮圖 0.1

```text
A cozy small attic guest room under a slanted wooden roof: a simple bed with a patchwork quilt, a small writing desk piled with books, a candle and an open journal, a travel map pinned to the beam, a round attic window with afternoon light. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 旅館房型（6）・修練廂房（現用）

- 素材：`/_blob/fbc71279920655e528a714709276cafd`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g136
- 對應方式：縮圖 0.1

```text
A practice guest room at an inn: a straw training dummy, a wooden rack of practice swords and staffs, a worn sparring mat on the floor, a plain bed in the corner, morning light through a tall window. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 旅館房型（6）・鬥士通鋪（現用）

- 素材：`/_blob/8bb1000ec8a5cfe88f1a5d9345c1dc29`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g141
- 對應方式：縮圖 0.1

```text
A rowdy fighters' bunk room at an inn: rows of sturdy wooden bunk beds, swords and round shields hung on the walls, ale mugs on a crude table, a warm hearth, a hanging lantern. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 旅館房型（6）・星見塔房（現用）

- 素材：`/_blob/18b622f0622f5aa2fb185db1f40b6c28`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g139
- 對應方式：縮圖 0.1

```text
A round tower guest room at an inn: a brass telescope at an arched window showing a starry night sky, star charts pinned to the stone wall, stacks of arcane books, a cozy bed with a deep blue quilt, candlelight. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 旅館房型（6）・鐵壁客房（現用）

- 素材：`/_blob/97fa24a944ff8a342501c7278608afb5`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g138
- 對應方式：縮圖 0.1

```text
A fortified guest room at an inn: thick stone walls, an iron-bound oak door, a heavy four-poster oak bed, a full suit of armor on a stand, a shield above the fireplace, steady warm light. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 旅館房型（6）・靜謐禱室（現用）

- 素材：`/_blob/b3f8428bfb61dff770fe0aa214cf8ee9`
- 產圖：grok，2026-10-29，比例 16:9，紀錄 g140
- 對應方式：縮圖 0.1

```text
A quiet prayer chamber guest room at an inn: a small altar with many lit candles, a stained glass window casting soft colored light, a simple tidy bed with white linen, hanging herbs and a prayer rug, serene atmosphere. hand-drawn ink linework with soft transparent watercolor washes, paper texture, muted warm palette, storybook fantasy RPG inn guest room interior, wide view, no people, no text, no signs with letters
```

## 轉職列表列底圖（12）・戰士（現用）

- 素材：`/_blob/2dd45a0c9e2b7300b751ba65c99452bc`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g080
- 對應方式：裁切比對 18.4（人工確認）
- 提示詞：同上方 g080

## 轉職列表列底圖（12）・術士（現用）

- 素材：`/_blob/2a00c7e0999f49b75fbb7dd76d91a5bd`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g085
- 對應方式：裁切比對 17.5（人工確認）
- 提示詞：同上方 g085

## 轉職列表列底圖（12）・盜賊（現用）

- 素材：`/_blob/8a90811465bb4c6a30333ed605743892`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g083
- 對應方式：裁切比對 16.2（人工確認）
- 提示詞：同上方 g083

## 轉職列表列底圖（12）・樂手（現用）

- 素材：`/_blob/1a9c8a4a1ff793540ee13d76921d9838`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g082
- 對應方式：裁切比對 13.2（人工確認）
- 提示詞：同上方 g082

## 轉職列表列底圖（12）・狂戰士（現用）

- 素材：`/_blob/5c1020db24ae422a3523857056d0ada4`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g074
- 對應方式：裁切比對 13.3（人工確認）
- 提示詞：同上方 g074

## 轉職列表列底圖（12）・騎士（現用）

- 素材：`/_blob/b226b6ea0069ab793923aea80c19f617`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g073
- 對應方式：裁切比對 17.3（人工確認）
- 提示詞：同上方 g073

## 轉職列表列底圖（12）・魔導師（現用）

- 素材：`/_blob/7673ceb372cd50008d41a73344d0e6c8`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g075
- 對應方式：裁切比對 13.2（人工確認）
- 提示詞：同上方 g075

## 轉職列表列底圖（12）・風水師（現用）

- 素材：`/_blob/dcf27bfca0c67baf899410ce741eaaeb`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g084
- 對應方式：裁切比對 14.4（人工確認）
- 提示詞：同上方 g084

## 轉職列表列底圖（12）・刺客（現用）

- 素材：`/_blob/4e4042e45f47ded21347dd81db73d6b2`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g077
- 對應方式：裁切比對 14.0（人工確認）
- 提示詞：同上方 g077

## 轉職列表列底圖（12）・武術家（現用）

- 素材：`/_blob/1b01478aa7e693e6990dad859b8c7b8a`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g081
- 對應方式：裁切比對 21.5（人工確認）
- 提示詞：同上方 g081

## 轉職列表列底圖（12）・舞者（現用）

- 素材：`/_blob/536bfe81016897a035acd7f5926f1071`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g079
- 對應方式：裁切比對 14.7（人工確認）
- 提示詞：同上方 g079

## 轉職列表列底圖（12）・獸師（現用）

- 素材：`/_blob/bae8b3e13ced8ecd3c86594b1a1edf77`
- 產圖：grok，2026-10-26，比例 16:9，紀錄 g076
- 對應方式：裁切比對 16.9（人工確認）
- 提示詞：同上方 g076
