# Task 18: User Data Compatibility

## Goal
Preserve or intentionally migrate LaserGRBL user data formats used by the legacy app.

## Context
The legacy app stores user-facing state in several places and formats: binary settings, custom buttons, hotkeys, material databases, standard materials, bundled presets, usage statistics, laser lifetime counters, and exported/imported files. The Linux port must avoid silent data loss and provide clear migration behavior.

## Scope
- Inventory user data formats and storage locations used by settings, custom buttons, hotkeys, materials, generators, usage/lifetime data, and import/export flows.
- Include at least `LaserGRBL.Settings.bin`, `UsageStats.bin`, `LaserLifeCounter.bin`, custom button files, hotkeys, `StandardMaterials.psh`, `PSHelper/MaterialDB.*`, GRBL config import/export files, `.lps` project files, DPAPI-protected Telegram credentials, and other serializer-backed files.
- Define a compatibility table: legacy format, new Linux-port format, import behavior, export behavior, backup behavior, and failure behavior.
- Implement migration/import for supported formats used by ported features.
- Preserve old files by default and write backups before destructive migrations.
- Add user-facing failure results for unsupported, corrupt, or partially migrated data.

## Out of Scope
- Do not guarantee migration of arbitrary unknown serialized object graphs.
- Do not delete legacy user files.
- Do not redesign the user-facing data model unless required for Linux compatibility.

## Implementation Requirements
- Migrations must be idempotent where practical.
- Import failures must not prevent app startup unless the affected feature cannot safely run.
- New Linux data files must use platform paths from `IAppPaths`.
- File formats should be documented enough for manual troubleshooting.
- Serializer-backed files that use binary serialization, encryption, compression, or password prompts must have an explicit support decision: migrate, import-only, skip with message, or unsupported with rationale.
- Legacy DPAPI Telegram ciphertext is not portable: preserve the source file, do not import the token, and require the user to re-enter it only after explicitly enabling Telegram notifications. New tokens must use `ISecretStore`, never JSON settings.
- Round-trip compatibility must be tested for every supported import/export format.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Settings migration from a supported legacy sample.
- Custom button import/export, including invalid file handling.
- Hotkey persistence and conflict data migration.
- Material DB and standard material import.
- Usage statistics and laser lifetime counter migration or explicit skip behavior.
- GRBL config import/export round trip.
- Serializer-backed invalid, encrypted, or password-required files fail with clear messages.
- `.lps` project import/export, including embedded-image data and invalid binary input.
- DPAPI credential migration records a re-entry-required result without exposing token material.
- Backup creation before migration.
- Idempotent re-run of migrations.

## Checkpoint Report
Create `docs/checkpoints/18-user-data-compatibility.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/18-user-data-compatibility`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 18: User data compatibility`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Supported legacy user data formats are listed with explicit migration/import behavior.
- Implemented migrations are tested with valid and invalid samples.
- Unsupported formats fail with clear user-facing messages and do not destroy data.
- The checkpoint exists, and the commit has been pushed.
