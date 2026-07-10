# Linux Port Inventory And Baseline

Baseline collected on 2026-07-10 from the repository checkout at `ee56af1` on Linux Mint 22.3 (`linux-x64`). This is an inventory only; no production source was retargeted or migrated.

## Build Baseline

| Area | Observed state | Port impact |
| --- | --- | --- |
| Solution | `LaserGRBL.sln` contains only the legacy `LaserGRBL` project. | A Linux-only solution is required before CI can build the port. |
| Main application | Non-SDK `LaserGRBL/LaserGRBL.csproj`, `.NETFramework v4.0`, `WinExe`. | Cannot build against the installed Linux .NET SDK. |
| Legacy tests | `LaserGRBL.Tests` targets `netcoreapp3.1`, enables WinForms and references `Microsoft.WindowsDesktop.App`; it is not in the solution. | Tests must be moved or replaced by Linux-runnable `net8.0` tests. |
| Installed SDK | .NET SDK `8.0.422`, runtime `8.0.28`, `linux-x64`; no `global.json` and no workloads. | Task 01 should target `net8.0` and pin/document the chosen SDK policy. |

Commands executed:

```text
dotnet --info
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.sln --no-restore
```

The build fails before compilation with:

```text
error MSB3644: The reference assemblies for .NETFramework,Version=v4.0 were not found.
```

This is expected on the Linux host and is a build-system blocker, not a missing package to install for the Linux port.

## Repository Size And UI/Resource Surface

Repository inspection found 752 C# files under `LaserGRBL` and `LaserGRBL.Tests`, including 139 source files importing `System.Windows.Forms`, 239 importing `System.Drawing`, 54 WinForms designer files, and 416 `.resx` files.

The main project directly references `System.Drawing`, `System.Management`, and `System.Windows.Forms`. The port must keep the legacy project as reference only; copying it into an SDK project would carry Windows desktop dependencies into the Linux build.

Localization includes shared `Strings.*.resx`, per-form resources, icons, images, and designer metadata. Task 17 must inventory only resources reachable from ported workflows and make an explicit decision for non-text data.

## Windows, Native, And External Dependencies

| Area | Evidence | Required port direction |
| --- | --- | --- |
| WinForms/UI synchronization | `MainForm`, `GrblCore`, dialogs, user controls, 139 WinForms-using source files. | Extract domain state and callbacks behind contracts before Avalonia UI work. |
| Imaging | 239 `System.Drawing` users, including raster and SVG paths. | Task 15B must choose a supported Linux image backend and protect output with fixtures. |
| Serial | Custom serial wrappers and RJCP code include Windows P/Invoke and Unix bindings to `libnserial.so.1`. | Task 07 must select/package a maintained Linux transport, cover DTR/RTS and cancellation, and avoid an undocumented native dependency. |
| Native Windows APIs | 17 files use `DllImport`; `System.Management`, registry, `kernel32`, `winmm`, `iphlpapi`, and Windows serial paths occur in platform code. | Isolate behind Task 02 contracts; do not compile Windows implementations into the Linux app. |
| External executables | `Firmware/avrdude.exe`, `Firmware/libusb0.dll`, `Autotrace/autotrace.exe`, `Driver/CH341SER.EXE`. | Task 09 must discover Linux tools or package approved alternatives; CH341 becomes guidance, not an installer. |
| WiFi/network configuration | `netsh.exe`, `iphlpapi.dll`, Ortur WiFi configurator. | Task 10 uses explicit read-only discovery and an opt-in Linux service such as NetworkManager integration. |
| Rendering | 167 source files match SharpGL/`GrblPanel3D`/`Obj3D`/OpenGL terms; the legacy OpenGL host is WinForms-specific. | Task 13B is release-required: Avalonia OpenGL host plus rotate/pan/zoom/progress parity and diagnostics. |
| Timing and sleep | `HiResTimer`, Windows timer-resolution calls, `SetThreadExecutionState`. | Tasks 02–03 use monotonic time; Task 12/15D provide best-effort sleep inhibition and bounded shutdown. |
| Multi-instance run | `SincroStart` uses a global named-event workflow. | Deferred by Task 22; Task 15D still prevents competing ownership of one serial device/endpoint. |

