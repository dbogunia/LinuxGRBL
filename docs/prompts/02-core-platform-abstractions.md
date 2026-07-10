# Task 02: Core Platform Abstractions

## Goal
Define the platform boundary needed to extract core behavior away from WinForms and Windows APIs.

## Context
`GrblCore` currently depends on WinForms synchronization and application paths. Other modules directly use dialogs, message boxes, process launching, serial enumeration, sounds, updater behavior, firmware flashing, WiFi APIs, and Windows-specific binaries.

## Scope
Add abstractions and DTOs in the new Linux-port projects:
- `IUiDispatcher`
- `IAppPaths`
- `IProcessRunner`
- `IFileDialogService`
- `IMessageService`
- `ISerialPortService`
- `ISoundService`
- `IUpdateService`
- `IFirmwareFlashService`
- `IWifiService`
- `IMonotonicClock`
- `IExecutionInhibitor`
- `ISecretStore`
- `PortInfo`
- Result types needed for process execution, tool discovery, and user-facing failures.

## Out of Scope
- Do not migrate `GrblCore` behavior yet.
- Do not implement Avalonia UI.
- Do not replace every caller in the legacy project.

## Implementation Requirements
- Put UI-independent contracts in `LaserGRBL.Core` only when core logic must depend on them.
- Put OS-facing contracts and implementations in `LaserGRBL.Platform` when they are not pure domain contracts.
- APIs must be async-capable where operations can block: process execution, dialogs, firmware flashing, WiFi operations, updates.
- Result types must carry success/failure, user-readable message, and optional exception/detail text.
- `IMonotonicClock` must expose elapsed monotonic time suitable for streaming watchdogs and duration accounting; production code must not depend on Windows timer-resolution APIs.
- `IExecutionInhibitor` must acquire and release a best-effort system-sleep inhibitor for an active machine job, returning a nonfatal diagnostic when unavailable.
- `ISecretStore` must never expose persisted secret material through normal settings serialization and must distinguish unavailable storage from a missing secret.
- Add XML comments only where they clarify contract semantics.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests covering DTO equality/construction and simple fake implementations where useful.
- Add fake-clock, fake-inhibitor, and unavailable-secret-store tests.

## Checkpoint Report
Create `docs/checkpoints/02-core-platform-abstractions.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 02: Core platform abstractions`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- All listed interfaces/DTOs exist in appropriate projects.
- The new solution builds and tests pass.
- The checkpoint exists, and the commit has been pushed.
