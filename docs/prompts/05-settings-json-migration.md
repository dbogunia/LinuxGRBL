# Task 05: Settings JSON Migration

## Goal
Replace `BinaryFormatter` settings persistence with versioned JSON settings that work safely on Linux.

## Context
The legacy `Settings` class stores arbitrary objects in `LaserGRBL.Settings.bin` using `BinaryFormatter`, which is unsafe and unsuitable for a modern cross-platform app. Linux storage should use XDG-style application paths through `IAppPaths`.

## Scope
- Implement a JSON-backed settings service.
- Store settings under the platform data/config path provided by `IAppPaths`.
- Add schema version metadata.
- Add best-effort migration from readable legacy `.bin` files.
- Preserve known settings keys needed by extracted core and upcoming Avalonia UI.

## Out of Scope
- Do not port the full settings UI.
- Do not guarantee migration of unknown arbitrary serialized object graphs.
- Do not delete legacy settings files.

## Implementation Requirements
- JSON must be human-readable and versioned.
- Corrupt JSON must not crash startup; fall back to defaults and preserve the corrupt file if possible.
- Legacy import must be isolated, optional, and failure-tolerant.
- Avoid storing runtime-only objects directly; define typed settings models or typed key access.
- Include documented default values for critical settings.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Default settings creation.
- Save/load round trip.
- Schema version persistence.
- Corrupt JSON fallback.
- Legacy import success for a simple supported sample if practical.
- Legacy import failure does not crash.

## Checkpoint Report
Create `docs/checkpoints/05-settings-json-migration.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 05: Settings JSON migration`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- JSON settings service is implemented and tested.
- BinaryFormatter is not required for normal Linux-port settings operation.
- The checkpoint exists, and the commit has been pushed.
