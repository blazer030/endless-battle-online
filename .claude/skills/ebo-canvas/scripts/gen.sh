#!/bin/zsh
# 用 grok 產一張 EBO 風格的素材，接著去背、做深淺底檢查圖。
# 用法：gen.sh 名稱 比例 風格 "主體描述" [keyclean 參數…]
#   比例：1:1、16:9、9:16、3:2、2:3、auto
#   風格：item（道具／介面物件，洋紅底要去背）或 scene（場景背景，不去背）
#   keyclean 參數：--all（主體包住洋紅，例如印章框）、--pieces N 名稱…（一排物件切開）
# 產出：.design-work/raw/名稱.png（原圖）、.design-work/out/名稱.png（去背）、.design-work/out/名稱.check.png
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
  *) echo "風格要是 item 或 scene" >&2; exit 1 ;;
esac

sid=$(grok -p "Call image_gen exactly once with aspect_ratio $ratio and this prompt verbatim: \"$prompt\" Then reply with the saved path only." \
  --tools image_gen --allow image_gen --max-turns 4 --output-format json 2>/dev/null | python3 -I -c 'import json,sys;print(json.load(sys.stdin)["sessionId"])')
img=$(ls ~/.grok/sessions/*/$sid/images/*.(jpg|png) 2>/dev/null | head -1)
[[ -n $img ]] || { echo "grok 沒有產出圖片：$name" >&2; exit 1; }
sips -s format png "$img" --out $WORK/raw/$name.png >/dev/null
echo "raw  $WORK/raw/$name.png"

if [[ $style == item ]]; then
  if [[ " $* " == *" --pieces "* ]]; then
    python3 -I $KEY $WORK/raw/$name.png $WORK/out "$@"
  else
    python3 -I $KEY $WORK/raw/$name.png $WORK/out/$name.png "$@"
    python3 -I $HERE/canvas.py check $WORK/out/$name.check.png $WORK/out/$name.png
  fi
fi
