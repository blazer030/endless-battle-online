# 素材庫・立繪（含怪物）：產圖提示詞

畫布素材庫這塊板子裡每張素材的產圖紀錄。提示詞是當時實際送給產圖模型的原文；
Gemini 會把需求改寫成自己的提示詞，所以另外附上我們送出的需求。說明見 [README](README.md)。

## 戰士立繪（現用）

- 素材：`/_blob/bb6799c8abe50cf5d7959adf87687848`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 3:4，紀錄 g030
- 對應方式：形狀比對 17.0（人工確認）

```text
Full-body character concept art of an original young male adventurer swordsman in the style of Japanese fantasy RPG illustration, featuring fine delicate ink linework, soft watercolor cel shading, and gentle painterly highlights. The character stands in a confident three-quarter pose, completely visible from head to boots with generous margin on all sides. He has short messy dark-teal hair and determined amber eyes. He wears light polished steel pauldrons and breastplate armor over a navy blue fabric tunic, a long flowing bright red scarf trailing gracefully, brown leather utility belts and straps, and fitted brown leather adventuring boots. His hand holds a straight steel longsword resting casually against his shoulder. Entire figure is isolated on a completely flat, solid magenta background (#FF00FF) with no shadows on the background, no ground plane, no gradient, and no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-hero'. Image brief: Full-body character illustration of an original young adventurer swordsman in the style of hand-painted Japanese fantasy RPG character art (fine ink linework, soft watercolor cel shading, delicate highlights). Standing confident three-quarter view, short messy dark-teal hair, amber eyes, light steel pauldrons and breastplate over a navy tunic, a long red scarf flowing, brown leather belts and boots, holding a straight longsword resting on the shoulder. The whole figure from head to boots is visible with margin, centered, on a solid magenta background #FF00FF, no text, no shadow on the background. Reply with only the saved file path.
```

</details>

## 術士立繪（現用）

- 素材：`/_blob/bd2e4e5654b1b17dd7f78da8c8bfc32b`
- 產圖：grok，2026-10-26，比例 2:3，紀錄 g065
- 對應方式：人工比對（去背後製）

```text
Japanese fantasy RPG character art, hand-drawn ink line art with soft watercolor wash coloring, light paper-texture shading, muted natural palette, clean anime proportions. Full-body standing portrait, head to boots fully visible, of The same young male hero: short messy dark teal-green hair, amber eyes, calm slight smile, slim build. He is now a sorcerer: deep navy hooded robe with a short cape, brass clasps, brown leather belt with pouches and a small spellbook, holding a tall wooden staff topped with a glowing pale blue crystal, knee-high brown leather boots. Three-quarter view facing slightly left, centered with margin around the figure. Isolated on a perfectly flat uniform pure magenta background #FF00FF, no gradient, no drop shadow, no cast shadow, no ground, no glow on background. No text.
```

## 盜賊立繪（現用）

- 素材：`/_blob/d6d70ee0c836d7370e8dca61edb3059a`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g233
- 對應方式：形狀比對 31.8（人工確認）
- 參考圖：`ref-warrior.png`

