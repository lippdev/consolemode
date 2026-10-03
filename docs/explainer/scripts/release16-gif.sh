#!/usr/bin/env bash
# Short GIF of the 1.6 video (~26 s, 960 px, ~8.5 MB): the best moment of each scene, at normal
# speed. Each piece gets its own palette; the pieces with game footage run at 10 fps (footage is
# what makes a GIF heavy), the interface ones at 15 fps. Needs out/release16-x.mp4 (npm run
# release16) and gifsicle (npx downloads it).
set -euo pipefail
cd "$(dirname "$0")/.."
SRC=out/release16-x.mp4
TMP=out/gif-parts
mkdir -p "$TMP"
FILTER='scale=960:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=256:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle'
# start end fps (seconds in the video)
SEGMENTS=(
  "1.2 4.4 10"   # one click: the desk goes dark, the TV turns on, fly-in
  "5.9 6.9 10"   # Console Mode 1.6 over the game
  "11.9 13.9 10" # session menu: open windows, X closes one
  "15.9 19.0 10" # ControlFS: zoom, press, the file explorer opens
  "22.9 24.5 15" # Console interface: Home
  "26.0 27.4 15" # Session tab
  "29.1 30.4 15" # System tab, background switch
  "35.6 38.7 15" # controller shortcuts being captured
  "40.2 42.0 15" # the fixes
  "45.2 48.4 15" # JoyChromium
  "48.6 51.4 15" # outro
)
parts=()
i=0
for seg in "${SEGMENTS[@]}"; do
  read -r a b fps <<<"$seg"
  i=$((i + 1))
  out="$TMP/$(printf %02d $i).gif"
  npx remotion ffmpeg -nostdin -loglevel error -y -ss "$a" -to "$b" -i "$SRC" -an -r "$fps" -vf "$FILTER" "$out"
  parts+=("$out")
done
npx --yes gifsicle@7 --no-warnings --loopcount=forever "${parts[@]}" -o "$TMP/merged.gif"
npx --yes gifsicle@7 --no-warnings -O3 --lossy=60 -o out/release16.gif "$TMP/merged.gif"
ls -la out/release16.gif
