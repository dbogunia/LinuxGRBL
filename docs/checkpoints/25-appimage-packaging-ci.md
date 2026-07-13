# Task 25 Checkpoint: AppImage Packaging And CI Artifact

Planned and implemented AppImage as a second Linux package artifact alongside the existing self-contained tarball.

## Implemented Changes

- Added `scripts/build-linux-appimage.sh`.
- The script publishes the Avalonia app for `linux-x64`, stages an AppDir, validates the embedded desktop entry, runs `appimagetool`, marks the AppImage executable, and writes a `.sha256` file.
- The AppDir contains:
  - `AppRun`;
  - root and `usr/share/applications` desktop entries;
  - root and hicolor SVG icons;
  - MIME metadata;
  - AppStream metadata;
  - the app binary and bundled sound cues;
  - package manifest, license, README, and third-party notices.
- Updated Linux packaging, support matrix, readiness, and parity documentation.
- Updated CI to build AppImage on `ubuntu-24.04`, validate checksum, extract the AppImage, inspect AppDir contents, smoke launch it with `xvfb-run`, and upload it as a GitHub Actions artifact.
- Extended `scripts/validate-release-hardware.sh` with `LINUXGRBL_PACKAGE_FORMAT=appimage` so final release validation can smoke either tarball or AppImage while still failing closed without serial hardware.

## Device Access Position

AppImage is a portable package format, not a sandbox. It does not install udev rules, add the user to `dialout`, or grant serial/USB permissions. Host serial permissions and the release hardware validation blocker remain unchanged.

## Expected Validation

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`
- `scripts/build-linux-appimage.sh linux-x64 0.1.0`
- `sha256sum -c artifacts/LinuxGRBL-0.1.0-x86_64.AppImage.sha256`
- `artifacts/LinuxGRBL-0.1.0-x86_64.AppImage --appimage-extract`

## Remaining Risks

- Release blocker: real USB GRBL hardware validation still requires physical hardware.
- Release blocker: clean-install package/device validation with serial hardware still requires physical hardware.
- AppImage build depends on `appimagetool`; CI downloads the official continuous `appimagetool-x86_64.AppImage` instead of vendoring it.

## Completion Status

Task 25 adds AppImage packaging and CI artifact generation. It does not close hardware release blockers.
