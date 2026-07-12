# Linux Packaging

Task 15 selects a self-contained `tar.gz` package as the first supported Linux artifact. This avoids privileged installation, keeps update behavior non-mutating, and leaves distro-specific device access and desktop integration for Task 15C.

## Build

From the repository root:

```bash
scripts/build-linux-tarball.sh linux-x64 0.1.0
```

The script runs `dotnet publish` for `LaserGRBL.Avalonia`, copies notice metadata, creates:

- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`
- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

The tarball includes the legacy `.wav` cues under `Sound/` for `LinuxSoundService`.

## Runtime Dependencies

The selected package is self-contained for the target RID. Optional feature tools remain host dependencies:

- `pw-play`, `paplay`, or `aplay` for sound playback.
- `avrdude` for firmware flashing.
- `autotrace` for optional conversion paths.
- `NetworkManager`/`nmcli` for WiFi discovery and configuration.

Serial permissions, USB access, desktop integration, MIME registration, and clean-install smoke tests are Task 15C release-blocking work.

## Update Behavior

Task 15 implements release notification mechanics only. It does not download, execute, elevate, or install updates. Privacy policy and opt-in/out behavior remain Task 21.

## Integrity

Release publishing must publish the tarball and `.sha256` file together. Users can verify with:

```bash
sha256sum -c linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256
```
