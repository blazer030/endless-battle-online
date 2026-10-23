#!/usr/bin/env bash
# 把生成的圖整理成規格內的 webp 放進 frontend/src/assets/art/
# 用法：scripts/import-art.sh <來源圖> <id>    id 是 player 或 backend/content/monsters.json 的怪物 id
set -euo pipefail

if [ "$#" -ne 2 ]; then
  echo "用法：$0 <來源圖> <id>" >&2
  exit 1
fi

source_image="$1"
id="$2"
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
monsters_file="$repository_root/backend/content/monsters.json"
output="$repository_root/frontend/src/assets/art/$id.webp"
max_bytes=153600

# 規格值要與 frontend/src/test/artRules.ts 一致
if [ "$id" = "player" ]; then
  width=600
  height=1020
elif [[ "$id" =~ ^[a-z0-9-]+$ ]] && grep -q "\"id\": \"$id\"" "$monsters_file"; then
  width=720
  height=1000
else
  echo "未知的 id：${id}（只能是 player 或 backend/content/monsters.json 內的怪物 id）" >&2
  exit 1
fi

if [ ! -f "$source_image" ]; then
  echo "找不到來源圖：$source_image" >&2
  exit 1
fi

if ! command -v magick >/dev/null 2>&1; then
  echo "找不到 ImageMagick 7 的 magick。請先安裝：brew install imagemagick（macOS）或 apt install imagemagick（Linux，需 7.x）" >&2
  exit 1
fi

convert_with_quality() {
  magick "$source_image" -trim +repage -resize "${width}x${height}" \
    -background none -gravity south -extent "${width}x${height}" \
    -quality "$1" "$output"
}

file_size() { wc -c <"$output" | tr -d ' '; }

convert_with_quality 82
if [ "$(file_size)" -gt "$max_bytes" ]; then
  convert_with_quality 70
fi
if [ "$(file_size)" -gt "$max_bytes" ]; then
  rm -f "$output"
  echo "超過 ${max_bytes} bytes，即使 quality 70 也放不下；請簡化來源圖（減少細節與漸層）" >&2
  exit 1
fi

echo "已輸出 $output（${width}x${height}，$(file_size) bytes）"
