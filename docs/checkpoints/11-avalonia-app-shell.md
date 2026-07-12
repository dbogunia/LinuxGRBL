# Task 11 Checkpoint: Avalonia App Shell

## Summary

Implemented the first runnable Avalonia application shell for the Linux port with startup bootstrap, typed service registration, diagnostics, basic layout placeholders, logging, localization, settings, paths, and semantic color schemes.

## Implemented Changes

- Replaced the placeholder `MainWindow` with an app shell containing connection/status, disabled run-control placeholders, diagnostics, preview placeholder, log area, paths/settings status, and selected color-scheme display.
- Added `AppBootstrapper` and `AppServices` to initialize Linux paths, settings store, process runner, serial service, WiFi service, execution inhibitor, secret store, localization, logging, diagnostics, theme catalog, and the main view model.
- Added non-fatal startup diagnostics for unavailable sleep inhibition and secure secret storage.
- Added `AppLogSink` for predictable Linux log file initialization under the XDG app log path.
- Added `LocalizationCatalog` with the initial shell strings and design-time-safe `MainWindowViewModel` construction.
- Added `ColorSchemeCatalog` with named semantic schemes, including safety-glasses-oriented variants and explicit semantic colors for preview, log, commands, links, warnings, and disabled controls.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 118 tests.

Added tests for app service registration, main view-model construction, settings/path initialization, localization bootstrap, log path initialization, design-time construction, and semantic color-scheme mapping.

## Headless Startup

A GUI startup smoke test was not added in this task because the current CI/sandbox environment does not provide a stable display server. The Avalonia project itself is compiled, XAML is validated by build, and shell bootstrap/view-model construction is covered by unit tests.

## Remaining Risks

- The shell does not yet implement real connect/run/jog workflows; Task 12 owns those controls and state transitions.
- The preview area is only a placeholder; rendering remains Task 13 and 13B.
- Secret storage and sleep inhibition are registered as unavailable services and must be replaced by Linux implementations in later tasks.

## Commit And Push

Implementation commit: `43d5c7e` (`Task 11: Avalonia app shell`)

Push: complete on `feature/11-avalonia-shell`

## Completion Status

Complete for Task 11 implementation and PR handoff.
