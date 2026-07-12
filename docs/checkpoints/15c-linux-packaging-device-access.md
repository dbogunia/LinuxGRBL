# Task 15C Checkpoint: Linux Packaging And Device Access

## Summary

Bound the Linux package model to a single supported artifact: self-contained `tar.gz` for `linux-x64`. Added explicit serial/device access guidance, desktop/MIME assets, startup file argument handling, and automated tests covering the package/device-access contract.

## Selected Package Format

Supported now:

- `tar.gz`
- `linux-x64`
- self-contained .NET publish output
- user-scoped desktop/MIME assets included in the archive

Not supported yet:

- `.deb`
- RPM
- AppImage
- Flatpak
- sandboxed serial/USB access claims

The tarball does not install udev rules, modify groups, elevate privileges, or install host dependencies.

## Implemented Changes

- Added package assets:
  - `packaging/linux/desktop/linuxgrbl.desktop`
  - `packaging/linux/mime/application-x-lasergrbl-project.xml`
  - `packaging/linux/icons/linuxgrbl.svg`
- Added `packaging/linux/install-desktop-integration.sh` for user-scoped desktop/MIME installation from an extracted tarball.
- Updated `scripts/build-linux-tarball.sh` to include desktop, MIME, and icon assets.
- Updated `packaging/linux/package-manifest.json` with user-scoped desktop integration metadata.
- Rewrote `packaging/linux/README.md` with:
  - selected artifact format
  - serial/device permission model
  - per-user desktop/MIME install commands
  - optional runtime tool policy
  - clean-install release blocker status
- Added `LinuxDeviceAccessPolicy` and wired serial permission-denied messages through it.
- Added `StartupFileArguments` and wired the Avalonia app to pass the first non-option startup path into `MainWorkflowViewModel.LoadFileAsync`.
- Added tests for:
  - desktop/MIME/icon assets
  - tarball script asset inclusion
  - package manifest desktop integration metadata
  - startup file-open argument routing
  - serial permission remediation guidance
  - package README support/clean-install status
  - stable `/dev/serial/by-id` preference in port enumeration

## Device Access Model

Serial/USB access uses host Linux permissions.

Supported guidance:

- Prefer `/dev/serial/by-id/*` when present.
- Fall back to `/dev/ttyUSB*` or `/dev/ttyACM*` only when stable by-id paths are unavailable.
- For Debian/Ubuntu-style systems, add the user to `dialout`:

`sudo usermod -aG dialout "$USER"`

The user must log out and back in. Running the GUI with `sudo` is not a supported workaround.

Broad world-writable udev rules are explicitly out of scope.

## Desktop And MIME Model

The tarball ships integration assets but does not install them automatically during extraction. The included `install-desktop-integration.sh` performs a user-scoped install when the user opts in.

The desktop template uses:

`Exec=LaserGRBL.Avalonia %f`

The installer rewrites that to an absolute path under the extracted package directory.

The Avalonia shell now handles the first non-option startup argument as a file-open request. G-code files can load directly; raster, SVG, and `.lps` project paths are recognized and routed to current user-facing placeholders until those conversion/project paths are completed.

## Runtime Tool Policy

Bundled/self-contained:

- .NET runtime for `linux-x64`
- SkiaSharp native assets
- packaged sound cue files

Host-provided optional tools:

- `pw-play`, `paplay`, or `aplay` for sound playback
- `avrdude` for firmware flashing
- `autotrace` for optional conversion paths
- `NetworkManager`/`nmcli` for WiFi discovery/configuration

Missing optional tools must surface actionable feature errors. The package must not silently install tools or elevate privileges.

## Test Evidence

Local commands run:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1`

Completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

Passed 184 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Package verification:

`scripts/build-linux-tarball.sh linux-x64 0.1.0`

Produced `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`.

`sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

Returned `OK`.

Archive listing confirmed:

- `desktop/linuxgrbl.desktop`
- `install-desktop-integration.sh`
- `mime/application-x-lasergrbl-project.xml`
- `icons/linuxgrbl.svg`
- `packaging/package-manifest.json`

## Remaining Release Blockers

- A real clean-install smoke test on a fresh Linux user/session with serial hardware is not performed yet.
- Manual Linux GPU/display validation from Task 13B is still not verified and remains release-blocking until Task 16/22 records real graphical evidence.
- No sandboxed package format is supported yet; Flatpak/AppImage/.deb/RPM permissions are not claimed.

## Commit And Push

Implementation branch: `feature/15c-package-device-access`

Implementation commit: `43833b1` (`Task 15C: Linux packaging device access`)

Push: pending until this checkpoint metadata is committed.

## Completion Status

Task 15C is implemented locally. The Linux tarball now has an explicit tested device-access and desktop-integration contract, with clean-install hardware validation clearly marked as a remaining release blocker.
