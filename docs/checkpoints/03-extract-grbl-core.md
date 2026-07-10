# Task 03 Checkpoint: Extract GRBL Core

## Summary

Extracted the Linux-port protocol/state/streaming foundation into `LaserGRBL.Core` without a WinForms reference. The legacy `GrblCore` remains unchanged as behavioral reference.

## Implemented Changes

- Firmware-neutral `MachineSession` with asynchronous transport receive loop, lifecycle, connection timeout, unexpected disconnect, alarm, abort, state/issue/position/override/configuration events, and UI-dispatcher boundary.
- GRBL 0.9/1.0c/1.1 status parsing, welcome/version detection for GRBL, GRBLHAL, Vigo, SimpleLaser, and Longer, plus GRBL configuration parsing.
- Firmware strategies for GRBL, Smoothie, Marlin, and Vigo; non-GRBL firmware does not silently adopt GRBL true-jog/realtime semantics.
- Buffered, synchronous, and repeat-on-error command streaming with byte-window accounting, monotonic watchdog, job lifecycle, bounded retry, and fake transport coverage.
- Legacy-equivalent jog, continuous-jog cancellation, realtime override, hold/resume/reset/query, alarm, and `M5` abort behavior.

## Test Evidence

```text
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build
```

Build completed with zero warnings/errors; 64 tests passed. Tests run outside the filesystem sandbox because VSTest requires a local IPC socket.

## Deliberate Follow-up Boundaries

- Task 04 owns G-code file parsing, conversion/job metadata and project-file workflow.
- Tasks 07–08 implement concrete serial, Telnet, WebSocket, and emulator transports for `IMachineTransport`; Task 03 provides the UI-independent contract and scripted transport lifecycle tests.
- Tasks 12 and 14 bind session events to Avalonia workflows and dialogs.
- Task 15D owns final shutdown/resource-ownership hardening; Task 03 supplies the session-level abort/disconnect primitives.

## Completion Status

Complete for the Core extraction foundation. Remaining transport/UI integrations are assigned to their dedicated tasks rather than retained as WinForms dependencies.