```text
Using the attached warrior illustration as the exact style and character reference, draw the SAME young man as a nimble thief. Art style must precisely match the reference: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, identical facial features, same short messy dark teal-green hair, same amber eyes, same body proportions and line weight. Hand-drawn traditional fantasy RPG watercolor illustration, not digital 3D or overly polished. Character outfit: dark charcoal hooded short cloak resting around his shoulders with the hood down, a fitted brown leather jerkin over a dark tunic shirt, fingerless leather gloves, a leather belt with multiple small pouches and a sheathed second dagger, slim dark trousers tucked into soft quiet leather boots. He holds a short curved steel dagger loosely in his right hand. Full body standing three-quarter view, head to boots fully visible in frame, confident relaxed thief pose, centered with breathing room around the figure. Background: perfectly flat, solid uniform magenta #FF00FF filling the entire background, single solid chroma key color with no gradient, no vignette, no noise, no speckles, no shadow under feet, no ground plane. No text, no watermark.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'job-thief'. Pass this file to it as the reference image: .design-work/work/create-jobs/ref-warrior.png. Image brief: Using the attached warrior illustration as the exact style and character reference, draw the SAME young man as a nimble thief. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, the same face, same short messy dark teal-green hair, same amber eyes, same proportions and line weight. Do not make it more polished, digital or saturated than the reference. Outfit: a dark charcoal hooded short cloak with the hood down, a fitted brown leather jerkin over a dark shirt, fingerless leather gloves, a belt with small pouches and a sheathed second knife, slim dark trousers tucked into soft leather boots, a short curved steel dagger held loosely in his right hand. Full body, head to boots fully visible, standing three-quarter view, confident relaxed pose, centered with a small margin around the figure. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 樂手立繪（現用）

- 素材：`/_blob/a99d9f0ad83b64bbaf40bfea4b56d79f`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g234
- 對應方式：人工比對（去背後製）
- 參考圖：`ref-warrior.png`

```text
A classic fantasy JRPG character concept art of the exact same young man from the reference image, depicted as a traveling bard, drawn in the identical traditional watercolor and pen-and-ink art style. The character has the identical facial features, same short messy dark teal-green hair, same warm amber eyes, youthful friendly expression with a gentle confident smile, and the exact same proportions and delicate ink linework. Outfit: a soft burgundy feathered cap tilted on his head with a small tasteful feather; a muted mustard-yellow and burgundy doublet over a cream undershirt; a short cream traveler's cape draped over one shoulder; a sturdy brown leather belt; fitted muted brown trousers; tall brown leather travel boots matching the reference. He is holding a wooden lute comfortably in his left arm against his hip. Strictly no magic glows, sparkles, floating music notes, or light effects. Full body character design, from head to boots fully visible, standing in a confident relaxed three-quarter pose, centered with margins around the figure. Background is completely flat, solid magenta (#FF00FF) filling the entire image with no gradients, no shadows, no floor, no ground, no texture, no vignette. No text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'job-bard'. Pass this file to it as the reference image: .design-work/work/create-jobs/ref-warrior.png. Image brief: Using the attached warrior illustration as the exact style and character reference, draw the SAME young man as a traveling bard. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, the same face, same short messy dark teal-green hair, same amber eyes, same proportions and line weight. Do not make it more polished, digital or saturated than the reference. Outfit: a soft burgundy feathered cap, a muted mustard-and-burgundy doublet with a short cream cape over one shoulder, a leather belt, brown trousers and tall travel boots, holding a wooden lute in his left arm against his hip, no glow, no sparkles, no light effects around it. Full body, head to boots fully visible, standing three-quarter view, confident relaxed pose, centered with a small margin around the figure. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 女戰士立繪（現用）

- 素材：`/_blob/60848c9e87b253f2e8a5eab4110cfdac`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g239
- 對應方式：形狀比對 16.3（人工確認）
- 參考圖：`jobf-warrior-a.png`

```text
In the exact same anime watercolor illustration style, line art, and character design as the reference image, redraw the female warrior character. Keep her identical in every way: the exact same face, gentle expression, warm amber eyes, light skin, silver metal breastplate cuirass, single left shoulder pauldron, dark blue tunic with red border trim, red cloth waist sash with loose tied ends, brown leather gauntlets, brown trousers, brown folded leather boots, and holding the silver longsword upright in front of her with both gloved hands resting on the hilt. 

The ONLY change is her hairstyle: instead of a ponytail, her dark teal-green hair is now worn completely DOWN in long, voluminous, big loose waves flowing past her shoulders down to her mid-back, with soft side-swept bangs framing her face. The hair color is the exact same dark teal / deep forest green tone as in the reference image.

The background must be a completely uniform, flat, solid magenta (#FF00FF) chroma key background with zero shadows, zero gradients, zero vignette, zero floor, and no text. Full-body portrait shot matching the original framing.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jobf-warrior-w'. Pass this file to it as the reference image: .design-work/work/create-jobs/jobf-warrior-a.png. Image brief: Redraw the attached illustration keeping EXACTLY the same character, face, outfit, colors, pose, sword, composition, framing, art style, linework and watercolor texture. Change ONLY her hairstyle: she now has long voluminous hair in big loose waves, worn down, flowing past her shoulders to mid-back, with soft side-swept bangs, in the same dark teal-green color. Everything else must stay identical. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 女術士立繪（現用）

- 素材：`/_blob/855c671b18fc50230ccc1693a35914f7`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g240
- 對應方式：形狀比對 16.2（人工確認）
- 參考圖：`ref-warrior.png`

```text
Full-body character illustration of a young female sorceress in the exact traditional watercolor and fine ink linework art style of the reference image. She is the female counterpart to the reference character, sharing the exact same dark teal-green hair color and warm amber eyes. Her hair is long, voluminous, and worn down in loose wavy locks flowing past her shoulders to mid-back, with soft side-swept bangs framing a gentle, determined feminine face. She has a slender feminine figure with narrower shoulders and soft hips. She wears practical medieval fantasy attire: a long deep-blue hooded robe-dress with the hood lowered around her neck, cinched at the waist by a wide brown leather corset belt with buckles, wide flowing sleeves lined with cream fabric, a short dark shoulder capelet secured with a silver clasp, a brown leather satchel with scroll cases at her hip, and soft brown traveler boots. In one hand she holds a carved wooden staff upright topped with a pale, unlit crystal, and with her other arm she holds a closed leather spellbook against her chest. No magic glowing effects, no sparkles, no lighting flares. Full body shown from head to boots with breathing room around the figure, three-quarter standing view. Art style matches the reference: visible ink pen outlines, delicate transparent watercolor wash shading, visible watercolor paper texture, muted natural colors, not overly saturated or digital. Background is a completely flat solid magenta color #FF00FF with no floor shadow, no gradient, no texture, no ground, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jobf-sorcerer'. Pass this file to it as the reference image: .design-work/work/create-jobs/ref-warrior.png. Image brief: Use the attached warrior illustration ONLY as the reference for the art style and for the hair and eye colors. Do NOT copy his outfit, his silhouette, his body shape or his pose. Draw a NEW character: a young woman sorceress, the female counterpart of that hero, with the same dark teal-green hair (hers is long voluminous hair in big loose waves, worn down, flowing past her shoulders to mid-back, with soft side-swept bangs) and the same amber eyes, a gentle but determined face with clearly feminine features. Body: a clearly feminine figure, distinctly different from the man in the reference: narrower shoulders, slender waist, softer curves at the hips, slimmer arms and legs, a little shorter and lighter-built than him; modest, practical medieval-fantasy adventurer clothing in the same world as the reference, nothing modern (no pleated school-style skirts, no tights or stockings, no modern cuts). Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same line weight and level of detail. Do not make it more polished, digital or saturated than the reference. Outfit: a long deep-blue hooded robe-dress with the hood down, fitted at the waist with a brown leather corset belt, wide sleeves lined in cream, a short shoulder capelet fastened with a silver clasp, a leather satchel of scrolls at her hip, soft brown boots; she holds a wooden staff topped with a pale crystal upright in one hand and a closed spellbook against her chest with the other, no glow, no sparkles, no light effects around it. Full body, head to boots fully visible, standing three-quarter view, a pose different from the reference as described in the outfit, centered with a small margin around the figure. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 女盜賊立繪（現用）

