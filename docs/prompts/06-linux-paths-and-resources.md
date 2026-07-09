# Task 06: Linux Paths And Resources

## Goal
Normalize paths and resource lookup for Linux by removing hardcoded Windows path separators and direct executable-path assumptions.

## Context
The legacy app uses values like `Firmware\\`, `Sound\\name.wav`, `Autotrace\\autotrace.exe`, and `Application.ExecutablePath`. Linux requires platform-aware paths and resource lookup that works after packaging.

## Scope
- Route executable, data, config, cache, temp, resource, firmware, sound, material, and custom button paths through `IAppPaths`.
- Replace hardcoded `\` path joins with `Path.Combine` or resource APIs.
- Ensure bundled files can be located from development builds and packaged app layouts.
- Add tests for Linux path behavior.

## Out of Scope
- Do not implement firmware flashing behavior beyond path preparation.
- Do not implement packaging.
- Do not port UI screens.

## Implementation Requirements
- `IAppPaths` implementation for Linux must follow XDG defaults:
  - Config under `$XDG_CONFIG_HOME` or `~/.config`.
  - Data under `$XDG_DATA_HOME` or `~/.local/share`.
  - Cache/temp under appropriate cache/temp locations.
- Tests must not depend on the real user home directory; use temporary directories or environment overrides.
- Resource lookup must be deterministic and documented.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- XDG environment variable handling.
- Default fallback paths.
- Resource path generation with no Windows separators on Linux.
- Missing resource produces a clear failure result.

## Checkpoint Report
Create `docs/checkpoints/06-linux-paths-and-resources.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 06: Linux paths and resources`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Linux path service exists and is tested.
- Ported code no longer creates Windows-style paths for Linux resources.
- The checkpoint exists, and the commit has been pushed.
