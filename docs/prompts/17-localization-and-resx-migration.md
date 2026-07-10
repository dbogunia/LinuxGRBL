# Task 17: Localization And Resx Migration

## Goal
Migrate legacy WinForms localization and resource assumptions into the Avalonia Linux port with explicit fallback behavior and coverage checks.

## Context
The legacy app uses `Strings.*.resx` plus many form-specific `.resx` files containing localized strings and WinForms resource metadata. Avalonia needs a UI-independent localization service and a clear strategy for text, icons, images, and other non-text resources.

## Scope
- Inventory `Strings.*.resx` and form-specific `.resx` files used by ported workflows.
- Implement or update the Avalonia localization service to resolve translated strings by culture with deterministic fallback.
- Migrate user-visible strings needed by the Avalonia shell, main workflow, dialogs, tools, safety flows, and packaging messages.
- Define how non-text `.resx` resources such as images, icons, sizes, and serialized WinForms values are handled.
- Add language selection and persistence if the settings service already exists.

## Out of Scope
- Do not perform manual translation work beyond preserving existing translations.
- Do not port unused WinForms layout metadata.
- Do not block the Linux MVP on translations for screens that are not yet ported.

## Implementation Requirements
- Keep localization lookup independent of WinForms and `System.Windows.Forms`.
- Missing keys must fall back to the invariant or English resource and must be detectable in tests.
- Culture selection must be stable across app restarts when settings are available.
- Document every resource category that is intentionally not migrated.
- Avoid duplicating translated text in view-models when a localization service can provide it.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Culture fallback order.
- Missing key detection/reporting.
- At least one translated string lookup for each supported culture included in the migrated resources.
- Persisted language selection if implemented.
- Non-text `.resx` resources are either migrated to asset lookup or reported as intentionally skipped.

## Checkpoint Report
Create `docs/checkpoints/17-localization-and-resx-migration.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 17: Localization and resx migration`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Avalonia-visible workflows use the Linux-port localization service for migrated strings.
- Missing translations have deterministic fallback and test coverage.
- Non-text resource handling is documented.
- The checkpoint exists, and the commit has been pushed.
