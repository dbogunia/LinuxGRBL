# Task 09: Linux Process Tools

## Goal
Replace Windows-only external tool launches with Linux-capable process services for firmware flashing, autotrace, and driver guidance.

## Context
The legacy app launches `Firmware\\avrdude.exe`, `Autotrace\\autotrace.exe`, and `Driver\\CH341SER.EXE`. Linux should use installed tools or packaged Linux binaries, with clear error messages when unavailable.

## Scope
- Implement process execution through `IProcessRunner`.
- Implement Linux firmware flashing through `IFirmwareFlashService` using system `avrdude`.
- Implement autotrace invocation through a configurable Linux tool path.
- Replace CH341 driver installer flow on Linux with a user-facing guidance result.

## Out of Scope
- Do not implement the Avalonia firmware flashing UI.
- Do not package Linux binaries yet unless required for tests.
- Do not run real firmware flashing unless explicitly configured for a real device.

## Implementation Requirements
- Process arguments must be passed safely as argument lists, not concatenated shell strings.
- Missing tools must produce actionable messages.
- Firmware flashing must support dry-run/test construction of command arguments.
- Real flashing must require explicit user action in later UI code.
- Autotrace temporary files must use platform temp paths.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- `avrdude` argument construction.
- Missing `avrdude` result.
- Autotrace argument/temp path construction.
- Missing autotrace result.
- CH341 Linux guidance result.
- Process runner captures stdout, stderr, exit code, timeout, and cancellation.

## Checkpoint Report
Create `docs/checkpoints/09-linux-process-tools.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 09: Linux process tools`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Linux process/tool services are implemented and tested.
- No Linux flow depends on `.exe` tool execution.
- The checkpoint exists, and the commit has been pushed.
