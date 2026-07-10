# Task 19: Logging, Diagnostics, And Support Bundle

## Goal
Provide Linux-ready logging, communication diagnostics, and an exportable support bundle for troubleshooting.

## Context
The legacy app contains application logs, communication logs, connect logs, and diagnostic screens tied to WinForms and legacy paths. Linux users need logs stored in predictable XDG locations and an easy way to gather evidence without exposing unnecessary private data.

## Scope
- Implement or complete a UI-independent logging service for application, session, communication, and connection diagnostics.
- Route logs through `IAppPaths` using Linux-appropriate data/cache paths.
- Add log retention, rotation, or size limits suitable for repeated machine-control sessions.
- Implement support bundle export containing app version, environment, settings summary, recent logs, device discovery results, and package metadata where available.
- Redact or omit sensitive values such as WiFi passwords, access tokens, full home paths where avoidable, and raw user file contents unless explicitly selected.

## Out of Scope
- Do not upload logs automatically.
- Do not add remote support or telemetry.
- Do not require a connected GRBL device for normal diagnostics tests.

## Implementation Requirements
- Logging must not crash the app if the log directory is unavailable or full.
- Communication logs must preserve transmitted/received command visibility needed for support.
- Support bundle creation must report included and skipped files.
- Redaction rules must be centralized and tested.
- UI integrations should use view-models and services rather than direct file writes.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Log path creation using fake `IAppPaths`.
- App/session/communication log writes.
- Log write failure fallback.
- Retention or rotation behavior.
- Support bundle contents and skipped-file reporting.
- Redaction of sensitive settings and WiFi values.

## Checkpoint Report
Create `docs/checkpoints/19-logging-diagnostics-and-support-bundle.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 19: Logging diagnostics and support bundle`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Logs are written through Linux-port services to documented paths.
- A support bundle can be created without hardware.
- Sensitive values are redacted or omitted by default.
- The checkpoint exists, and the commit has been pushed.
