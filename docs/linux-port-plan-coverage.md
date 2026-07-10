# Linux Port Plan Coverage

This document is the required coverage checklist for the Linux port prompts. Task 00 must refresh it from local repository inspection, and Task 22 must use it as the final parity checklist.

## Coverage Status Values

- `covered`: explicitly handled by one or more prompts.
- `partial`: mentioned but needs additional inventory or implementation detail.
- `required`: mandatory for final Linux-port completeness.
- `missing`: not yet covered by the prompt sequence.
- `deferred`: intentionally postponed with a documented follow-up.

## Build And Architecture

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| `.NETFramework v4.0` WinForms project | Tasks 00-01 | covered | Keep legacy project as reference and create SDK-style Linux solution. |
| `netcoreapp3.1` WindowsForms test project | Tasks 00-01 | covered | Move runnable tests to Linux-port test project. |
| Platform abstractions | Task 02 | partial | Add UI, paths, process, serial, sound, update, firmware, WiFi, dialogs, messages, monotonic timing, sleep inhibition, and secure secret storage. |
| Linux CI, support matrix, architectures, display stacks | Task 15A | required | Build/test from a clean Linux runner; document supported x64/arm64, Wayland/X11, .NET, and graphics baseline. |

## Core Workflows

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| GRBL core, streaming, state, overrides | Task 03 | partial | Must remove WinForms sync and UI callbacks while preserving firmware-specific behavior. |
| Firmware variants: GRBL, Smoothie, Marlin, VigoWork | Tasks 03, 05, 12, 22 | partial | Preserve selection, protocol strategies, state parsing, and firmware-specific command behavior. |
| G-code parsing and job metadata | Tasks 03-04 | partial | Must be UI-independent and retain supported file routing plus append behavior. |
| Raster/SVG conversion, dithering, Hershey, Clipper, potrace/autotrace | Tasks 04, 09, 14 | covered | External autotrace stays behind process service. |
| Main connect/run/jog/manual-command workflow | Tasks 07-08, 12 | partial | Serial/network/emulator services feed Avalonia UI; file drag/drop, append, reopen, and sleep inhibition are required. |
| Cross-process synchronized start (`SincroStart`) | Task 22 | deferred | Explicitly unsupported for the first Linux release; document global named-event dependency, user impact, and follow-up coordinator design. |
| Session shutdown, recovery, and single-device/endpoint ownership | Task 15D | required | Bounded best-effort safety sequence, fail-closed resume, PTY coverage, and no competing controller access. |

## Required 3D And Rendering

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| 2D preview and render model | Task 13 | covered | May be an interim/fallback renderer. |
| SharpGL, `GrblPanel3D`, `Obj3D` | Task 13B | required | Required for final Linux-port completeness. |
| Avalonia OpenGL host | Task 13B | required | Must not depend on WinForms controls. |
| 3D rotate/pan/zoom/auto-fit/progress/machine position | Task 13B | required | Task 22 validates parity. |
| OpenGL initialization failure diagnostics | Task 13B | required | App remains usable where possible; missing 3D is release-blocking. |
| Named color schemes and semantic preview colors | Tasks 05, 11, 13, 13B, 22 | partial | Preserve legacy scheme selection and preview/log/command semantic colors; generic light/dark is insufficient. |
| `System.Drawing`/GDI+ image, SVG, font, and codec dependencies | Task 15B | required | Ported Linux paths need a supported image backend and fixture-based compatibility evidence. |

## Platform And Native Dependencies

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| Serial ports and permissions | Tasks 07, 15C | covered | Include `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/serial/by-id/*`, permission messaging, and package/sandbox-specific access validation. |
| Telnet, ESP8266 WebSocket, emulator | Task 08 | covered | Must be cancellable and testable. |
| `avrdude.exe`, `autotrace.exe`, CH341 installer | Task 09 | covered | Linux uses system tools or explicit packaged dependencies. |
| `netsh.exe`, `iphlpapi.dll`, WiFi APIs | Task 10 | covered | Prefer .NET APIs and `nmcli` with non-destructive tests. |
| Linux paths and bundled resources | Task 06 | covered | XDG paths and deterministic packaged resource lookup. |
| Package format, desktop/MIME integration, udev/sandbox access | Task 15C | required | Select supported artifacts and validate serial/device, external-tool, desktop, and file-open behavior in a clean install. |
| Monotonic streaming timing and timer-resolution replacement | Tasks 02, 03, 22 | partial | Replace `HiResTimer` and Windows timer-resolution APIs with testable monotonic timing. |
| Prevent system sleep during active jobs | Tasks 02, 12, 22 | partial | Acquire/release a best-effort Linux inhibitor; unavailability is diagnostic, not a machine-control failure. |