- 素材：`/_blob/db23051f2b4c51978726046d97b3e8f3`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g241
- 對應方式：形狀比對 12.2（人工確認）
- 參考圖：`ref-warrior.png`

```text
A full-body fantasy RPG character concept illustration of a young female thief adventurer, matching the exact traditional art style and character coloring of the reference image.

Art Style & Medium:
- Identical art style to the reference: traditional hand-drawn ink line art with delicate visible pen strokes and subtle cross-hatching, filled with soft transparent watercolor washes showing fine watercolor paper grain texture beneath.
- Soft, natural lighting and gentle watercolor shading, muted earthy medieval fantasy color palette. Not digital vector, not 3D render, not overly saturated.

Character Appearance:
- A young woman, the female counterpart of the male hero in the reference.
- Hair: Matching dark teal-green hair color from the reference, worn long and voluminous in soft loose waves flowing past her shoulders to mid-back, with soft side-swept bangs framing her face.
- Eyes: Same warm golden amber eyes as the reference.
- Face: Gentle yet determined, youthful, distinctly feminine facial features and expression.
- Build: Slender, agile, feminine proportions—narrower shoulders, slim waist, soft hip curves, lighter and nimble build compared to the armored warrior.

Costume & Equipment:
- Modest, practical medieval adventurer rogue clothing (no modern garments, no school skirts, no tights).
- A dark charcoal hooded short cloak with the hood down around her neck/shoulders.
- A fitted brown leather jerkin laced up at the front, worn over a dark long-sleeved linen tunic.
- Knee-length split tunic skirt over slim dark travel trousers.
- Utility leather belt with small pouches.
- Fingerless leather gloves.
- Soft knee-high brown leather boots.
- Holding a short curved steel dagger in a reverse grip in one hand.

Pose & Framing:
- Full-length figure, complete head to boots fully visible within frame with margins around the character.
- Three-quarter view standing in a light, nimble ready stance with weight on her toes.

Background:
- Completely flat, solid, pure magenta (#FF00FF) background filling the entire frame behind the character. No shadows on the ground, no gradients, no borders, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jobf-thief'. Pass this file to it as the reference image: .design-work/work/create-jobs/ref-warrior.png. Image brief: Use the attached warrior illustration ONLY as the reference for the art style and for the hair and eye colors. Do NOT copy his outfit, his silhouette, his body shape or his pose. Draw a NEW character: a young woman thief, the female counterpart of that hero, with the same dark teal-green hair (hers is long voluminous hair in big loose waves, worn down, flowing past her shoulders to mid-back, with soft side-swept bangs) and the same amber eyes, a gentle but determined face with clearly feminine features. Body: a clearly feminine figure, distinctly different from the man in the reference: narrower shoulders, slender waist, softer curves at the hips, slimmer arms and legs, a little shorter and lighter-built than him; modest, practical medieval-fantasy adventurer clothing in the same world as the reference, nothing modern (no pleated school-style skirts, no tights or stockings, no modern cuts). Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same line weight and level of detail. Do not make it more polished, digital or saturated than the reference. Outfit: a dark charcoal hooded short cloak with the hood down, a fitted brown leather jerkin laced at the front over a dark long-sleeved tunic, a knee-length split tunic hem over slim dark trousers, fingerless leather gloves, a belt with small pouches, soft knee-high leather boots; she holds a short curved dagger in a reverse grip in one hand, light on her feet with her weight on her toes, a nimble ready stance. Full body, head to boots fully visible, standing three-quarter view, a pose different from the reference as described in the outfit, centered with a small margin around the figure. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 女樂手立繪（現用）

- 素材：`/_blob/edbe0140e6a2adeb5cc2197f38a9c768`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g242
- 對應方式：形狀比對 42.6（人工確認）
- 參考圖：`ref-warrior.png`

```text
Full body character illustration of a young female fantasy bard, standing in three-quarter view, created in the exact same traditional art style, linework, and color palette as input_file_0.png. 

