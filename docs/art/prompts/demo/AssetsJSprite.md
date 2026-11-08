# 素材庫・精靈圖：產圖提示詞

畫布素材庫這塊板子裡每張素材的產圖紀錄。提示詞是當時實際送給產圖模型的原文；
Gemini 會把需求改寫成自己的提示詞，所以另外附上我們送出的需求。說明見 [README](README.md)。

## 戰士戰鬥精靈圖（15 格）（現用）

- 素材：`/_blob/9b1b0ed649f4dbc2af00b69db6b971d9`
- 來源：本機的 grok／Gemini 紀錄裡找不到（多半是在 Gemini 網頁版產的，或是由其他素材後製）

## 斬擊精靈圖（4 格）（現用）

- 素材：`/_blob/bc56b6252c073355f20faef320b1d4b7`
- 產圖：Gemini（agy image-generator），2026-10-25，比例 16:9，紀錄 g051
- 對應方式：人工比對

```text
2D anime game visual effect sprite sheet featuring 4 animation frames arranged horizontally in a single row from left to right, evenly spaced with wide clear gaps between frames. The animation depicts a swift sword slash effect made of glowing bright white core and pale gold energy aura with luminous sparkles. Frame 1 (far left): A sharp, thin curved sliver of white and golden light initiating the slash. Frame 2 (second from left): A fully expanded, wide, dynamic crescent arc of dazzling brilliant white and pale gold slash energy with bright sparkle particles trailing. Frame 3 (third from left): The crescent slash arc breaking apart, fracturing and fading into segmented light trails and glowing embers. Frame 4 (far right): Only a few faint dissipating golden sparkles and residual glowing specks fading away into nothing. Style: Clean Japanese 2D action anime visual effect, VFX animation sprite sheet. Background: Completely flat, solid pure magenta background (#FF00FF, RGB 255, 0, 255) with no gradients, no shadows, no noise, perfectly uniform chroma key magenta. Strictly no characters, no sword hilt or blade, no human figures, no UI, no text, no borders.
```

<details><summary>送出的需求</summary>

```text
Use the image-generator subagent to generate ONE image and name the file exactly 'jp-slash'. Image brief: A sprite sheet of 4 frames of a sword slash visual effect in one horizontal row, evenly spaced with wide clear gaps: a crescent arc of bright white and pale gold light with a few sparkles, growing from a thin sliver in frame 1, to a full wide crescent in frame 2, to a fading broken arc in frame 3, to a few dissipating sparkles in frame 4. Anime game effect style, on a solid magenta background #FF00FF, no text. Reply with only the saved file path.
```

</details>
