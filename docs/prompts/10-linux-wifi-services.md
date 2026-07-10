# Task 10: Linux WiFi Services

## Goal
Replace Windows WiFi/IP APIs with Linux-capable WiFi discovery and configuration services.

## Context
The legacy code uses `netsh.exe` for WiFi interface details and `iphlpapi.dll` for IP helper behavior. Linux needs .NET network APIs and optional NetworkManager integration.

## Scope
- Implement `IWifiService` for Linux.
- Use .NET network interface APIs for IP/interface discovery where possible.
- Use `nmcli` where available for WiFi SSID/interface information and configuration support.
- Provide clear unsupported-state results if NetworkManager or permissions are unavailable.

## Out of Scope
- Do not implement Avalonia WiFi UI screens.
- Do not require WiFi hardware for unit tests.
- Do not perform destructive network changes in automated tests.

## Implementation Requirements
- Parse `nmcli` output using a stable machine-friendly mode where possible.
- Keep all network-changing operations explicit and separate from read-only discovery.
- Return structured results with user-readable failure messages.
- Do not shell-concatenate user-provided SSIDs/passwords.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- `nmcli` parser with sample outputs.
- Network interface filtering using fakes.
- Missing `nmcli` result.
- Permission/unsupported result formatting.
- Configuration command argument construction without executing real changes.

## Checkpoint Report
Create `docs/checkpoints/10-linux-wifi-services.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/10-wifi-services`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 10: Linux WiFi services`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Linux WiFi service is implemented and testable without hardware.
- Windows-only `netsh.exe`/`iphlpapi.dll` behavior has Linux replacements.
- The checkpoint exists, and the commit has been pushed.
