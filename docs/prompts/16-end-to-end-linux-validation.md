# Task 16: End-To-End Linux MVP Validation

## Goal
Validate the Linux port MVP end to end and produce a readiness report for the core Linux user workflow.

## Context
Previous tasks should have created the SDK-style projects, extracted core/platform logic, implemented Avalonia UI, ported external tools, implemented required 3D/OpenGL preview behavior from Task 13B, added packaging, and completed Tasks 15A-15D for CI/support, image compatibility, package device access, and safe session lifecycle. This task verifies the core Linux user workflow before the parity-completion tasks. Localization, user data compatibility, diagnostics, safety/legal behavior, privacy policy, and final feature parity are completed in Tasks 17-22.

## Scope
- Run full build and test validation.
- Validate app startup.
- Validate emulator workflow.
- Validate file loading, 2D/3D preview paths, run controls, pause/resume/reset, manual command, and logs.
- Validate serial device discovery and document real-device testing if hardware is available.
- Validate assets/resources, settings persistence, sound fallback, updater behavior, firmware flash dry-run, WiFi service behavior, and package artifact.
- Validate the supported-environment claim, package-specific serial permission path, image conversion fixtures, shutdown/recovery lifecycle, and single-device ownership.
- Produce an MVP Linux port readiness report.
- List remaining parity-completion work that must be handled by Tasks 17-22.

## Out of Scope
- Do not start major feature rewrites.
- Do not hide failing validations; document them clearly.
- Do not perform real firmware flashing unless explicitly authorized and hardware is configured for it.

## Implementation Requirements
- Create `docs/linux-port-readiness.md`.
- Include exact commands, environment details, package artifact path, and pass/fail table.
- Any remaining blocker must include severity, impacted workflow, and recommended next task, including references to Tasks 17-22 where applicable.
- Use the emulator for automated workflow validation where real hardware is unavailable.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`
- Avalonia app startup command for the ported app.
- Package build command from Task 15.
- Emulator workflow validation.
- 3D/OpenGL preview startup and nonblank render validation from Task 13B.
- CI/support-matrix and package metadata validation from Tasks 15A and 15C.
- Image conversion fixture suite from Task 15B.
- Lifecycle and resource-ownership tests from Task 15D.

Manual or gated checks:
- Real USB GRBL connection if hardware is available.
- Serial permission behavior for unavailable/permission-denied devices.
- WiFi discovery/configuration if NetworkManager and hardware are available.
- Firmware flash dry-run; real flash only with explicit authorization.
- 3D preview rotate, pan, zoom, auto-fit/reset, progress, machine/cursor position, and OpenGL failure fallback.
- Clean-install package smoke test and PTY serial test if the selected package/CI environment supports them.

## Checkpoint Report
Create `docs/checkpoints/16-end-to-end-linux-validation.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/16-mvp-validation`; do not push directly to `master`. After Task 16, create a `release/linux-v<version>` branch only for release-blocking fixes, validation, packaging, and documentation.

After validation and the checkpoint are complete:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 16: End-to-end Linux validation`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- `docs/linux-port-readiness.md` exists and reports MVP validation.
- The readiness report clearly separates MVP validation from final parity validation in Task 22.
- Required 3D/OpenGL preview validation is included; missing 3D behavior is recorded as a release blocker rather than generic future work.
- Missing CI support evidence, supported image backend, package device-access validation, or safe shutdown/resource-ownership evidence is recorded as a release blocker.
- All required automated tests pass or have concrete documented blockers.
- Manual/hardware-dependent checks are either completed or explicitly marked unavailable.
- The checkpoint exists, and the commit has been pushed.
