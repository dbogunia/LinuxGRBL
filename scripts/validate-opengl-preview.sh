#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
rid="${1:-linux-x64}"
app="${2:-$root/artifacts/publish/$rid/LaserGRBL.Avalonia}"
out_dir="${3:-$root/artifacts/opengl-validation}"

if [[ ! -x "$app" ]]; then
  echo "App executable not found: $app" >&2
  echo "Run: dotnet publish LaserGRBL.Avalonia/LaserGRBL.Avalonia.csproj -c Release -r $rid --self-contained true -o artifacts/publish/$rid" >&2
  exit 2
fi

for tool in import identify wmctrl python3; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Required tool missing: $tool" >&2
    exit 2
  fi
done

if [[ -z "${DISPLAY:-}" ]]; then
  echo "DISPLAY is not set; real OpenGL validation requires a graphical session." >&2
  exit 2
fi

rm -rf "$out_dir"
mkdir -p "$out_dir"

sample="$out_dir/opengl-validation-sample.gcode"
cat > "$sample" <<'GCODE'
G21
G90
G0 X0 Y0 Z0
G1 X40 Y0 Z0 F1200
G1 X40 Y30 Z0
G1 X0 Y30 Z0
G1 X0 Y0 Z0
G0 X5 Y5 Z3
G1 X35 Y25 Z3
G1 X20 Y10 Z8
G0 X0 Y0 Z0
GCODE

runtime="$out_dir/runtime"
mkdir -p "$runtime/config" "$runtime/data" "$runtime/cache"

XDG_CONFIG_HOME="$runtime/config" \
XDG_DATA_HOME="$runtime/data" \
XDG_CACHE_HOME="$runtime/cache" \
LASERGRBL_OPENGL_DIAGNOSTICS_PATH="$out_dir/opengl-diagnostics.log" \
  "$app" "$sample" > "$out_dir/app.stdout.log" 2> "$out_dir/app.stderr.log" &
app_pid=$!

cleanup() {
  if kill -0 "$app_pid" >/dev/null 2>&1; then
    kill "$app_pid" >/dev/null 2>&1 || true
    wait "$app_pid" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

window_id=""
for _ in $(seq 1 80); do
  window_id="$(wmctrl -lG | awk '$0 ~ / LaserGRBL$/ { print $1; exit }')"
  [[ -n "$window_id" ]] && break
  sleep 0.25
done

if [[ -z "$window_id" ]]; then
  echo "LaserGRBL window did not appear." >&2
  exit 1
fi

wmctrl -i -r "$window_id" -b add,above || true
wmctrl -i -r "$window_id" -e 0,80,80,1180,760 || true
sleep 4

wmctrl -lG > "$out_dir/windows.txt"
geometry="$(awk -v id="$window_id" '$1 == id { print $3, $4, $5, $6; exit }' "$out_dir/windows.txt")"
if [[ -z "$geometry" ]]; then
  echo "Unable to read LaserGRBL window geometry." >&2
  exit 1
fi

read -r win_x win_y win_w win_h <<< "$geometry"
root_png="$out_dir/lasergrbl-window-root.png"
window_png="$out_dir/lasergrbl-window.png"
preview_png="$out_dir/lasergrbl-3d-preview.png"

import -window root "$root_png"
import -window "$window_id" "$window_png"
python3 "$root/scripts/validate-opengl-preview.py" "$window_png" "$preview_png" "$win_w" "$win_h" > "$out_dir/pixel-report.txt"

log_dir="$runtime/data/LaserGRBL/logs"
if [[ -d "$log_dir" ]]; then
  cp -a "$log_dir" "$out_dir/app-logs"
fi

if command -v glxinfo >/dev/null 2>&1; then
  glxinfo -B > "$out_dir/glxinfo.txt" 2>&1 || true
fi

if ! grep -i "Avalonia OpenGL rendered frame" "$out_dir/opengl-diagnostics.log" >/dev/null 2>&1; then
  echo "Avalonia OpenGL render-frame diagnostic was not captured." >&2
  exit 1
fi

cat "$out_dir/pixel-report.txt"
echo "Artifacts: $out_dir"
