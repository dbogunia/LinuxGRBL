# Logging, Diagnostics, And Support Bundle

Task 19 adds Linux-port diagnostics that can run without a connected GRBL device. Logs use `IAppPaths.LogDirectory`, support bounded rotation, and are safe to fail without crashing startup or machine-control workflows.

## Log Files

| Channel | File | Purpose |
| --- | --- | --- |
| Application | `lasergrbl.log` | startup, UI messages, nonfatal warnings, app-level errors |
| Session | `session.log` | machine-control session lifecycle and job-level diagnostics |
| Communication | `communication.log` | visible transmitted and received machine commands/responses |
| Connection | `connection.log` | serial/network connection discovery and connect/disconnect diagnostics |

`AppLogSink` rotates each file when it reaches the configured size limit. Archives use numeric suffixes such as `lasergrbl.log.1`, with a bounded retained archive count.

Log writes return `DiagnosticLogWriteResult`; callers can inspect failures, but logging failures remain nonfatal.

## Support Bundle

`SupportBundleService` writes a local zip archive. It does not upload data.

Included by default:

- `manifest.json`: creation time, app assembly version, OS, and .NET runtime.
- `paths.txt`: XDG app paths with the current home directory shortened to `~` where possible.
- `settings-summary.json`: settings schema, firmware, streaming mode, color scheme, language, and recent-file count.
- `startup-diagnostics.txt`: nonfatal startup diagnostics after redaction.
- `device-discovery.json`: discovered serial descriptors supplied by the caller.
- `package-metadata.json`: included when package metadata is available.
- `logs/*.log`: recent lines from each diagnostic channel.

The bundle reports both included and skipped entries. Empty log channels and missing package metadata are listed as skipped rather than silently ignored.

## Redaction

`DiagnosticRedactor` centralizes redaction before bundle export:

- redacts keys containing `password`, `passwd`, `token`, `secret`, `authorization`, `api_key`, `apikey`, or `credential`;
- redacts inline `key=value` token-like values;
- shortens the current home path to `~` where possible;
- does not include raw user file contents by default.

The support bundle intentionally includes settings summaries and recent log lines, not arbitrary user files.
