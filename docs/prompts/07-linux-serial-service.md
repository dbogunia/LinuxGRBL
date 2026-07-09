# Task 07: Linux Serial Service

## Goal
Implement Linux-capable serial port enumeration and communication for GRBL devices.

## Context
The legacy app uses `System.IO.Ports.SerialPort`, custom serial wrappers, and a bundled RJCP serial stack that expects `libnserial.so.1` on Unix. Linux support needs a maintained and explicitly packaged serial implementation.

## Scope
- Implement `ISerialPortService` for Linux.
- Enumerate serial devices such as `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/serial/by-id/*`, and library-reported ports.
- Open/read/write/close ports with baud rate, data bits, parity, stop bits, DTR/RTS, newline, timeout, and buffer discard behavior.
- Preserve behavior required by GRBL connect/reset flows.

## Out of Scope
- Do not implement Avalonia connection UI.
- Do not require a real device for normal CI/unit tests.
- Do not silently depend on unpackaged native libraries.

## Implementation Requirements
- Prefer a maintained NuGet package with Linux support, or document and package any required native dependency explicitly.
- Permission-denied errors must tell the user which device failed and mention Linux serial group/udev permission remediation.
- Device descriptions should use `/dev/serial/by-id` or available metadata where practical.
- Provide fake/in-memory serial implementations for tests.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Port enumeration parsing/filtering.
- Open failure for missing device.
- Permission failure message formatting.
- Read/write using fake serial service.
- DTR/RTS option propagation.

If a real GRBL device is available, run a manual integration test and document it. If not, document that real-device validation is pending.

## Checkpoint Report
Create `docs/checkpoints/07-linux-serial-service.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 07: Linux serial service`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Linux serial service is implemented and tested with fakes.
- Native dependency strategy is explicit.
- The checkpoint exists, and the commit has been pushed.
