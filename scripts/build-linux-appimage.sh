#!/usr/bin/env bash
set -euo pipefail

rid="${1:-linux-x64}"
version="${2:-0.1.0}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_dir="$root/artifacts/publish/$rid"
appdir="$root/artifacts/appimage/LinuxGRBL.AppDir"
appimage="$root/artifacts/LinuxGRBL-$version-x86_64.AppImage"
appimagetool="${APPIMAGETOOL:-appimagetool}"
app_id="io.github.dbogunia.LinuxGRBL"

if [[ "$rid" != "linux-x64" ]]; then
  echo "AppImage packaging currently supports linux-x64 only; got '$rid'." >&2
  exit 2
fi

if ! command -v "$appimagetool" >/dev/null 2>&1 && [[ ! -x "$appimagetool" ]]; then
  echo "appimagetool not found. Set APPIMAGETOOL=/path/to/appimagetool-x86_64.AppImage or install appimagetool." >&2
  exit 2
fi

if ! command -v desktop-file-validate >/dev/null 2>&1; then
  echo "desktop-file-validate not found; install desktop-file-utils." >&2
  exit 2
fi

rm -rf "$publish_dir" "$appdir" "$appimage" "$appimage.sha256"
mkdir -p "$publish_dir" "$appdir/usr/bin" "$appdir/usr/share/applications" "$appdir/usr/share/icons/hicolor/scalable/apps" "$appdir/usr/share/mime/packages" "$appdir/usr/share/metainfo" "$appdir/usr/share/doc/linuxgrbl"

dotnet publish "$root/LaserGRBL.Avalonia/LaserGRBL.Avalonia.csproj" \
  -c Release \
  -r "$rid" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:Version="$version" \
  -o "$publish_dir"

cp -a "$publish_dir/." "$appdir/usr/bin/"
mkdir -p "$appdir/usr/bin/Sound"
cp "$root/LaserGRBL/Sound/"*.wav "$appdir/usr/bin/Sound/"

cp "$root/LICENSE.md" "$appdir/usr/share/doc/linuxgrbl/"
cp "$root/README.md" "$appdir/usr/share/doc/linuxgrbl/"
cp "$root/packaging/linux/THIRD-PARTY-NOTICES.md" "$appdir/usr/share/doc/linuxgrbl/"
cp "$root/packaging/linux/package-manifest.json" "$appdir/usr/share/doc/linuxgrbl/"
cp "$root/packaging/linux/mime/application-x-lasergrbl-project.xml" "$appdir/usr/share/mime/packages/"
cp "$root/packaging/linux/metainfo/io.github.dbogunia.LinuxGRBL.appdata.xml" "$appdir/usr/share/metainfo/"
cp "$root/packaging/linux/icons/linuxgrbl.svg" "$appdir/usr/share/icons/hicolor/scalable/apps/linuxgrbl.svg"
cp "$root/packaging/linux/icons/linuxgrbl.svg" "$appdir/linuxgrbl.svg"
sed 's/^Exec=.*/Exec=AppRun %f/' "$root/packaging/linux/desktop/linuxgrbl.desktop" > "$appdir/usr/share/applications/$app_id.desktop"
cp "$appdir/usr/share/applications/$app_id.desktop" "$appdir/$app_id.desktop"

cat > "$appdir/AppRun" <<'APPRUN'
#!/usr/bin/env bash
set -euo pipefail
self="$(readlink -f "$0")"
here="$(dirname "$self")"
exec "$here/usr/bin/LaserGRBL.Avalonia" "$@"
APPRUN
chmod +x "$appdir/AppRun"

desktop-file-validate "$appdir/$app_id.desktop"

ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$appimagetool" "$appdir" "$appimage"
chmod +x "$appimage"
sha256sum "$appimage" > "$appimage.sha256"

echo "$appimage"
echo "$appimage.sha256"
