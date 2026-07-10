# Task 01 Checkpoint: SDK-Style Solution Skeleton

## Summary

Added a Linux-only `net8.0` solution while leaving the legacy .NET Framework/WinForms project untouched.

## Implemented Changes

- `LaserGRBL.Linux.sln` contains `LaserGRBL.Core`, `LaserGRBL.Platform`, `LaserGRBL.Avalonia`, and `LaserGRBL.Linux.Tests`.
- `LaserGRBL.Avalonia` references Core and Platform and contains a minimal Avalonia desktop shell.
- Platform references Core; Linux-runnable xUnit tests reference Core and Platform.
- `Directory.Packages.props` centrally pins Avalonia 12.0.5 and test dependencies.
- `docs/linux-port-build.md` documents Linux-only restore/build/test/run commands.

## Test Evidence

Executed on Linux Mint 22.3 with .NET SDK 8.0.422:

```text
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore LaserGRBL.Linux.sln
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build
```

The standard build succeeded with zero warnings/errors. The test run passed 2/2 tests. Sandbox-only process/IPC restrictions require the build and VSTest commands to run outside the sandbox; no source or package failure remained.

## Remaining Risks

- The Avalonia shell is intentionally only a startup skeleton; no legacy workflows are ported yet.
- Task 02 must define the platform interfaces before legacy core extraction begins.
- GUI startup requires a graphical Linux session and is not exercised by this headless checkpoint.

## Completion Status

Complete for Task 01. The next implementation task is Task 02: core/platform abstractions.