Character design: She is the female counterpart to the hero in input_file_0.png. She has a gentle, determined feminine face with large expressive amber eyes identical in color to input_file_0.png. Her hair is the exact same dark teal-green color as input_file_0.png, styled long in voluminous loose waves that cascade past her shoulders down to mid-back, with soft side-swept bangs framing her face. Her body shape is distinctly feminine and slender with narrower shoulders, a slim waist, soft hip curves, and graceful limbs, visibly different from the male warrior.

Attire and equipment: Modest, practical medieval-fantasy adventurer garb. She wears a tilted burgundy beret adorned with a long feather. Over a loose cream linen blouse with billowing full sleeves, she wears a fitted mustard-and-burgundy laced bodice/corset. A short cream travel capelet is draped over one shoulder. A long burgundy travel skirt is split at the front to reveal practical brown riding trousers underneath. On her feet are tall brown leather travel boots. She holds an acoustic wooden lute in front of her, one hand on the fretboard and the other lightly resting over the strings as if poised to play; no magic glows, no sparkles, completely grounded acoustic wooden instrument.

Artistic style: Precise match to input_file_0.png—classic hand-drawn ink line art with visible pen strokes, soft translucent watercolor washes with visible paper texture, muted natural tones, gentle light and shadow, consistent line weight.

Composition and background: Full body centered standing pose with the entire character from hat to boots fully in frame, small margin around the figure. The background is a flat, uniform, solid pure magenta #FF00FF chroma background with no gradients, no cast shadows on the floor, no background elements, and no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jobf-bard'. Pass this file to it as the reference image: .design-work/work/create-jobs/ref-warrior.png. Image brief: Use the attached warrior illustration ONLY as the reference for the art style and for the hair and eye colors. Do NOT copy his outfit, his silhouette, his body shape or his pose. Draw a NEW character: a young woman bard, the female counterpart of that hero, with the same dark teal-green hair (hers is long voluminous hair in big loose waves, worn down, flowing past her shoulders to mid-back, with soft side-swept bangs) and the same amber eyes, a gentle but determined face with clearly feminine features. Body: a clearly feminine figure, distinctly different from the man in the reference: narrower shoulders, slender waist, softer curves at the hips, slimmer arms and legs, a little shorter and lighter-built than him; modest, practical medieval-fantasy adventurer clothing in the same world as the reference, nothing modern (no pleated school-style skirts, no tights or stockings, no modern cuts). Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same line weight and level of detail. Do not make it more polished, digital or saturated than the reference. Outfit: a burgundy beret with a long feather, a mustard-and-burgundy laced bodice over a cream linen blouse with full sleeves, a long burgundy travel skirt split at the front over brown riding trousers, a short cream cape over one shoulder, tall brown travel boots; she holds a wooden lute in front of her with one hand on the strings as if about to play, no glow, no sparkles, no light effects around it. Full body, head to boots fully visible, standing three-quarter view, a pose different from the reference as described in the outfit, centered with a small margin around the figure. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 戰士 Q 版站姿（現用）

- 素材：`/_blob/e84a7a7e8d39b610eff1319a9f299c18`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 16:9，紀錄 g041
- 對應方式：形狀比對 26.2（人工確認）

