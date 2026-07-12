# Linux Port MVP Readiness

Date: 2026-07-12

Branch: `feature/16-mvp-validation`

Baseline master commit after PR #23: `d50df493a602fb2bb8253c45fa5b1052c601dca3`

## Environment

| Area | Value |
| --- | --- |
| Host | Linux `t3500` |
| Kernel | `6.8.0-134-generic` |
| Distribution | Linux Mint 22.3 |
| Architecture | `x86_64` / `linux-x64` |
| .NET SDK | `8.0.422` |
| .NET runtime | `8.0.28` |
| Session | `DISPLAY=:0`, `XDG_SESSION_TYPE=x11` |
| Package artifact | `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz` |

## Summary

The Linux MVP is buildable, testable, packageable, and the Avalonia shell starts on the validation host. Automated validation covers the core workflow model, serial abstractions, emulator/fake transports, G-code file loading, 2D/3D scene models, OpenGL draw backend through fake GL, packaging metadata, image conversion, device-access policy, safe shutdown, and resource ownership.

The MVP is not release-ready yet because required manual/hardware validations are still missing:

- real GPU/display nonblank 3D OpenGL validation from Task 13B/16/22
- real USB GRBL device validation
- real clean-install package smoke with serial hardware
- final parity, diagnostics, safety/legal, privacy, localization, and user-data tasks from Tasks 17-22

## Validation Table

| Area | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Restore/build | Pass | `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln -m:1` | 0 warnings, 0 errors |
| Test suite | Pass | `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln -m:1 /nr:false` | 192/192 pass |
| Avalonia startup | Pass | `timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia` | Process stayed alive until timeout; log records shell bootstrap |
| Startup file-open argument | Pass | `timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia /tmp/linuxgrbl-task16-smoke.gcode` | Process stayed alive until timeout |
| Package build | Pass | `scripts/build-linux-tarball.sh linux-x64 0.1.0` | Tarball and `.sha256` produced |
| Package checksum | Pass | `sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256` | OK |
| Package contents | Pass | `tar -tzf ... | rg ...` | binary, sound cues, desktop, MIME, icon, installer, manifest present |
| Package desktop installer | Pass after fix | Extracted tarball under `/tmp`, ran `XDG_DATA_HOME=/tmp/linuxgrbl-task16-smoke/xdg ./install-desktop-integration.sh` | Found and fixed wrong package-root calculation in Task 16 |
| CI/support matrix | Pass | Tests + Task 15A GitHub Actions evidence | CI target is `LaserGRBL.Linux.sln` |
| Image/GDI compatibility | Pass | Task 15B fixtures in 192-test suite | SkiaSharp backend; no production `System.Drawing.Common` dependency |
| Device access/package policy | Pass | Task 15C tests in 192-test suite | tar.gz only; host permissions; `/dev/serial/by-id`; `dialout` guidance |
| Safe shutdown/resource ownership | Pass | Task 15D tests in 192-test suite | bounded shutdown, fail-closed recovery, file lock ownership, PTY smoke |
| Emulator/fake workflow | Pass | Main workflow, transport, lifecycle, command streaming tests | Automated fake/emulator coverage only |
| Serial device discovery | Partial | Tests + host probe | No `/dev/serial/by-id`, `/dev/ttyUSB*`, or `/dev/ttyACM*` devices present on host |
| Sound fallback | Pass | Task 15 tests + host tools | `pw-play`, `paplay`, and `aplay` present |
| WiFi service | Partial | Task 10 tests + `nmcli` present | `nmcli` exists; no manual WiFi scan/config validation recorded |
| Firmware flash dry-run/tool policy | Partial | Task 14/15 tests | `avrdude` not installed on host; real flashing not authorized/performed |
| Autotrace/tool policy | Partial | Task 09/15 tests | `autotrace` not installed on host |
| OpenGL host info | Blocker | `glxinfo -B` failed: `unable to open display :0` | No real nonblank GPU/render validation |
| 3D preview interactions | Blocker | Automated model/fake-GL tests only | Manual rotate/pan/zoom/reset/progress/cursor validation not recorded |
| Real GRBL hardware | Blocker | Host probe found no serial device | Real controller workflow not validated |
| Clean install with hardware | Blocker | `/tmp` package smoke only | Fresh user/session with serial hardware not validated |

