# Task 16 Checkpoint: End-To-End Linux MVP Validation

## Summary

Validated the Linux MVP end to end using local build/test/package/startup evidence and produced `docs/linux-port-readiness.md`. Automated validation is green. Manual hardware/GPU validations remain clearly marked as release blockers.

## Implemented Changes

- Added `docs/linux-port-readiness.md`.
- Added this checkpoint report.
- Fixed `packaging/linux/install-desktop-integration.sh` after validation found that it resolved the extracted package root incorrectly.
- Added package installer regression coverage in `LinuxPackagingDeviceAccessTests`.

## Test Evidence

Environment:

- Linux Mint 22.3
- Kernel `6.8.0-134-generic`
- `linux-x64`
- .NET SDK `8.0.422`
- `DISPLAY=:0`
- `XDG_SESSION_TYPE=x11`

Commands run:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln -m:1`

Completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln -m:1 /nr:false`

Passed 192 tests outside the sandbox.

`scripts/build-linux-tarball.sh linux-x64 0.1.0`

Produced:

- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`
- `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

`sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

Returned `OK`.

Archive listing confirmed:

- `LaserGRBL.Avalonia`
- `Sound/*.wav`
- `install-desktop-integration.sh`
- `desktop/linuxgrbl.desktop`
- `mime/application-x-lasergrbl-project.xml`
- `icons/linuxgrbl.svg`
- `packaging/package-manifest.json`

Avalonia startup smoke:

- `timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia`
- `timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia /tmp/linuxgrbl-task16-smoke.gcode`

Both stayed alive until timeout without crash output. App log recorded shell bootstrap.

Clean-ish package installer smoke:

- extracted tarball under `/tmp/linuxgrbl-task16-smoke`
- ran `XDG_DATA_HOME=/tmp/linuxgrbl-task16-smoke/xdg ./install-desktop-integration.sh`
- verified user-scoped desktop entry, MIME XML, icon, and absolute `Exec=.../LaserGRBL.Avalonia %f`

OpenGL host probe:

- `glxinfo -B` failed with `Error: unable to open display :0`

## Validation Result

Automated MVP validation is passing.

Release readiness is blocked by:

- missing real GPU/display nonblank OpenGL validation
- missing manual 3D preview interaction validation
- no real USB GRBL device connected on validation host
- no clean-install package smoke with serial hardware
- Tasks 17-22 still pending for localization, user data compatibility, diagnostics, safety/legal, privacy/update policy, and final feature parity

## Git Commit And Push

Implementation branch: `feature/16-mvp-validation`

Implementation commit: `d0891f6` (`Task 16: End-to-end Linux validation`)

Push: pending until this checkpoint metadata is committed.

## Completion Status

Task 16 is implemented locally. The Linux MVP has a readiness report with exact commands, pass/fail evidence, package artifact path, and release blockers separated from final parity work.