## Avalonia UI And Tools

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| App shell and DI/bootstrap | Task 11 | covered | Includes logging/settings/paths/localization/theme bootstrap. |
| Main window workflow | Task 12 | covered | Includes status, logs, file loading, run controls, jog. |
| Settings, custom buttons, hotkeys | Tasks 14, 18 | covered | UI plus compatibility/migration. |
| Material editor and Material DB | Tasks 14, 18, 21 | covered | Includes local data and optional network update policy. |
| Raster/SVG option dialogs | Tasks 04, 14 | covered | Algorithms stay outside UI. |
| Run-from-position and resume job | Tasks 03, 14, 20 | covered | Safety gating must be preserved. |
| Issue detector, connect/log details | Tasks 14, 19, 20 | covered | Diagnostics and safety behavior validated later. |
| Laser usage/lifetime and laser selector | Tasks 14, 18, 21 | covered | Includes `LaserLifeCounter.bin` and laser statistics policy. |
| Firmware flash UI | Tasks 09, 14, 20 | covered | Real flashing is gated. |
| WiFi discovery and Ortur configuration | Tasks 10, 14 | covered | Network-changing operations explicit. |
| Generators: power-vs-speed, cutting, shake | Tasks 04, 14 | covered | Option propagation and generated G-code tested. |
| LaserGRBL project files (`.lps`) | Tasks 04, 14, 18, 22 | partial | Replace unsafe binary persistence, preserve originals, and handle embedded images and migration failures. |
| Emulator activity console | Tasks 08, 14, 22 | partial | Retain headless emulators and provide a bounded read-only Avalonia diagnostic console. |
| Splash, license, save-option, input-box replacements | Tasks 14, 17, 20 | partial | Required only where still used by ported workflows. |

## Data, Resources, Privacy, And Validation

| Legacy area | Port task coverage | Status | Notes |
| --- | --- | --- | --- |
| Binary settings and typed JSON settings | Tasks 05, 18 | covered | Legacy import best-effort, no silent data loss. |
| Legacy DPAPI credentials and new Telegram secret storage | Tasks 02, 18, 21 | partial | Do not import Windows-only ciphertext; require explicit re-entry and use a secure store for newly entered tokens. |
| `UsageStats.bin` and serializer-backed data | Tasks 18, 21 | covered | Migration or explicit skip behavior plus privacy policy. |
| Custom buttons, hotkeys, GRBL config files | Tasks 14, 18 | covered | Round-trip behavior tested where supported. |
| `StandardMaterials.psh`, `PSHelper/MaterialDB.*` | Tasks 14, 18, 21 | covered | Local compatibility and network update policy. |
| Localization and `.resx` resources | Task 17 | covered | Text migration, fallback, non-text resource decision. |
| Logging, diagnostics, support bundle | Task 19 | covered | Includes redaction and predictable Linux paths. |
| Safety, legal, first-run, countdowns | Task 20 | covered | Risky operations fail closed until acknowledged. |
| Update checks, Material DB download, `UsageStats`, Telegram, laser stats | Task 21 | covered | Telemetry off by default; network calls policy-gated. |
| Linux packaging and sound | Task 15 | covered | Technical mechanisms; privacy policy remains Task 21. |
| Dependency provenance, licenses, and update artifact integrity | Tasks 15A, 15, 21 | covered | Pin and audit shipped dependencies; document third-party notices and integrity/signature policy before release. |
| End-to-end MVP readiness | Task 16 | covered | MVP validation before final parity. |
| Final feature parity | Task 22 | covered | Must validate this matrix and required 3D/OpenGL parity. |