```text
A 2D video game sprite sheet featuring exactly 4 animation frames of the identical character in a single horizontal row, evenly spaced with wide clear gaps between each figure. All 4 figures are rendered at the exact same scale and aligned on the same horizontal baseline, full body shown from head to toe in every frame, viewed from the side profile facing right. Character: A chibi super-deformed young swordsman, 3.5 heads tall, big head, short messy dark-teal hair, amber eyes, polished light steel breastplate and pauldrons over a navy blue tunic, long vibrant red scarf, brown leather belts, and brown leather boots, holding a straight steel longsword. Japanese RPG battle sprite art style with crisp ink outlines and soft watercolor cel-shading. Sequence from left to right: Frame 1: battle-ready idle stance holding the sword forward. Frame 2: wind-up pose with sword raised high behind the head. Frame 3: dashing forward mid-slash with sword swung down in front and scarf flying. Frame 4: follow-through recovery with sword low after the swing. Pure solid flat magenta background (#FF00FF), completely blank with no shadows, no ground line, no lighting effects, no motion lines, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-chibi-sheet'. Image brief: A sprite sheet of 4 animation frames of the SAME character in one horizontal row, evenly spaced with wide clear gaps, all frames the same scale and standing on the same baseline: a chibi super-deformed young swordsman, 3.5 heads tall, big head, short messy dark-teal hair, amber eyes, light steel pauldrons and breastplate over a navy tunic, long red scarf, brown leather belts and boots, holding a straight longsword, hand-painted Japanese RPG battle sprite style with clean ink outlines and soft watercolor cel shading, facing right in side view. Frame 1: battle-ready idle stance holding the sword forward. Frame 2: wind-up, sword raised high behind the head. Frame 3: dashing forward mid-slash, sword swung down in front, scarf flying. Frame 4: follow-through, sword low after the swing. Full body in every frame, on a solid magenta background #FF00FF, no text, no shadow on the background, no effects. Reply with only the saved file path.
```

</details>

## 術士 Q 版站姿（現用）

- 素材：`/_blob/cd7261f18a128737feee111eebc2064d`
- 產圖：grok，2026-10-26，比例 1:1，紀錄 g067
- 對應方式：形狀比對 21.8（人工確認）

```text
Detailed painted chibi RPG sprite, 2.5 heads tall, soft painterly shading with highlights and gradients (not flat vector), thin dark brown outlines, like premium mobile JRPG battle sprites. Single character: young male hero with short messy spiky hair colored dark teal (deep blue-green, clearly green-tinted, not black), amber-gold eyes, confident slight smile, as a sorcerer in a layered navy hooded robe with brass clasps, brown leather belt and pouch, brown boots, holding a wooden staff with a glowing pale blue crystal, battle-ready stance facing right, full body centered. Isolated on a perfectly flat uniform pure magenta background #FF00FF, no gradient, no shadow, no ground. No text.
```

## 草原史萊姆（現用）

- 素材：`/_blob/fffbcf46caa43a05edd00ca75fc91320`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 1:1，紀錄 g040
- 對應方式：形狀比對 6.6（人工確認）

```text
A single cute chibi green slime monster battle sprite for a classic Japanese 2D fantasy RPG. The slime has a round bouncy jelly blob body, glossy translucent bright green gelatin texture with soft specular highlights and a slightly lighter translucent pale lime-green belly. Facial features: two small cute black bead dot eyes and a comical tiny determined angry frown. A tiny fresh green plant sprout with two little leaves growing out of the top of its head. Hand-painted Japanese RPG sprite art style, clean defined ink lineart outlines, soft watercolor cel shading, smooth anime game concept art. Profile side view facing towards the left, full body centered. Isolated completely on an entirely flat solid pure magenta background (#FF00FF), perfectly plain solid magenta background with no cast shadow, no floor shadow, no gradient, no borders, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-slime'. Image brief: A single cute chibi green slime monster for a Japanese fantasy RPG battle, round jelly blob body, glossy translucent green with a lighter belly, small black dot eyes and a tiny angry frown, a little leaf sprout on top, hand-painted style with clean ink outline and soft watercolor cel shading, side view facing left, full body, on a solid magenta background #FF00FF, no text, no shadow on the background. Reply with only the saved file path.
```

</details>

## 莉可（Lv1 雜貨鋪）（現用）

- 素材：`/_blob/f9c08968a058c9a694037c0c149ea63e`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a cheerful teenage girl apprentice merchant with short wavy light-brown hair and freckles. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a mustard apron over a cream blouse, rolled sleeves, a huge overstuffed backpack with pots and rolled maps, holding a small potion bottle up proudly.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 巴爾克（Lv2 鍛冶屋）（現用）

