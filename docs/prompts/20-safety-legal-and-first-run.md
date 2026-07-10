# Task 20: Safety, Legal, And First Run

## Goal
Port LaserGRBL safety, legal, warning, and first-run behavior so Linux users cannot accidentally bypass important machine-control safeguards.

## Context
The legacy app includes safety countdowns, legal disclaimers, issue detection, laser usage/lifetime flows, and warning dialogs. These are not just secondary UI: they affect when the app should allow machine actions and how risks are communicated.

## Scope
- Inventory safety and legal workflows from `SafetyCountdown`, `LegalDisclaimer`, issue detector, firmware warnings, laser usage/lifetime warnings, and any first-run prompts.
- Implement Avalonia view-models and services for required safety/legal acknowledgements.
- Define which acknowledgements are one-time, per-version, per-session, or per-operation.
- Gate risky operations such as starting a job, firmware flashing, reset/abort, and laser test commands where the legacy behavior requires it.
- Persist acknowledgements through the Linux settings service when appropriate.

## Out of Scope
- Do not add new legal terms beyond existing project text unless maintainers provide them.
- Do not weaken existing safety checks without documenting and approving the change.
- Do not perform real firmware flashing or machine motion in automated tests.

## Implementation Requirements
- Risky actions must have explicit view-model state transitions that can be tested without UI automation.
- First-run and legal flows must not loop indefinitely if settings are corrupt; they should fail closed for risky operations.
- User-facing safety messages must use the localization service when available.
- Any removed or changed warning must be listed in the checkpoint with the reason.
- Hardware-independent tests must verify that blocked actions remain blocked until required acknowledgement is present.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- First-run acknowledgement persistence.
- Safety countdown completion and cancellation.
- Job start blocked before required acknowledgement.
- Firmware flash initiation blocked before required acknowledgement.
- Corrupt settings fail closed for risky operations.
- Localized safety/legal message lookup if localization exists.

## Checkpoint Report
Create `docs/checkpoints/20-safety-legal-and-first-run.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 20: Safety legal and first run`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Required safety/legal flows are implemented or explicitly documented as intentionally changed.
- Risky actions are blocked until required acknowledgements are present.
- Tests cover safety state transitions without real hardware.
- The checkpoint exists, and the commit has been pushed.
