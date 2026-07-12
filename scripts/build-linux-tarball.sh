#!/usr/bin/env bash
set -euo pipefail

rid="${1:-linux-x64}"
version="${2:-0.1.0}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_dir="$root/artifacts/publish/$rid"
package_name="linuxgrbl-avalonia-$version-$rid"
staging_dir="$root/artifacts/package/$package_name"
archive="$root/artifacts/$package_name.tar.gz"

rm -rf "$publish_dir" "$staging_dir" "$archive" "$archive.sha256"
mkdir -p "$publish_dir" "$staging_dir"

dotnet publish "$root/LaserGRBL.Avalonia/LaserGRBL.Avalonia.csproj" \
  -c Release \
  -r "$rid" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:Version="$version" \
  -o "$publish_dir"

cp -a "$publish_dir/." "$staging_dir/"
mkdir -p "$staging_dir/notices" "$staging_dir/packaging" "$staging_dir/desktop" "$staging_dir/mime" "$staging_dir/icons"
mkdir -p "$staging_dir/Sound"
cp "$root/LaserGRBL/Sound/"*.wav "$staging_dir/Sound/"
cp "$root/LICENSE.md" "$staging_dir/notices/"
cp "$root/README.md" "$staging_dir/notices/"
cp "$root/packaging/linux/THIRD-PARTY-NOTICES.md" "$staging_dir/notices/"
cp "$root/packaging/linux/package-manifest.json" "$staging_dir/packaging/"
cp "$root/packaging/linux/install-desktop-integration.sh" "$staging_dir/"
chmod +x "$staging_dir/install-desktop-integration.sh"
cp "$root/packaging/linux/desktop/linuxgrbl.desktop" "$staging_dir/desktop/"
cp "$root/packaging/linux/mime/application-x-lasergrbl-project.xml" "$staging_dir/mime/"
cp "$root/packaging/linux/icons/linuxgrbl.svg" "$staging_dir/icons/"

tar -C "$root/artifacts/package" -czf "$archive" "$package_name"
sha256sum "$archive" > "$archive.sha256"

echo "$archive"
echo "$archive.sha256"