- 素材：`/_blob/4a347add7986b96832d1a4189e229a02`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a stout middle-aged dwarf blacksmith with a braided red beard and thick arms. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a scorched leather smith apron, rolled sleeves, heavy gloves, a forging hammer resting on his shoulder.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 希爾薇（Lv3 細工坊）（現用）

- 素材：`/_blob/2c487dcab58eee68cc24b68cc69df015`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供；成圖的單片眼鏡畫成了圓框眼鏡
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a calm elegant elf woman silversmith with long silver hair and pointed ears. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a pale blue long coat with silver embroidery, a jeweler's monocle, holding a small silver dagger on a velvet cloth; no glow, no sparkles.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 葛雷（Lv4 補給站）（現用）

- 素材：`/_blob/81647ec017cd6aa7109b66eeed4f3cfb`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a friendly retired adventurer in his fifties with grey stubble and a scar over one eyebrow. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a worn travel coat with many pouches, a coil of rope over his shoulder, an old map in one hand, a lantern hanging at his belt.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 梅露（Lv5 魔導具工房）（現用）

- 素材：`/_blob/95d002a460cf2c0c7fb7f24cba1dda69`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a cute, small and cheerful young witch girl around 14 years old, with a round soft face, big sparkling violet eyes, rosy cheeks, a playful wink, and fluffy wavy lavender hair in two loose low pigtails. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: an oversized floppy lavender witch hat that is a bit too big for her, with a drooping tip and a small star charm; a short puffy-sleeved lavender dress with a cream collar and a little ribbon, a small belt pouch with two tiny potion vials, striped stockings and round brown boots; a small black cat familiar sitting on her shoulder; one hand making a cheerful V sign, the other hugging a small spell book; no glow, no sparkles, no light effects.

Full body, head to feet fully visible, standing three-quarter view facing right, a playful welcoming pose standing slightly on tiptoe, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 艾蓮娜（Lv6 星鐵商會）（現用）

- 素材：`/_blob/6ff5cc01fdbafba8d87424e0c2759a89`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供；成圖的扇子是打開的
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a confident young noblewoman merchant with golden curled hair in an updo. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: an elegant navy and gold dress with a short cape, white gloves, holding a closed folding fan and a small ledger.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 薩格（Lv7 龍鱗秘庫）（現用）

- 素材：`/_blob/96b39a560b674b2011c7169acf4773b6`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a tall mysterious dragonkin vault keeper with dark scales, small horns and calm golden eyes. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a long dark robe with ancient gold trims, a ring of old keys at the waist, one clawed hand resting on a tall staff; no glow, no fire, no light effects.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming shopkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 瑪莎（暖爐亭）（現用）

- 素材：`/_blob/047c012e8ad6861745570e978cc23e93`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：戰士立繪

```text
Using the attached warrior illustration as the exact ART STYLE reference only (not the character), draw a NEW character: a warm, motherly innkeeper woman in her forties with a plump build, rosy cheeks, and auburn hair tied up in a loose bun with a few strands falling down. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions style and line weight. Do not make it more polished, digital or saturated than the reference.

Outfit and props: a dark green bodice dress with rolled sleeves, a long cream apron with a pocket, a ring of brass room keys hanging at her waist, holding a steaming mug of tea in one hand, the other hand on her hip.

Full body, head to feet fully visible, standing three-quarter view facing right, a welcoming innkeeper pose, centered with a small margin around the figure.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no speckles, no checkerboard, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

## 洛岡（爐火鐵舖）（現用）

- 素材：`/_blob/563c47f6138ee8d06ee479b6f37063c5`
- 產圖：Gemini 網頁版（使用者產），比例 2:3
- 對應方式：使用者 2026-11-08 提供
- 參考圖：巴爾克立繪

```text
Using the attached character illustration as the exact style reference, draw a new character: a middle-aged human blacksmith, broad-shouldered, short cropped grey-streaked hair and a neat short beard, soot on his cheeks, sleeves rolled up, thick leather apron over a linen shirt, heavy gloves, holding a pair of long forge tongs at his side. Match the reference's art style precisely: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, muted natural palette, same line weight and proportions. Waist-up to knees visible, three-quarter view, friendly gruff expression.

Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no shadow. No text. Portrait aspect ratio 2:3.
```

## 莉瑟（公會接待員）（現用）

- 素材：`/_blob/a32a504a16c1c3a0a3c5b77fce32e303`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 2:3，紀錄 g201
- 對應方式：形狀比對 16.9（人工確認）
- 參考圖：`ref-smith.png`、`ref-inn.png`

```text
Using the attached blacksmith and innkeeper illustrations as the exact style reference, draw a NEW character in precisely the same art style: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions, same line weight and same level of detail. Do not make it more polished, digital or saturated than the references. Character: a young woman around twenty who works as the receptionist clerk of the royal honour hall, short wavy honey-blonde bob hair, gentle hazel eyes, a shy friendly smile, wearing a neat navy blue uniform waistcoat with gold piping over a white long-sleeved blouse, a crimson neck ribbon, a long navy skirt, a pair of small round reading glasses, hugging a thin leather ledger to her chest with both arms. Standing three-quarter view facing slightly to the right, from head to knees, centered with a small margin. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no shadow, no ground. No text. Portrait aspect ratio 2:3.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'clerk-b'. Pass these two files to it as reference images: .design-work/work/honour/ref-smith.png and .design-work/work/honour/ref-inn.png. Image brief: Using the attached blacksmith and innkeeper illustrations as the exact style reference, draw a NEW character in precisely the same art style: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions, same line weight and same level of detail. Do not make it more polished, digital or saturated than the references. Character: a young woman around twenty who works as the receptionist clerk of the royal honour hall, short wavy honey-blonde bob hair, gentle hazel eyes, a shy friendly smile, wearing a neat navy blue uniform waistcoat with gold piping over a white long-sleeved blouse, a crimson neck ribbon, a long navy skirt, a pair of small round reading glasses, hugging a thin leather ledger to her chest with both arms. Standing three-quarter view facing slightly to the right, from head to knees, centered with a small margin. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 米菈（獸欄管理員）（現用）

