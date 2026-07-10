# Task 21: Telemetry, Update, And Privacy Policy

## Goal
Make all network calls, telemetry-like behavior, and update checks explicit, privacy-preserving, and testable in the Linux port.

## Context
The legacy app includes update checks, Material DB downloads, usage statistics, laser statistics upload, Telegram notifications, and external URL/file opening. The Linux port must not silently send telemetry and should keep update checks non-blocking, cancellable, and user-controlled.

## Scope
- Inventory update, telemetry, usage statistics, laser statistics, Telegram notification, Material DB update/download, and browser/file-opening code paths.
- Define a privacy policy for the Linux port: default behavior, opt-in/opt-out settings, stored preferences, and user-visible wording.
- Implement update checks as a cancellable service that can be disabled and does not block startup.
- Disable or replace telemetry-like behavior unless the user explicitly opts in.
- Route external URL opening through a platform service with safe error handling.
- Add a policy layer that gates all outbound network calls before they reach HTTP or platform URL services.
- Store a newly entered Telegram token through `ISecretStore`; if no secure store is available, keep Telegram disabled and explain that a token cannot be persisted securely.

## Out of Scope
- Do not implement a privileged auto-updater.
- Do not upload diagnostics or support bundles automatically.
- Do not add new telemetry endpoints.

## Implementation Requirements
- Default telemetry setting must be off unless maintainers explicitly decide otherwise and document it.
- `UsageStats`, laser statistics upload, and Telegram notifications must be off by default.
- Material DB and update checks must be separately controllable from telemetry.
- Update checks must have timeout, cancellation, and failure results.
- If the application offers downloadable releases, treat metadata and artifact integrity as separate concerns: require HTTPS, record the expected artifact version and checksum/signature policy, and never execute a downloaded updater.
- Tests must prove no telemetry request is made when telemetry is disabled.
- Tests must prove no `UsageStats`, laser statistics, Telegram, Material DB update, or update-check request is made when its policy setting is disabled.
- Network failures must not crash startup or core workflows.
- Privacy-related settings must be included in user data compatibility and support bundle redaction where applicable.
- Legacy DPAPI Telegram ciphertext must never be treated as decryptable Linux input; Task 18 preserves it and requires explicit re-entry.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Telemetry disabled by default.
- Opt-in enables only the documented telemetry calls.
- Separate opt-in/opt-out behavior for update checks, Material DB updates, usage statistics, laser statistics, and Telegram notifications.
- Update available, no update, malformed response, timeout, cancellation, and offline failures.
- Update metadata/artifact version mismatch and missing or invalid integrity information.
- No outbound HTTP call occurs when each policy flag is disabled, verified with fake HTTP services.
- Startup does not block on update checks.
- External URL open success and failure through a fake platform service.
- Unavailable secret store and explicit Telegram token re-entry behavior.

## Checkpoint Report
Create `docs/checkpoints/21-telemetry-update-and-privacy-policy.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/21-privacy-updates`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 21: Telemetry update and privacy policy`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Network and telemetry-like behavior is inventoried and policy-controlled.
- Telemetry is disabled by default or a maintainer-approved exception is documented.
- Update checks are cancellable, non-blocking, and tested.
- The checkpoint exists, and the commit has been pushed.