## Data, Privacy, And User Compatibility

| Legacy data/flow | Evidence | Required decision |
| --- | --- | --- |
| Settings | `LaserGRBL.Settings.bin` uses `BinaryFormatter` in `Settings.cs`. | Typed JSON with non-destructive import/failure behavior (Tasks 05, 18). |
| Custom buttons/hotkeys | `CustomButtons.bin`, `.zbn`, `HotKeysManager`; serializers and WinForms types are involved. | Explicit import/export table and UI-independent models (Tasks 14, 18). |
| Materials | `StandardMaterials.psh` and `PSHelper/MaterialDB.*`. | Preserve local compatibility and make network updates policy-gated (Tasks 14, 18, 21). |
| Project files | `.lps` loading/saving is routed by `GrblCore`. | Replace unsafe persistence, preserve originals and embedded assets, fail clearly (Tasks 04, 18, 22). |
| Secrets and telemetry | `DataProtector`, `UsageStats`, `Telegram`, `UrlManager`, update/material downloads. | Never import Windows DPAPI ciphertext on Linux; network paths default off or policy-gated (Tasks 18, 21). |
| Diagnostics and safety | Logging, issue detection, legal/countdown and machine abort/reset code are dispersed through core/UI. | Task 19 supplies redacted diagnostics; Task 20 and Task 15D make risky-state and shutdown behavior explicit. |

## Prioritized Blockers

### Release blockers before a runnable Linux MVP

1. **Build system:** the only solution targets .NET Framework 4.0 and fails with `MSB3644`; the test project is WindowsDesktop-only.
2. **Architecture/UI:** domain logic, dialogs, file dialogs, threading, and lifecycle code are coupled to WinForms.
3. **Serial and machine safety:** Linux transport selection, device permissions, cancellation, shutdown and exclusive device ownership are not implemented.
4. **Rendering:** required 3D preview depends on a WinForms SharpGL host; a 2D-only replacement is not feature-complete.
5. **Image compatibility:** production paths rely on unsupported modern-.NET Linux `System.Drawing` behavior.

### Required before a distributable release

1. Replace Windows-only tools/APIs and package their Linux dependencies with a tested device-access model.
2. Migrate binary persistence and Windows-only secret storage without destructive import.
3. Port reachable localization/resources and preserve semantic colour schemes.
4. Add diagnostics, safety/legal gating, privacy/update policy, CI/support matrix, reproducible packaging, and artifact integrity/license evidence.
5. Complete emulator/fake/PTY validation plus gated real-device, WiFi, audio, OpenGL, and clean-install checks.

## Recommended Execution Order

1. Tasks 01–02: establish the Linux-only SDK solution and platform boundaries.
2. Tasks 03–06: extract core, conversions, settings, paths, and resources without UI dependencies.
3. Tasks 07–10: serial, network/emulator, external tools, and WiFi services.
4. Tasks 11–14 and 13B: Avalonia shell/workflow, renderer, dialogs, and required 3D parity.
5. Tasks 15, 15A–15D: packaging/audio, CI matrix, GDI replacement, package device access, and safe lifecycle.
6. Task 16: MVP validation; Tasks 17–21: parity-completion concerns; Task 22: final parity and release decision.

## Follow-up Matrix

`docs/linux-port-plan-coverage.md` remains the required legacy-to-task checklist. Its `required`, `partial`, and `deferred` status values are release inputs, not proof of implementation. In particular, required 3D/OpenGL behavior, image backend compatibility, Linux CI/support evidence, package device access, and safe session lifecycle cannot be deferred for a complete Linux release.
