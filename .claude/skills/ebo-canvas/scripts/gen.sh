#!/bin/zsh
# 用 grok 產一張 EBO 風格的素材，接著去背、做深淺底檢查圖。
# 用法：gen.sh 名稱 比例 風格 "主體描述" [keyclean 參數…]
#   比例：1:1、16:9、9:16、3:2、2:3、auto
#   風格：item（道具／介面物件，洋紅底要去背）、scene（場景背景，不去背）、stage（戰鬥舞台背景，繪本水彩，不去背；主體只寫場景）
#   keyclean 參數：--all（主體包住洋紅，例如印章框）、--pieces N 名稱…（一排物件切開）
# 產出：.design-work/raw/名稱.png（原圖）、raw/名稱.prompt.txt（實際送出的提示詞）、out/名稱.png（去背）、out/名稱.check.png
set -e
HERE=${0:A:h}
WORK=${EBO_DESIGN_WORK:-${HERE:h:h:h:h}/.design-work}
KEY=~/.claude/skills/preparing-ai-generated-ui-assets/scripts/keyclean.py
name=$1 ratio=$2 style=$3 subject=$4; shift 4
mkdir -p $WORK/raw $WORK/out

# EBO 的固定畫風（跟城鎮插畫、帳簿同一套：墨線＋水彩、暖色）
INK="hand-drawn ink linework with visible pen strokes, soft transparent watercolor washes with paper texture, warm muted palette"
case $style in
  item)  prompt="$subject $INK, flat front view, centered, the object fills about 80 percent of the frame, on a solid flat magenta background #FF00FF, no text, no letters, no drop shadow" ;;
  scene) prompt="$subject $INK, no people, no text" ;;
  # 戰鬥背景（2026-11-08 定案的 H 版）：比 INK 明亮、可愛；限定細節，不然 grok 會畫得太寫實
  stage) prompt="Battle stage background for a side-view 2D RPG: $subject, horizon at about 45 percent height, eye-level view. Keep it simple: the ground is painted as broad soft washes with only a few short strokes near the bottom edge; only a few large simple background shapes, each with just a few soft dabs of texture, no detailed rendering; the far background as two soft color layers with very little shading; a wide open calm ground across the lower half where characters will stand, cute Japanese picture book (ehon) watercolor illustration, soft watercolor washes with gentle color bleeding and visible paper grain, light soft pencil outlines, rounded friendly shapes, bright airy light, fresh soft pastel colors that are not oversaturated, no people, no animals, no text" ;;
  *) echo "風格要是 item、scene 或 stage" >&2; exit 1 ;;
esac

sid=$(grok -p "Call image_gen exactly once with aspect_ratio $ratio and this prompt verbatim: \"$prompt\" Then reply with the saved path only." \
  --tools image_gen --allow image_gen --max-turns 4 --output-format json 2>/dev/null | python3 -I -c 'import json,sys;print(json.load(sys.stdin)["sessionId"])')
img=$(ls ~/.grok/sessions/*/$sid/images/*.(jpg|png) 2>/dev/null | head -1)
[[ -n $img ]] || { echo "grok 沒有產出圖片：$name" >&2; exit 1; }
sips -s format png "$img" --out $WORK/raw/$name.png >/dev/null
print -r -- "$prompt" > $WORK/raw/$name.prompt.txt
echo "raw  $WORK/raw/$name.png"

if [[ $style == item ]]; then
  if [[ " $* " == *" --pieces "* ]]; then
    python3 -I $KEY $WORK/raw/$name.png $WORK/out "$@"
  else
    python3 -I $KEY $WORK/raw/$name.png $WORK/out/$name.png "$@"
    python3 -I $HERE/canvas.py check $WORK/out/$name.check.png $WORK/out/$name.png
  fi
fi
