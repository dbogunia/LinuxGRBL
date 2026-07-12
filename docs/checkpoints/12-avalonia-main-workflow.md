# Task 12 Checkpoint: Avalonia Main Workflow

## Summary

Implemented the first testable Avalonia main workflow layer for connection setup, firmware selection, command dispatch, file loading, job state, jog controls, logs, and execution-inhibitor lifecycle.

## Implemented Changes

- Added `MainWorkflowViewModel` with port refresh, baud selection, firmware selection, connect/disconnect, machine status text, manual command dispatch, G-code loading, append/reopen-last-file behavior, run/hold/resume/reset/stop, jog commands, command logs, and UI command enablement.
- Bound the main Avalonia window to workflow controls for serial port, baud rate, firmware, run controls, jog controls, manual command entry, status, coordinates, and logs.
- Wired workflow services through `AppBootstrapper`, including Linux serial ports, unavailable execution inhibitor fallback, and a logging message service for user-facing errors.
- Kept preview rendering as a placeholder with machine-coordinate display; renderer internals remain Task 13 and Task 13B.
- Added tests for connection state transitions, firmware selection, manual command dispatch, file load/failure/append/reopen routing, jog dispatch, run/hold/resume/reset state, and execution-inhibitor acquire/release/unavailable fallback.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 125 tests outside the sandbox. The sandboxed VSTest run is still blocked by local socket permission (`SocketException (13): Permission denied`).

## Remaining Risks

- Serial receive-loop integration is still shallow; Task 12 establishes the UI command/state boundary, while richer live controller synchronization remains for later workflow hardening.
- Raster, SVG, and project files are recognized by routing but still require dedicated converter/dialog tasks before they can be fully loaded.
- Preview rendering remains intentionally placeholder-only until Task 13 and Task 13B.
- Sleep inhibition uses the existing unavailable Linux implementation, but the workflow now acquires/releases it correctly around active jobs.

## Commit And Push

Implementation commit: `be55875` (`Task 12: Avalonia main workflow`)

Push: complete on `feature/12-main-workflow`

## Completion Status

Complete for Task 12 implementation and PR handoff.