- 素材：`/_blob/b70e140ff9b998d7111d902e06e0ed1d`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g219
- 對應方式：形狀比對 13.3（人工確認）
- 參考圖：`ref-smith.png`、`ref-inn.png`

```text
A full character illustration in the exact same art style and medium as ref-smith.png and ref-inn.png: delicate hand-drawn ink pen linework with visible hatching, soft transparent watercolor washes, paper texture, subtle naturalistic lighting and gentle shading, muted medieval fantasy color palette. A friendly young woman who manages the village pet stable and animal companions. She has long blonde hair in a single thick braid draped over her shoulder, wearing a green kerchief tied over her head. She wears a cream linen peasant blouse with rolled-up sleeves, and a durable brown canvas work apron with pockets, with a few bits of hay and straw caught on her apron. She has sturdy brown leather boots and holds a small rustic wooden bucket filled with colorful fruit and berries in one arm. She has a warm, cheerful, welcoming smile and friendly expression. Character is standing in a three-quarter pose facing slightly to the right, framed from head to knees/boots, centered nicely. The entire background is completely solid, flat, seamless pure magenta (#FF00FF) chroma key with no gradient, no vignette, no cast shadow, no floor line, no scenery, no text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'keeper-a'. Pass these two files to it as reference images: .design-work/work/companions/ref-smith.png and .design-work/work/companions/ref-inn.png. Image brief: Using the attached blacksmith and innkeeper illustrations as the exact style reference, draw a NEW character in precisely the same art style: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions, same line weight and same level of detail. Do not make it more polished, digital or saturated than the references. Character: A friendly young woman who runs the town's pet stable. Long blonde hair in a single thick braid over her shoulder, a green kerchief tied over her head, cream linen blouse with rolled-up sleeves, a brown canvas work apron with pockets, sturdy boots, holding a small wooden bucket of fruit in one arm, warm cheerful smile, a little straw on her apron. Standing three-quarter view facing slightly to the right, from head to knees, centered with a small margin. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 巴頓（傭兵所長）（現用）

- 素材：`/_blob/cd486224618837274e4bb9fc8784fa58`
- 產圖：Gemini（agy image-generator），2026-11-07，比例 2:3，紀錄 g220
- 對應方式：形狀比對 15.1（人工確認）
- 參考圖：`ref-smith.png`、`ref-inn.png`

```text
In the exact art style of the reference images (hand-drawn ink linework with visible pen hatching and strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural earthy palette, fantasy RPG character illustration, same line weight, proportions, and level of detail, not overly digital or saturated): A veteran mercenary guild master in his fifties. Black leather eyepatch over his left eye, short cropped gray hair, trimmed gray beard, a visible scar across his cheek, broad and sturdy build. He wears a worn brown leather vest over a cream rolled-sleeve linen shirt, and a thick leather belt with a sheathed short sword at his hip. Arms crossed over his chest, showing a confident half-smile. Standing three-quarter view facing slightly to the right, framed from head to knees, centered with margin. Background is a completely flat, solid uniform magenta #FF00FF color with no gradient, no vignette, no noise, no shadows, no ground. No text.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'captain-a'. Pass these two files to it as reference images: .design-work/work/companions/ref-smith.png and .design-work/work/companions/ref-inn.png. Image brief: Using the attached blacksmith and innkeeper illustrations as the exact style reference, draw a NEW character in precisely the same art style: hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture showing through, gentle light-and-shadow, muted natural palette, same proportions, same line weight and same level of detail. Do not make it more polished, digital or saturated than the references. Character: A veteran mercenary guild master in his fifties. Black leather eyepatch over his left eye, short cropped gray hair and a trimmed gray beard, a scar across his cheek, broad and sturdy build, worn brown leather vest over a rolled-sleeve linen shirt, thick belt with a sheathed short sword, arms crossed, a confident half-smile. Standing three-quarter view facing slightly to the right, from head to knees, centered with a small margin. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no noise, no shadow, no ground. No text. Portrait aspect ratio 2:3. Reply with only the saved file path.
```

</details>

## 微風狐（寵物）（現用）

- 素材：`/_blob/aeb4ca2a8630744a5f6d95e046a2dd96`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 1:1，紀錄 g213
- 對應方式：work/companions/up/pet-fox.png → raw/pet-fox.png
- 參考圖：`pet_fox_1793972527240.jpg`、`ref-item.png`

```text
Remove the white paper rectangle from the first image completely. Keep the fox cub subject, linework, and watercolor texture exactly as shown, but place the fox cub directly on the pure flat solid magenta #FF00FF background, exactly like the isolated object style in the second image. The background must be 100% solid magenta (#FF00FF) from edge to edge, filling the entire frame right up to the fox's outer ink outlines, with no white sheet, no white card, no border, no frame, no ground, and no shadow. Fox centered, filling 80% of the square frame.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'pet-fox'. Pass this file to it as a style reference image: .design-work/work/companions/ref-item.png. Image brief: Using the attached image only as the art style reference (same ink line weight, same watercolor texture, same level of detail; do not copy its subject), draw: A small cute fantasy breeze fox cub, pale mint green and cream fur, a large fluffy tail curling like a gust of wind, big friendly eyes, sitting, three-quarter front view, full body hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, flat front view, centered, the object fills about 80 percent of the frame. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no shadow, no ground. No text, no letters. Aspect ratio 1:1. Reply with only the saved file path.
```

</details>

## 叢林黑豹（寵物）（現用）

- 素材：`/_blob/abee5352d148ba01de8d6f2f8b071c55`
- 產圖：Gemini（agy image-generator），2026-11-06，比例 1:1，紀錄 g214
- 對應方式：work/companions/up/pet-panther.png → raw/pet-panther.png
- 參考圖：`ref-item.png`

```text
In the exact same art style and rendering technique as the reference image (crisp hand-drawn black ink outlines with fine pen cross-hatching, layered transparent watercolor washes with paper texture and natural pigment pooling): A small cute black jungle panther cub sitting politely, full body three-quarter front view, with faint subtle dark rosettes across its sleek midnight-black coat, big bright amber-golden eyes, small rounded ears, and a long tail curled neatly by its paws. Centered composition, the cub fills about 80 percent of the frame. The background is a perfectly flat, solid vivid magenta (#FF00FF) color filling the entire frame, with zero gradients, zero shadows, no floor or ground, and clean crisp edges isolating the subject. No text, no letters.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'pet-panther'. Pass this file to it as a style reference image: .design-work/work/companions/ref-item.png. Image brief: Using the attached image only as the art style reference (same ink line weight, same watercolor texture, same level of detail; do not copy its subject), draw: A small cute black jungle panther cub with faint dark rosettes and bright amber eyes, sitting, three-quarter front view, full body hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, flat front view, centered, the object fills about 80 percent of the frame. Background: a perfectly flat, solid magenta #FF00FF filling the whole image, one single color with no gradient, no vignette, no shadow, no ground. No text, no letters. Aspect ratio 1:1. Reply with only the saved file path.
```

</details>

## 通用寵物剪影（現用）

- 素材：`/_blob/297e4e524678073162a154ac2dd54aaa`
- 產圖：grok，2026-11-14，比例 1:1，紀錄 g278
- 對應方式：人工：raw md5 對 grok 原圖轉 png（2026-11-14）

```text
A mysterious unknown little beast drawn as a soft dark silhouette: a cute chubby four-legged creature sitting upright and facing the viewer, round head with two perky pointed ears, a fluffy tail curled beside it, the whole body filled with one flat deep warm umber-gray ink wash with subtle watercolor texture and a slightly lighter rim along the top edge, only two small round glowing amber eyes visible, no mouth, no nose, no fur details, no markings, simple readable shape hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette, flat front view, centered, the object fills about 80 percent of the frame, on a solid flat magenta background #FF00FF, no text, no letters, no drop shadow
```
