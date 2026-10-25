#!/bin/zsh
# 用 Antigravity CLI（agy）的 image-generator 批次產道具圖示。
# 用法：scripts/art-gen-items.sh [id ...]   不給 id 就跑 docs/art/prompts/items/ 底下全部（略過 _style.txt）
# 每個 id 的提示詞 = _style.txt（共用風格段）+ 一句主體描述（<id>.txt）。
# 原始圖（桃紅底 JPEG→PNG）落在 .art-out/items/<id>.png；去背、裁切、縮圖另外做（見 docs/art/prompts.md）。
set -e
ROOT=${0:a:h:h}
PROMPTS=$ROOT/docs/art/prompts/items
OUT=$ROOT/.art-out/items; mkdir -p $OUT
STYLE=$(<$PROMPTS/_style.txt)
BRAIN=~/.gemini/antigravity-cli/brain
ids=("$@")
if (( ${#ids} == 0 )); then
  for f in $PROMPTS/*.txt; do n=${f:t:r}; [[ $n == _* ]] || ids+=($n); done
fi
for id in $ids; do
  [[ -f $PROMPTS/$id.txt ]] || { echo "no prompt for $id" >&2; continue; }
  [[ -f $OUT/$id.png ]] && { echo "skip $id (exists)"; continue; }
  name="item-$id"; key=${name//-/_}
  before=$(mktemp); find $BRAIN -name "${key}_*" 2>/dev/null | sort > $before
  agy --model gemini-3.8-flash-low --print-timeout 300s \
    -p="Use the image-generator subagent to generate ONE image and name the file exactly '${name}'. Image brief: ${STYLE} Subject: $(<$PROMPTS/$id.txt). Reply with only the saved file path." >/dev/null 2>&1 || true
  after=$(mktemp); find $BRAIN -name "${key}_*" 2>/dev/null | sort > $after
  new=$(comm -13 $before $after | grep -v tempmedia | tail -1)
  if [[ -z $new ]]; then echo "FAILED $id" >&2; continue; fi
  sips -s format png "$new" --out $OUT/$id.png >/dev/null && echo "$id -> $OUT/$id.png"
done
