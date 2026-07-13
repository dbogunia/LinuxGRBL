# Task 19 Checkpoint: Logging, Diagnostics, And Support Bundle

Implemented Linux-ready diagnostic logging and local support bundle export without telemetry, remote upload, or hardware requirements.

## Implemented Changes

- Added platform `AppLogSink` with UI-independent diagnostic channels:
  - application log,
  - session log,
  - communication TX/RX log,
  - connection log.
- Routed logs through `IAppPaths.LogDirectory`.
- Added bounded log rotation through `DiagnosticLogOptions`.
- Made log write failures nonfatal through `DiagnosticLogWriteResult`.
- Added `DiagnosticRedactor` for centralized redaction of passwords, tokens, credentials, authorization values, API keys, and current-home paths.
- Added `SupportBundleService` to create a local zip containing:
  - app/runtime manifest,
  - redacted path summary,
  - settings summary,
  - startup diagnostics,
  - discovered serial devices supplied by the caller,
  - package metadata when available,
  - recent redacted log lines.
- Updated Avalonia bootstrap to use the platform logger instead of a UI-local logger.
- Documented log channels, bundle contents, skipped-file reporting, and redaction policy in `docs/logging-diagnostics-support-bundle.md`.

## Tests Run

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

The plain sandboxed VSTest path still cannot run because local socket creation is denied, matching earlier checkpoints.

## Test Evidence

- Build passed with 0 warnings and 0 errors.
- Test run passed: 215/215 tests.
- New coverage includes:
  - log path creation using fake `IAppPaths`,
  - application/session/communication/connection log writes,
  - log write failure fallback without throwing,
  - bounded log rotation,
  - support bundle contents and skipped package metadata reporting,
  - redaction of sensitive settings/WiFi-style values and current-home paths.

## Git

- Branch: `feature/19-diagnostics`
- Implementation commit: `a0d4da1` (`Task 19: Logging diagnostics and support bundle`)
- Metadata commit: `444efcf` (`Record Task 19 checkpoint metadata`)
- Push: branch pushed to `origin/feature/19-diagnostics`; PR #27 opened at https://github.com/dbogunia/LinuxGRBL/pull/27.

## Remaining Risks

- Support bundle export is service-level and ready for UI wiring, but there is not yet a native file-dialog flow for choosing the bundle destination.
- Device discovery content depends on the caller passing the latest serial descriptor list; no connected hardware is required or assumed.
- Communication logging hooks are implemented as logger APIs and tested directly; deeper automatic integration into every machine session path can be expanded as Task 22 parity work.
- Manual Linux GPU/display validation, USB GRBL hardware validation, and clean-install hardware validation remain release-blocking per prior checkpoints.

## Completion Status

Task 19 implementation and tests are complete. Commit and push metadata will be recorded after the final commit is created and pushed.
