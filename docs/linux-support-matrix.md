# Linux Support Matrix

This matrix defines the current support contract for the SDK-style Linux port. It applies to `LaserGRBL.Linux.sln`, not the legacy .NET Framework WinForms solution.

## Supported For CI

| Area | Supported | Evidence |
| --- | --- | --- |
| .NET SDK | 8.0.422 with `global.json` roll-forward to latest feature band | `global.json`, GitHub Actions setup-dotnet |
| Runner OS | Ubuntu 24.04 GitHub-hosted runner | `.github/workflows/linux-port.yml` |
| Target framework | `net8.0` | SDK-style Core, Platform, Avalonia, Tests projects |
| Build architecture | `linux-x64` | Task 15 tarball manifest and CI runner |
| Package format | self-contained `tar.gz` metadata and script validation | `packaging/linux/package-manifest.json`, `scripts/build-linux-tarball.sh` |
| Test scope | restore, build, xUnit tests, package metadata guard | `.github/workflows/linux-port.yml` |

## Tested Locally But Not Yet Claimed As Release Support

| Area | Status | Notes |
| --- | --- | --- |
| Debian/Ubuntu-like developer host | tested for build/test/package script | Not a clean-install package validation. |
| `linux-x64` tarball generation | tested locally | Task 15C owns clean-install smoke tests and device access. |
| Headless renderer fallback | tested through view-model/control construction and OpenGL fallback diagnostics | Does not prove GPU nonblank rendering. |

## Untested Or Not Supported Yet

| Area | Status | Required Follow-Up |
| --- | --- | --- |
| `linux-arm64` | not supported yet | Add CI runner or cross-publish validation before claiming support. |
| Wayland | not manually validated | Task 16/22 graphical validation. |
| X11 | not manually validated | Task 16/22 graphical validation. |
| OpenGL/Mesa minimum | not manually validated | Task 13B/16/22 must record real GPU/display evidence. |
| Flatpak, AppImage, `.deb`, RPM | not supported by Task 15 | Task 15C can add a package-specific access model. |
| Serial/USB permissions from installed package | not validated | Task 15C. |
| Physical laser hardware | not required in CI | Task 16 gated validation. |

## Minimum Runtime Expectations

- Linux distribution with glibc compatible with the selected .NET 8 Linux RID.
- A graphical desktop session is required for normal Avalonia UI use.
- Sound playback is optional and uses the first available host player from `pw-play`, `paplay`, or `aplay`.
- Firmware flashing requires host `avrdude`.
- WiFi discovery/configuration requires NetworkManager `nmcli` and appropriate user authorization.
- Optional vector conversion paths require host `autotrace`.

## Renderer Smoke Strategy

Ordinary CI remains headless. It validates:

- 2D preview scene generation and transform math.
- 3D scene/camera/projection models.
- OpenGL host boundary construction and explicit fallback diagnostic paths.
- Immediate OpenGL draw backend through fake `IOpenGlApi` draw-call tests.

CI must not mark real GPU rendering as validated. A graphical runner or manual host must separately verify:

- Avalonia starts under X11 and Wayland.
- OpenGL context creation succeeds.
- 3D preview is nonblank.
- rotate, tilt, zoom, reset, progress, and machine cursor are visible.
- Fallback diagnostics are shown when OpenGL is unavailable.

Until that evidence exists, Task 13B manual Linux GPU/display validation remains a release blocker for Task 16/22.

## Local CI Reproduction

Run from the repository root:

```bash
dotnet restore LaserGRBL.Linux.sln
dotnet build LaserGRBL.Linux.sln --no-restore -m:1
dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false
test -f packaging/linux/package-manifest.json
test -f packaging/linux/THIRD-PARTY-NOTICES.md
test -x scripts/build-linux-tarball.sh
```

The full tarball package path can be reproduced with:

```bash
scripts/build-linux-tarball.sh linux-x64 0.1.0
sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256
```