## Exact Commands

```bash
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln -m:1
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln -m:1 /nr:false
scripts/build-linux-tarball.sh linux-x64 0.1.0
sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256
tar -tzf artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz | rg 'LaserGRBL.Avalonia$|Sound/.+\.wav|install-desktop-integration.sh|desktop/linuxgrbl.desktop|mime/application-x-lasergrbl-project.xml|icons/linuxgrbl.svg|packaging/package-manifest.json'
timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia
timeout 8s artifacts/publish/linux-x64/LaserGRBL.Avalonia /tmp/linuxgrbl-task16-smoke.gcode
glxinfo -B
```

Clean-ish package installer smoke:

```bash
rm -rf /tmp/linuxgrbl-task16-smoke
mkdir -p /tmp/linuxgrbl-task16-smoke
tar -xzf artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz -C /tmp/linuxgrbl-task16-smoke
cd /tmp/linuxgrbl-task16-smoke/linuxgrbl-avalonia-0.1.0-linux-x64
XDG_DATA_HOME=/tmp/linuxgrbl-task16-smoke/xdg ./install-desktop-integration.sh
test -f /tmp/linuxgrbl-task16-smoke/xdg/applications/linuxgrbl.desktop
test -f /tmp/linuxgrbl-task16-smoke/xdg/mime/packages/application-x-lasergrbl-project.xml
test -f /tmp/linuxgrbl-task16-smoke/xdg/icons/hicolor/scalable/apps/linuxgrbl.svg
rg 'Exec=.*/LaserGRBL.Avalonia %f' /tmp/linuxgrbl-task16-smoke/xdg/applications/linuxgrbl.desktop
```

## Fix Made During Validation

Task 16 found a real package installer bug in `packaging/linux/install-desktop-integration.sh`: the script treated the parent of the extracted package as the app directory. The clean-ish smoke failed because it looked for `desktop/linuxgrbl.desktop` one level too high.

Fix:

- changed `app_dir` to the directory containing `install-desktop-integration.sh`
- added test coverage in `LinuxPackagingDeviceAccessTests`
- rebuilt package and reran clean-ish installer smoke successfully

## Remaining Blockers

| Severity | Blocker | Impacted Workflow | Recommended Next Task |
| --- | --- | --- | --- |
| Release-blocking | Real GPU/display nonblank OpenGL validation missing | 3D preview, rotate/pan/zoom/reset/progress/cursor | Task 22, or a dedicated validation pass before release |
| Release-blocking | Real USB GRBL hardware not available on validation host | connect/run/hold/resume/reset/manual command against physical controller | Task 16 hardware pass or release branch validation |
| Release-blocking | Clean-install package smoke with real serial hardware not performed | install docs, desktop/MIME, serial permission path | Release branch validation after Task 16 |
| Release-blocking | Safety/legal first-run language not complete | user understanding of software safety limits | Task 20 |
| High | Diagnostics/support bundle not complete | supportability after field failures | Task 19 |
| High | Privacy/update policy not complete | update checks and user trust | Task 21 |
| Medium | Localization/resx migration not complete | non-English UI/resources | Task 17 |
| Medium | User data compatibility not complete | migration of existing settings/projects | Task 18 |
| Medium | Final parity review not complete | gap closure against legacy LaserGRBL | Task 22 |

## MVP Verdict

The Linux port MVP is ready to continue into Tasks 17-22. It is not release-ready. The largest remaining release blockers are real OpenGL/GPU validation, real hardware serial validation, clean-install package validation, and final safety/legal/privacy/parity work.
