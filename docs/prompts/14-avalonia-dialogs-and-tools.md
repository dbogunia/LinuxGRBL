# Task 14: Avalonia Dialogs And Tools

## Goal
Port the secondary LaserGRBL screens and tools to Avalonia for feature parity.

## Context
The legacy app has many WinForms screens: settings, custom buttons, hotkeys, material editor, raster/SVG import, run from position, issue detector, logs, laser usage, firmware flashing, WiFi discovery/configuration, generators, and related option dialogs.

This task implements the Avalonia UI and view-models for these tools. Final feature parity, localization completeness, user data compatibility, diagnostics, safety/legal behavior, and privacy policy are validated in Tasks 17-22. Required 3D/OpenGL preview parity is implemented in Task 13B and validated again in Task 22.

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
- Ortur WiFi configuration and prompt flows.
- Existing generators such as power-vs-speed/cutting/shake tests where core support exists.
- GRBL configuration import/export and CSV-backed error/alarm/settings code views where exposed by the legacy UI.
- LaserGRBL project (`.lps`) save/open flows, including embedded-image projects and user-visible migration errors.
- A read-only emulator activity console for the in-process GRBL and WebSocket emulators.
- Splash, license, save-option, laser-selector, and input-box replacement flows where still needed by ported workflows.

## Out of Scope
- Do not redesign algorithms.
- Do not add Linux packaging.
- Do not treat this task as the final parity audit; Task 22 owns the complete WinForms-to-Avalonia parity matrix.
- Do not remove legacy WinForms files unless the repo maintainers explicitly want cleanup.

## Implementation Requirements
- Each dialog/tool must have a view-model with testable save/apply/cancel behavior.
- Dialogs must use `IFileDialogService` and `IMessageService`.
- Long-running imports, firmware flashing, and WiFi operations must be async and cancellable where practical.
- Preserve existing user-facing capabilities and settings keys unless a migration is documented.
- The emulator console must subscribe to the UI-independent activity stream from Task 08 and retain a bounded diagnostic history.
- Keep feature gaps documented in the checkpoint with exact follow-up work.
- Link any deferred localization, user data migration, logging, safety/legal, or privacy work to Tasks 17-22.
- Do not defer required 3D/OpenGL preview work from Task 13B as a dialog/tool gap.

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
- Ortur WiFi configuration command/result handling through fakes.
- Generator option propagation for power-vs-speed, cutting, and shake tests.
- GRBL config import/export success and invalid file handling.
- Project save/open success, legacy project import failure, and embedded-image error handling.
- Emulator activity console update and bounded-history behavior.

## Checkpoint Report
Create `docs/checkpoints/14-avalonia-dialogs-and-tools.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/14-dialogs-tools`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 14: Avalonia dialogs and tools`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Listed tools/dialogs are implemented or any impossible item has a documented blocker.
- Any feature not fully ported has a concrete follow-up reference, especially for Tasks 17-22.
- View-model tests cover the feature behavior.
- The checkpoint exists, and the commit has been pushed.
