# Task 15 Checkpoint: Sound, Update, And Linux Packaging

## Summary

Implemented the Linux technical mechanisms for sound playback fallback, update release notification, and a reproducible self-contained Linux tarball package path. The implementation avoids privileged auto-update behavior and records package notices, runtime dependencies, provenance, and SHA256 integrity verification.

## Implemented Changes

- Added `LinuxSoundService`, which maps `SoundCue` values to `.wav` files and tries `pw-play`, `paplay`, then `aplay` through `IProcessRunner`.
- Sound playback now returns `OperationResult` failures for missing files or unavailable audio tools instead of throwing.
- Added disabled-sound behavior that succeeds without starting any process.
- Added update manifest boundaries: `IUpdateManifestClient`, `HttpUpdateManifestClient`, and `ReleaseManifestUpdateService`.
- Update checks are cancellable, do not run at startup, return update/no-update/failure results, and never download, execute, elevate, or install an updater.
- Added `PackageMetadata`, `PackageDependency`, `IPackageMetadataService`, and `PackageMetadataService` validation for package id, format, RID, runtime policy, notice files, and SHA256 integrity policy.
- Wired sound, update, and package metadata services into `AppBootstrapper`.
- Added `packaging/linux/package-manifest.json`, `packaging/linux/README.md`, and `packaging/linux/THIRD-PARTY-NOTICES.md`.
- Added `scripts/build-linux-tarball.sh` to run `dotnet publish`, stage notices and sound assets, create a `tar.gz`, and write a sibling `.sha256` file.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 162 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Added tests for:

- Sound missing-file fallback.
- Sound disabled behavior.
- Sound player fallback ordering through fake process runner.
- Update available, current/no-update, disabled, and manifest failure paths.
- Package metadata manifest validation.
- Package metadata rejection when notice material is missing.
- Tarball script coverage for publish, sound assets, third-party notices, and SHA256 generation.

## Package Evidence

Ran:

`scripts/build-linux-tarball.sh linux-x64 0.1.0`

Generated ignored local artifacts:

- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`
- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

`sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256` passed.

Observed SHA256:

`71d26f47b22cb5841a95c4a2d38f80608621368f8941bddbe9ceb6b3f5203877`

The tarball contains:

- Avalonia self-contained publish output for `linux-x64`.
- `Sound/*.wav` cues.
- `notices/LICENSE.md`, `notices/README.md`, `notices/THIRD-PARTY-NOTICES.md`.
- `packaging/package-manifest.json`.

## Remaining Risks

- Task 15 selects tarball packaging only. Desktop integration, MIME registration, serial/USB permission model, package-specific device access, clean-install smoke tests, and least-privilege user guidance remain Task 15C.
- CI matrix and automated package metadata checks remain Task 15A.
- Image/GDI compatibility remains Task 15B.
- Safe shutdown/resource ownership remains Task 15D.
- Update opt-in/out policy, network-call auditing, privacy policy, and release artifact signature/checksum policy remain Task 21.
- Manual Linux GUI package launch, audio-device playback, and real update endpoint validation were not performed in this headless run.
- Task 13B manual Linux GPU/display validation remains not verified and release-blocking until Task 16/22 validation.

## Commit And Push

Implementation branch: `feature/15-packaging-sound`

Commit and push: pending.

## Completion Status

Task 15 technical mechanisms are implemented and tested: sound fallback, update notification boundary, tarball package path, notices, dependency metadata, and checksum verification are in place. Distro installation/device-access and privacy policy decisions remain in their dedicated follow-up tasks.
