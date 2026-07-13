# Linux Packaging

Task 15C selects a self-contained `tar.gz` package as the first supported Linux artifact. This avoids privileged installation, keeps update behavior non-mutating, and makes device access an explicit host permission decision.

No `.deb`, RPM, AppImage, or Flatpak artifact is supported yet. Do not advertise sandboxed device access until a sandboxed package exists and its serial/USB permissions are tested.

## Build

From the repository root:

```bash
scripts/build-linux-tarball.sh linux-x64 0.1.0
```

The script runs `dotnet publish` for `LaserGRBL.Avalonia`, copies notice metadata, creates:

- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`
- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

The tarball includes the legacy `.wav` cues under `Sound/` for `LinuxSoundService`.

It also includes optional desktop integration assets:

- `desktop/linuxgrbl.desktop`
- `icons/linuxgrbl.svg`
- `mime/application-x-lasergrbl-project.xml`

## Device Access

The tarball does not install udev rules, elevate privileges, or modify groups. Serial and USB access remain normal host Linux permissions.

Recommended port selection:

- Prefer stable `/dev/serial/by-id/*` devices when present.
- Fall back to `/dev/ttyUSB*` or `/dev/ttyACM*` only when no stable by-id device exists.

Least-privilege serial remediation for Debian/Ubuntu-style systems:

```bash
sudo usermod -aG dialout "$USER"
```

Then log out and back in before retrying. Do not use `sudo` to run the GUI as a workaround.

If a distribution uses a different serial device group, use that distribution's documented equivalent. Broad world-writable udev rules are not part of the supported package model.

## Desktop And MIME Integration

The tarball ships desktop/MIME assets but does not install them automatically. For a per-user install from an extracted package directory:

```bash
./install-desktop-integration.sh
```

The installer writes a user-scoped `.desktop` entry with an absolute `Exec=<extracted-package>/LaserGRBL.Avalonia %f`, copies the SVG icon, installs MIME metadata, and updates the user MIME database.

The packaged `.desktop` template uses `Exec=LaserGRBL.Avalonia %f`. When launched with a file path, the Avalonia shell passes the first non-option argument to the workflow loader. G-code files can load directly; raster, SVG, and `.lps` project paths are recognized and routed to the current user-facing conversion/project placeholders until those paths are completed.

## Runtime Dependencies

The selected package is self-contained for the target RID. OpenGL is provided by the host graphics stack. Optional feature tools remain host dependencies:

- `pw-play`, `paplay`, or `aplay` for sound playback.
- `avrdude` for firmware flashing.
- `autotrace` for optional conversion paths.
- `NetworkManager`/`nmcli` for WiFi discovery and configuration.

Missing optional tools must be surfaced as actionable feature errors. The package must not silently install tools or elevate privileges.

## Clean Install Status

Task 15C validates tarball construction, metadata, desktop/MIME assets, file-open argument routing, and permission/tool remediation text in automated tests.

A repeatable clean-install and hardware validation runner is available:

```bash
scripts/validate-release-hardware.sh linux-x64 0.1.0
```

The runner validates the tarball checksum, extracts the package into an isolated validation directory, installs desktop/MIME integration under an isolated `XDG_DATA_HOME`, starts the packaged app with sample G-code, and detects a serial controller from an explicit device path, `/dev/serial/by-id/*`, `/dev/ttyUSB*`, or `/dev/ttyACM*`.

A real clean-install smoke test with serial hardware is not complete until that runner records a physical readable/writable controller and the manual GRBL workflow evidence. Missing hardware is reported as blocked, not passed, and remains release-blocking.

## Update Behavior

Task 15 implements release notification mechanics only. It does not download, execute, elevate, or install updates. Privacy policy and opt-in/out behavior remain Task 21.

## Integrity

Release publishing must publish the tarball and `.sha256` file together. Users can verify with:

```bash
sha256sum -c linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256
```
