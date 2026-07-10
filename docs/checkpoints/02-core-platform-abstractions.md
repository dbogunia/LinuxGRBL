# Task 02 Checkpoint: Core Platform Abstractions

## Summary

Defined the initial Linux-port boundary without migrating legacy `GrblCore` behavior or introducing Avalonia workflow code.

## Implemented Changes

- Core contracts: UI dispatcher, file dialogs, messages, monotonic clock, execution inhibitor, secret store, and structured operation/secret results.
- Platform contracts: app paths, process runner, serial enumeration, sound, updates, firmware flashing, and WiFi discovery.
- Minimal production-safe implementations: `StopwatchMonotonicClock`, a nonfatal unavailable sleep-inhibitor fallback, and an unavailable secret-store fallback.
- Contract tests include result diagnostics, a deterministic fake clock, unavailable inhibitor behavior, and unavailable-secret-store semantics.

## Test Evidence

```text
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build
```

The build completed with zero warnings/errors. All 6 tests passed. VSTest was run outside the filesystem sandbox because its local socket IPC is blocked inside it.

## Remaining Risks

- The contracts are not yet wired into legacy streaming, settings, or UI code; Task 03 starts that extraction.
- Serial, process, WiFi, sound, update, firmware, sleep-inhibition, and secure-store implementations remain deliberately unimplemented until their dedicated tasks.
- No secret value is serialized by these contracts, but a production secure-store backend remains required.

## Completion Status

Complete for Task 02. The next implementation task is Task 03: extract GRBL core behavior behind these abstractions.
