# Task 12: Avalonia Main Workflow

## Goal
Implement the main LaserGRBL operational workflow in Avalonia.

## Context
Users need to connect to a device, select port/baud, inspect status/logs, load a G-code file, run jobs, pause/resume/reset, send manual commands, and jog. Core and platform services should already exist.

## Scope
- Implement Avalonia view-models and views for:
  - Port/baud selection.
  - Firmware type selection for GRBL, Smoothie, Marlin, and VigoWork before connection.
  - Connect/disconnect.
  - Machine status and coordinates.
  - Command log and manual command entry.
  - File loading.
  - Drag/drop, append, reopen-last-file, and supported G-code/raster/SVG/project file routing.
  - Run, hold, resume, reset, stop/abort.
  - Jog controls and feed/power overrides where core support exists.
- Bind UI to extracted core/platform services.

## Out of Scope
- Do not implement preview renderer internals; use placeholder or existing renderer interface until Task 13.
- Do not port secondary dialogs/tools; that is Task 14.
- Do not add packaging.

## Implementation Requirements
- Controls must be disabled/enabled based on connection and job state.
- User-facing errors must be routed through the message service and logs.
- Manual commands must preserve existing newline/command behavior.
- Long-running operations must not block the UI thread.
- Acquire `IExecutionInhibitor` only while a job is active and release it on every completion, abort, disconnect, and shutdown path; failure to inhibit sleep must be visible in diagnostics but must not block machine control.
- View-models must be testable with fake core/platform services.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Connect/disconnect state transitions.
- Port refresh behavior.
- Run/hold/resume/reset command availability.
- Manual command dispatch.
- File load success/failure.
- Jog command dispatch.
- Firmware selection creates the matching protocol strategy.
- File routing, append, and reopen-last-file state transitions.
- Execution-inhibitor acquire/release lifecycle and unavailable fallback.

## Checkpoint Report
Create `docs/checkpoints/12-avalonia-main-workflow.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 12: Avalonia main workflow`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Main operational workflow is implemented in Avalonia with tests.
- UI remains responsive for async operations.
- The checkpoint exists, and the commit has been pushed.
