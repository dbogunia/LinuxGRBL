# Task 15A Checkpoint: Linux CI And Support Matrix

## Summary

Added a reproducible Linux CI workflow, pinned .NET SDK selection, and an explicit Linux support matrix for the Avalonia port. The workflow targets `LaserGRBL.Linux.sln` only and validates restore, build, tests, package metadata, packaging script presence, and Linux-solution targeting guards.

## Implemented Changes

- Added `global.json` pinning .NET SDK `8.0.422` with `latestFeature` roll-forward.
- Added `.github/workflows/linux-port.yml` for `pull_request` and `push` to `master`.
- CI uses Ubuntu 24.04, `actions/checkout@v4`, `actions/setup-dotnet@v4`, and `global.json`.
- CI runs:
  - `dotnet restore LaserGRBL.Linux.sln`
  - `dotnet build LaserGRBL.Linux.sln --no-restore -m:1`
  - `dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`
  - package metadata/script guards for Task 15 tarball packaging.
  - a workflow guard proving `LaserGRBL.Linux.sln` is the CI target.
- Added `docs/linux-support-matrix.md` with supported, tested-but-not-supported, and untested environments.
- Documented the headless renderer smoke strategy and separated it from real GPU/display validation.
- Added tests validating `global.json`, CI Linux-solution targeting, package metadata guard coverage, and support matrix risk statements.

## Test Evidence

Local CI-equivalent commands run:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore LaserGRBL.Linux.sln`

Passed outside the sandbox. The sandboxed restore returned a non-diagnostic failure, consistent with previous sandbox/network constraints.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1`

Completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

Passed 166 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Package metadata guard commands passed locally:

- `test -f packaging/linux/package-manifest.json`
- `test -f packaging/linux/THIRD-PARTY-NOTICES.md`
- `test -x scripts/build-linux-tarball.sh`
- manifest checks for `tar.gz` and `SHA256`
- script checks for `dotnet publish` and `sha256sum`
- workflow check for `LaserGRBL.Linux.sln`

## Support Matrix

Support matrix added at `docs/linux-support-matrix.md`.

Current CI-supported scope:

- Ubuntu 24.04 GitHub-hosted runner.
- .NET SDK 8.0.422 via `global.json`.
- `net8.0` SDK-style Linux solution.
- `linux-x64` build/test/package metadata path.
- `tar.gz` package metadata/script validation.

Explicitly not claimed yet:

- `linux-arm64`.
- Flatpak/AppImage/.deb/RPM.
- Wayland/X11 manual GUI validation.
- Real OpenGL/Mesa minimum.
- Clean-install package smoke tests.
- Serial/USB permissions from an installed package.

## Renderer And Headless Evidence

CI validates non-graphical renderer behavior: 2D/3D scene models, OpenGL fallback diagnostics, and fake `IOpenGlApi` draw-call tests. It does not claim nonblank GPU rendering.

Task 13B manual Linux GPU/display validation remains not verified and release-blocking until Task 16/22 records real graphical evidence.

## Remaining Risks

- GitHub Actions must run on GitHub after PR creation to confirm hosted runner behavior.
- Task 15A does not publish package artifacts; release publishing is out of scope.
- Task 15C still owns desktop integration, MIME registration, serial/USB permission model, package-specific device access, and clean-install validation.
- Task 16/22 still own graphical OpenGL nonblank validation and final release readiness.

## Commit And Push

Implementation branch: `feature/15a-ci-support-matrix`

Commit and push: pending.

## Completion Status

Task 15A is implemented locally: reproducible Linux CI configuration, SDK pinning, support matrix, package metadata guards, and CI-target tests are in place. Hosted CI result will be visible after the pull request is opened.
