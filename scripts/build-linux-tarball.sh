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
mkdir -p "$staging_dir/notices" "$staging_dir/packaging"
mkdir -p "$staging_dir/Sound"
cp "$root/LaserGRBL/Sound/"*.wav "$staging_dir/Sound/"
cp "$root/LICENSE.md" "$staging_dir/notices/"
cp "$root/README.md" "$staging_dir/notices/"
cp "$root/packaging/linux/THIRD-PARTY-NOTICES.md" "$staging_dir/notices/"
cp "$root/packaging/linux/package-manifest.json" "$staging_dir/packaging/"

tar -C "$root/artifacts/package" -czf "$archive" "$package_name"
sha256sum "$archive" > "$archive.sha256"

echo "$archive"
echo "$archive.sha256"
