# Task 08: Network Communications

## Goal
Port network-based communication modes and emulator communication into the Linux-port architecture.

## Context
LaserGRBL supports Telnet, ESP8266 WebSocket, and emulator communication in addition to USB serial. These should work independently of WinForms and Windows-only APIs.

## Scope
- Move or adapt Telnet communication behind the core/platform communication boundary.
- Move or adapt ESP8266 WebSocket communication behind the same boundary.
- Move or adapt emulator communication for automated tests and UI workflows.
- Publish emulator activity as UI-independent events so Task 14 can provide the legacy-equivalent diagnostic console without coupling the emulator to WinForms.
- Preserve read/write semantics expected by `GrblCore`.

## Out of Scope
- Do not implement WiFi discovery/configuration; that is Task 10.
- Do not build the Avalonia UI workflow; that is Task 12.
- Do not remove legacy wrappers unless they are fully replaced.

## Implementation Requirements
- Communication implementations must be disposable and cancellation-aware.
- Blocking reads must have predictable timeout/cancellation behavior.
- Emulator must be usable in automated tests without external services.
- Logging hooks must preserve transmitted/received command visibility without hard-coding UI concerns.
- Emulator activity must retain bounded history outside of the UI thread.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Emulator connect/read/write lifecycle.
- Loopback TCP/Telnet communication.
- WebSocket message parsing or an in-process WebSocket fake.
- Timeout/cancellation behavior.
- Emulator activity event ordering and bounded-buffer behavior.

## Checkpoint Report
Create `docs/checkpoints/08-network-communications.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 08: Network communications`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Network and emulator communication build without WinForms dependencies.
- Required tests pass.
- The checkpoint exists, and the commit has been pushed.
