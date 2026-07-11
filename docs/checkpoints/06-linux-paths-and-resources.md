# Task 06 Checkpoint: Linux Paths And Resources

## Summary

Implemented deterministic Linux XDG paths and bundled-resource lookup without Windows path assumptions.

## Implemented Changes

- `LinuxAppPaths` implements `IAppPaths` with XDG config/data/cache defaults, log and temporary directories.
- Constructor supports environment/home/temp injection for deterministic tests.
- `ResourceLocator` uses `Path.Combine` and returns a user-readable missing-resource result.

## Test Evidence

`dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors. `dotnet test LaserGRBL.Linux.sln --no-build` passed 92 tests.

## Remaining Risks

- Package-specific resource-root selection is validated in Task 15C.
- Firmware/sound/tool consumers are moved to these paths by their dedicated implementation tasks.

## Completion Status

Complete for Task 06.
