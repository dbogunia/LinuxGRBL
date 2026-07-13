#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
rid="${1:-linux-x64}"
version="${2:-0.1.0}"
serial_device="${3:-}"
out_dir="${4:-$root/artifacts/release-hardware-validation}"
package_name="linuxgrbl-avalonia-$version-$rid"
archive="$root/artifacts/$package_name.tar.gz"
checksum="$archive.sha256"

rm -rf "$out_dir"
mkdir -p "$out_dir"

report="$out_dir/report.md"
status="pass"

write_report_header() {
  {
    echo "# Release Hardware Validation"
    echo
    echo "- Date UTC: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo "- RID: $rid"
    echo "- Version: $version"
    echo "- Archive: $archive"
    echo
  } > "$report"
}

append_result() {
  local state="$1"
  local item="$2"
  local detail="$3"
  echo "- [$state] $item: $detail" >> "$report"
}

find_serial_device() {
  if [[ -n "$serial_device" ]]; then
    echo "$serial_device"
    return
  fi

  local candidate
  candidate="$(find /dev/serial/by-id -maxdepth 1 -type l 2>/dev/null | sort | head -n 1 || true)"
  if [[ -n "$candidate" ]]; then
    echo "$candidate"
    return
  fi

  candidate="$(find /dev -maxdepth 1 \( -name 'ttyUSB*' -o -name 'ttyACM*' \) 2>/dev/null | sort | head -n 1 || true)"
  if [[ -n "$candidate" ]]; then
    echo "$candidate"
  fi
}

write_report_header

if [[ ! -f "$archive" || ! -f "$checksum" ]]; then
  append_result "fail" "package artifact" "missing tarball or checksum; run scripts/build-linux-tarball.sh $rid $version first"
  status="fail"
else
  sha256sum -c "$checksum" > "$out_dir/checksum.txt"
  append_result "pass" "package checksum" "$(cat "$out_dir/checksum.txt")"
fi

if [[ "$status" == "pass" ]]; then
  install_root="$out_dir/clean-install"
  xdg_root="$out_dir/xdg"
  mkdir -p "$install_root" "$xdg_root"
  tar -xzf "$archive" -C "$install_root"
  package_dir="$install_root/$package_name"

  XDG_DATA_HOME="$xdg_root" "$package_dir/install-desktop-integration.sh" > "$out_dir/desktop-install.log" 2>&1
  test -x "$package_dir/LaserGRBL.Avalonia"
  test -f "$xdg_root/applications/linuxgrbl.desktop"
  test -f "$xdg_root/mime/packages/application-x-lasergrbl-project.xml"
  test -f "$xdg_root/icons/hicolor/scalable/apps/linuxgrbl.svg"
  rg "Exec=$package_dir/LaserGRBL.Avalonia %f" "$xdg_root/applications/linuxgrbl.desktop" > "$out_dir/desktop-exec.txt"
  append_result "pass" "clean install desktop/MIME smoke" "$package_dir"

  sample="$out_dir/clean-install-sample.gcode"
  printf "G21\nG90\nG0 X0 Y0\nG1 X5 Y5 F500\n" > "$sample"
  XDG_CONFIG_HOME="$out_dir/runtime/config" \
  XDG_DATA_HOME="$out_dir/runtime/data" \
  XDG_CACHE_HOME="$out_dir/runtime/cache" \
    timeout 8s "$package_dir/LaserGRBL.Avalonia" "$sample" > "$out_dir/startup.stdout.log" 2> "$out_dir/startup.stderr.log" || startup_code=$?
  startup_code="${startup_code:-0}"
  if [[ "$startup_code" == "124" ]]; then
    append_result "pass" "clean install startup/file-open smoke" "app stayed alive until timeout with sample G-code"
  else
    append_result "fail" "clean install startup/file-open smoke" "unexpected exit code $startup_code"
    status="fail"
  fi
fi

device="$(find_serial_device)"
if [[ -z "$device" ]]; then
  append_result "blocked" "serial hardware" "no /dev/serial/by-id, /dev/ttyUSB*, or /dev/ttyACM* device found"
  status="blocked"
elif [[ ! -e "$device" ]]; then
  append_result "fail" "serial hardware" "requested device does not exist: $device"
  status="fail"
elif [[ ! -r "$device" || ! -w "$device" ]]; then
  append_result "blocked" "serial permissions" "$device exists but is not readable/writable by the current user"
  status="blocked"
else
  append_result "pass" "serial hardware present" "$device"
  {
    echo
    echo "## Manual GRBL Workflow Evidence Required"
    echo
    echo "Record the controller model/firmware and validate:"
    echo
    echo "1. Detect stable port, preferably /dev/serial/by-id."
    echo "2. Connect at the target baud rate."
    echo "3. Send a harmless status/manual command."
    echo "4. Load a safe sample G-code file."
    echo "5. Run, hold, resume, reset/abort, and disconnect with the laser disabled or safe test fixture installed."
  } >> "$report"
fi

echo >> "$report"
echo "Final status: $status" >> "$report"
cat "$report"

case "$status" in
  pass) exit 0 ;;
  blocked) exit 3 ;;
  *) exit 1 ;;
esac
