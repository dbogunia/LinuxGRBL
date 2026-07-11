# Task 05 Checkpoint: Settings JSON Migration

## Summary

Implemented versioned, human-readable JSON settings for the Linux port. Normal operation does not use `BinaryFormatter`.

## Implemented Changes

- Typed `PortSettings` with schema version, firmware, streaming mode, color scheme, and recent-file state.
- `JsonSettingsStore` uses `IAppPaths`, atomic temporary-file writes, and indented JSON.
- Missing/corrupt/unknown-version data falls back safely to documented defaults; corrupt JSON receives a timestamped backup copy.
- `LegacySettingsImportService` isolates legacy `.bin` detection and preserves the source without deserializing arbitrary BinaryFormatter data. Task 18 owns any supported compatibility import decision.

## Test Evidence

`dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed without warnings/errors. `dotnet test LaserGRBL.Linux.sln --no-build` passed 89 tests.

## Remaining Risks

- XDG path implementation is Task 06.
- Legacy binary content is intentionally not imported by the Linux runtime; Task 18 provides the compatibility table and any gated migration tooling.
- Secure credentials remain excluded from settings and are handled by Tasks 18/21.

## Completion Status

Complete for Task 05.
