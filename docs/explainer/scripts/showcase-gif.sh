#!/usr/bin/env bash
# README GIF about Console Mode (src/Showcase.tsx), at the best quality a GIF allows:
# 1280 px, 25 fps (the composition's own rate, so no frame is dropped), one 256-colour palette
# per scene (a single palette for the whole film washes out the colours), then a gifsicle pass
# with --lossy=15: about 40% smaller, no visible difference at 1:1 (~9 MB instead of ~16 MB).
# Usage: scripts/showcase-gif.sh [en|pt]
set -euo pipefail
cd "$(dirname "$0")/.."
LANG_ARG="${1:-en}"
NAME=showcase; [ "$LANG_ARG" = pt ] && NAME=showcase.pt-BR
OUT="../../assets/console-mode-$( [ "$LANG_ARG" = pt ] && echo tour.pt-BR || echo tour ).gif"
TMP="out/$NAME-parts"
rm -rf "$TMP" && mkdir -p "$TMP"
npx remotion render src/index.ts Showcase "out/$NAME.mp4" --props="{\"lang\":\"$LANG_ARG\"}" --crf=8 ${REMOTION_FLAGS:-}
# Scene lengths in frames at 25 fps: must match DURS in src/Showcase.tsx.
DURS=(75 125 130 125 100 90 115 125 110 100 100 100 90)
FILTER='scale=1280:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=256:stats_mode=full[p];[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle'
start=0
parts=()
for i in "${!DURS[@]}"; do
  d=${DURS[$i]}
  part="$TMP/$(printf %02d "$i").gif"
  npx remotion ffmpeg -nostdin -loglevel error -y -i "out/$NAME.mp4" -vf "trim=start_frame=$start:end_frame=$((start + d)),$FILTER" "$part"
  parts+=("$part")
  start=$((start + d))
done
npx --yes gifsicle@7 --no-warnings --loopcount=forever "${parts[@]}" -o "$TMP/merged.gif"
npx --yes gifsicle@7 --no-warnings -O3 --lossy=15 -o "$OUT" "$TMP/merged.gif"
ls -la "$OUT"
