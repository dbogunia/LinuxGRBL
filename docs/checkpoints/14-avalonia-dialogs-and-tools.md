# Task 14 Checkpoint: Avalonia Dialogs And Tools

## Summary

Added the first Avalonia dialog/tool hub layer for secondary LaserGRBL workflows. The implementation introduces testable view-models for the major Task 14 tool groups, wires the hub into the app shell, and keeps deeper compatibility, localization, safety, and final parity work linked to Tasks 17-22.

## Implemented Changes

- Added `DialogToolsViewModel` as the secondary tools hub exposed from `MainWindowViewModel`.
- Added view-models for settings, custom buttons, hotkeys, material profiles, raster/SVG import options, firmware flashing, WiFi discovery/configuration, GRBL configuration import, LaserGRBL project open/save flow, and emulator activity console.
- Wired existing platform contracts into the hub: `JsonSettingsStore`, `IFileDialogService`, `IMessageService`, `IWifiService`, `IFirmwareFlashService`, and `EmulatorMachineTransport` activity events.
- Added `UnavailableFileDialogService` so app startup remains non-fatal until native Avalonia file dialogs are wired.
- Added a visible Tools section to the Avalonia shell showing each tool group and status.
- Extended app bootstrap and shell design-time construction to include dialog/tool services.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 153 tests outside the sandbox. The sandboxed VSTest runner is still blocked by local socket permission.

Added tests for:

- Settings load/save/cancel and draft updates.
- Custom button add/edit/delete/import/export routing.
- Hotkey assignment conflict detection.
- Material profile upsert and value clamping.
- Raster/SVG import option propagation and file-dialog routing.
- Firmware file selection and flash request initiation through a fake service.
- WiFi discovery/configuration through a fake service.
- GRBL configuration text import success and invalid-line failure.
- Project open/save flow with explicit Task 18 compatibility deferral.
- Emulator activity console bounded history behavior.
- Shell/bootstrap exposure of the tools hub.

## Manual Validation

No manual GUI validation was performed in this headless run. The Avalonia shell XAML compiles and view-model behavior is covered by tests, but clicking through native dialogs and long-running tool workflows still requires a graphical session.

Task 13B manual Linux GPU/display validation is explicitly not verified yet and remains a release-blocking validation item for Task 16/22.

## Remaining Risks

- Native file dialogs are represented by `UnavailableFileDialogService`; wiring real Avalonia storage provider dialogs remains required before interactive use.
- The view-models cover the workflow contracts and user-visible status paths, but full legacy data compatibility for custom buttons, `.lps` projects, material DB files, and hotkeys is deferred to Task 18.
- Localization completeness is deferred to Task 17.
- Logging/diagnostics/support bundle depth is deferred to Task 19.
- Firmware flashing, WiFi changes, run-from-position/resume safety gates, legal warnings, and first-run flows require Task 20 validation before release.
- Privacy/update policy and optional network behavior remain Task 21.
- Full WinForms parity, including final 3D/OpenGL behavior comparison, remains Task 22.

## Commit And Push

Implementation branch: `feature/14-dialogs-tools`

Implementation commit: `d12c080` (`Task 14: Avalonia dialogs and tools`)

Push: pending.

## Completion Status

Task 14 first-pass Avalonia dialog/tool hub is implemented and tested. Remaining gaps are documented as follow-up validation and compatibility work for Tasks 17-22 rather than hidden inside this checkpoint.
