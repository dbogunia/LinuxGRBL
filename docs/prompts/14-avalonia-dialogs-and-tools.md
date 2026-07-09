# Task 14: Avalonia Dialogs And Tools

## Goal
Port the secondary LaserGRBL screens and tools to Avalonia for feature parity.

## Context
The legacy app has many WinForms screens: settings, custom buttons, hotkeys, material editor, raster/SVG import, run from position, issue detector, logs, laser usage, firmware flashing, WiFi discovery/configuration, generators, and related option dialogs.

## Scope
Port Avalonia views/view-models for:
- Settings.
- Custom buttons.
- Hotkey manager.
- Material editor.
- Raster import and options.
- SVG import and options.
- Run from position and resume job.
- Issue detector.
- Connect/log details.
- Laser usage/lifetime.
- Firmware flash.
- WiFi discovery/configuration.
- Existing generators such as power-vs-speed/cutting/shake tests where core support exists.

## Out of Scope
- Do not redesign algorithms.
- Do not add Linux packaging.
- Do not remove legacy WinForms files unless the repo maintainers explicitly want cleanup.

## Implementation Requirements
- Each dialog/tool must have a view-model with testable save/apply/cancel behavior.
- Dialogs must use `IFileDialogService` and `IMessageService`.
- Long-running imports, firmware flashing, and WiFi operations must be async and cancellable where practical.
- Preserve existing user-facing capabilities and settings keys unless a migration is documented.
- Keep feature gaps documented in the checkpoint with exact follow-up work.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for each view-model group:
- Settings load/save/cancel.
- Custom button add/edit/delete/import/export.
- Hotkey assignment/conflict behavior.
- Material DB read/write.
- Raster/SVG import option propagation.
- Firmware flash command initiation through fake service.
- WiFi discovery/configuration through fake service.

## Checkpoint Report
Create `docs/checkpoints/14-avalonia-dialogs-and-tools.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 14: Avalonia dialogs and tools`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Listed tools/dialogs are implemented or any impossible item has a documented blocker.
- View-model tests cover the feature behavior.
- The checkpoint exists, and the commit has been pushed.
