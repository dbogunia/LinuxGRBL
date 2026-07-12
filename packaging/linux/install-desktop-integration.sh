#!/usr/bin/env bash
set -euo pipefail

app_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
applications_dir="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
icons_dir="${XDG_DATA_HOME:-$HOME/.local/share}/icons/hicolor/scalable/apps"
mime_dir="${XDG_DATA_HOME:-$HOME/.local/share}/mime/packages"

mkdir -p "$applications_dir" "$icons_dir" "$mime_dir"

sed "s|^Exec=.*|Exec=$app_dir/LaserGRBL.Avalonia %f|" \
  "$app_dir/desktop/linuxgrbl.desktop" > "$applications_dir/linuxgrbl.desktop"
cp "$app_dir/icons/linuxgrbl.svg" "$icons_dir/linuxgrbl.svg"
cp "$app_dir/mime/application-x-lasergrbl-project.xml" "$mime_dir/application-x-lasergrbl-project.xml"

update-mime-database "${XDG_DATA_HOME:-$HOME/.local/share}/mime"
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$applications_dir" || true
fi
