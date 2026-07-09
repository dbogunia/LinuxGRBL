# Task 16: End-To-End Linux Validation

## Goal
Validate the Linux port end to end and produce a final readiness report.

## Context
Previous tasks should have created the SDK-style projects, extracted core/platform logic, implemented Avalonia UI, ported external tools, and added packaging. This task verifies the complete Linux user workflow.

## Scope
- Run full build and test validation.
- Validate app startup.
- Validate emulator workflow.
- Validate file loading, preview, run controls, pause/resume/reset, manual command, and logs.
- Validate serial device discovery and document real-device testing if hardware is available.
- Validate assets/resources, settings persistence, sound fallback, updater behavior, firmware flash dry-run, WiFi service behavior, and package artifact.
- Produce a final Linux port readiness report.

## Out of Scope
- Do not start major feature rewrites.
- Do not hide failing validations; document them clearly.
- Do not perform real firmware flashing unless explicitly authorized and hardware is configured for it.

## Implementation Requirements
- Create `docs/linux-port-readiness.md`.
- Include exact commands, environment details, package artifact path, and pass/fail table.
- Any remaining blocker must include severity, impacted workflow, and recommended next task.
- Use the emulator for automated workflow validation where real hardware is unavailable.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`
- Avalonia app startup command for the ported app.
- Package build command from Task 15.
- Emulator workflow validation.

Manual or gated checks:
- Real USB GRBL connection if hardware is available.
- Serial permission behavior for unavailable/permission-denied devices.
- WiFi discovery/configuration if NetworkManager and hardware are available.
- Firmware flash dry-run; real flash only with explicit authorization.

## Checkpoint Report
Create `docs/checkpoints/16-end-to-end-linux-validation.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After validation and the checkpoint are complete:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 16: End-to-end Linux validation`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- `docs/linux-port-readiness.md` exists and reports final validation.
- All required automated tests pass or have concrete documented blockers.
- Manual/hardware-dependent checks are either completed or explicitly marked unavailable.
- The checkpoint exists, and the commit has been pushed.
