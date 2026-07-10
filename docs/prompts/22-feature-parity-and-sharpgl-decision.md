# Task 22: Feature Parity And SharpGL Decision

## Goal
Produce a final feature-parity assessment for the Linux port and verify required SharpGL/3D preview parity plus any intentionally omitted legacy features.

## Context
The porting tasks cover the main workflow and many secondary tools, but the legacy repository is large and includes WinForms-specific rendering, SharpGL, `GrblPanel3D`, `Obj3D`, many dialogs, resources, and support utilities. A final parity pass is needed before calling the Linux port complete. 3D/OpenGL preview parity is required; it cannot be removed or deferred for a complete Linux release.

## Scope
- Create a feature parity matrix mapping legacy WinForms screens, tools, communication modes, settings, file formats, and resources to Linux-port equivalents.
- Mark each item as implemented, intentionally omitted, replaced, blocked, or not applicable.
- Verify the required SharpGL/3D replacement or port from Task 13B, including `GrblPanel3D`, `Obj3D`, OpenGL hosting, and preview interactions.
- Verify that every intentionally omitted item has user impact, rationale, and follow-up recommendation.
- Update the final readiness report with the parity matrix and remaining gaps.

## Out of Scope
- Do not start major rewrites during this task.
- Do not implement missing features unless they are small fixes required to complete validation.
- Do not hide parity gaps behind generic "future work" wording.
- Do not accept missing 3D/OpenGL preview parity as an intentionally omitted feature.

## Implementation Requirements
- Use local repository inspection and previous checkpoints as source of truth.
- Use `docs/linux-port-plan-coverage.md` as the checklist for the parity matrix.
- The parity matrix must include at least: main window workflow; all firmware variants; preview and named color schemes; settings; custom buttons; hotkeys; material editor; G-code/raster/SVG/project-file import and export; image/font/GDI compatibility; generators; firmware flashing; WiFi; logs; safety/legal; localization; user data and secret storage; packaging, desktop/MIME integration, and device/sandbox access; updater/privacy and artifact integrity; diagnostics; emulator console; timing; sleep inhibition; CI/support matrix; shutdown/recovery; and single-device/endpoint ownership.
- SharpGL/3D status must be one of: ported to Avalonia/OpenGL, replaced by an equivalent OpenGL renderer, or release-blocking incomplete.
- 2D/Avalonia drawing alone is not sufficient for final preview parity.
- Every blocked item must include severity, impacted workflow, and recommended next task.
- Missing required 3D/OpenGL behavior must be severity `release blocker`.
- Any user-visible omission must be documented in release notes or readiness docs.
- `SincroStart` cross-process "Run Multi" behavior must be listed explicitly as deferred for the first Linux release, with its global named-event dependency, user impact, and a follow-up cross-process coordinator design. It must not be represented as implemented by ordinary run/resume/hold controls.
- A missing clean-install/device-access validation, supported image backend, or safe shutdown/resource-ownership contract is a release blocker, not generic future work.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`
- Avalonia app startup command for the ported app.
- Package build or package metadata validation from Task 15.
- 3D/OpenGL preview startup and nonblank render validation from Task 13B.

Manual or gated checks:
- Exercise each implemented parity-matrix workflow using emulator or fake services where hardware is unavailable.
- Exercise 3D preview rotate, pan, zoom, auto-fit/reset, progress, machine/cursor position, and OpenGL failure fallback.
- Validate real-device, WiFi, firmware, and audio behavior only when safe hardware/environment is available.
- Capture screenshots or logs for UI workflows where automated verification is not practical.
- Validate each firmware strategy with fake transport traces; validate project-file import/export, the emulator console, color-scheme rendering, and sleep-inhibitor fallback through fakes where host facilities are unavailable.
- Validate the support matrix against CI evidence; run conversion fixtures and lifecycle/resource-lock tests; validate package desktop/device metadata and clean-install behavior where the selected packaging environment is available.

## Checkpoint Report
Create `docs/checkpoints/22-feature-parity-and-sharpgl-decision.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/22-feature-parity`, open a pull request to protected `master`, and do not push directly to `master`. Release-only fixes after Task 16 belong on `release/linux-v<version>` and must be merged back to `master`.

After validation and documentation are complete:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 22: Feature parity and SharpGL decision`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- A feature parity matrix exists in the readiness documentation or a dedicated parity document.
- Required SharpGL/3D behavior is implemented through an Avalonia/OpenGL path or the release is marked incomplete with a release-blocking gap.
- All remaining gaps are concrete, severity-rated, and tied to follow-up work.
- The checkpoint exists, and the commit has been pushed.
