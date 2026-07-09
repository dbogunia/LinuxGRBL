# Task 11: Avalonia App Shell

## Goal
Create the runnable Avalonia application shell for the Linux port.

## Context
The port now has core/platform projects. The WinForms `Program.Main` and `MainForm` cannot be reused directly. Avalonia must provide app startup, main window, dependency injection, lifetime management, theme, and localization bootstrap.

## Scope
- Implement `LaserGRBL.Avalonia` startup.
- Add a main window with basic layout placeholders for connection/status/log/preview areas.
- Wire dependency injection for core/platform services.
- Initialize logging, settings, app paths, localization, and theme services.
- Ensure app can start on Linux.

## Out of Scope
- Do not implement full main workflow controls; that is Task 12.
- Do not implement preview rendering; that is Task 13.
- Do not port dialogs/tools; that is Task 14.

## Implementation Requirements
- Use Avalonia MVVM patterns consistent across future tasks.
- Avoid WinForms references in the Avalonia project.
- Keep startup failure messages visible and logged.
- Include design-time safe view-model construction if used.
- Keep the first screen as the actual app shell, not a marketing/landing page.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests where feasible for:
- App service registration.
- Main view-model construction.
- Settings/path initialization.

If a headless Avalonia startup smoke test is feasible in the repo, add it; otherwise document why it is not feasible.

## Checkpoint Report
Create `docs/checkpoints/11-avalonia-app-shell.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 11: Avalonia app shell`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Avalonia app builds and can start as a shell on Linux.
- DI/bootstrap code is tested where practical.
- The checkpoint exists, and the commit has been pushed.
