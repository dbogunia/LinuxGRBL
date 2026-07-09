# Task 00: Port Inventory And Baseline

## Goal
Create a factual baseline report for the Linux port: current build behavior, project structure, Windows-only dependencies, native binary assumptions, existing tests, and immediate blockers.

## Context
The current repository contains a legacy `LaserGRBL/LaserGRBL.csproj` targeting `.NETFramework v4.0` with WinForms, plus `LaserGRBL.Tests/LaserGRBL.Tests.csproj` targeting `netcoreapp3.1` with `UseWindowsForms`. On Linux with .NET 8, the main project is expected to fail before compilation because .NET Framework reference assemblies are unavailable.

## Scope
Inspect and document:
- Solution/project structure and target frameworks.
- WinForms and `System.Drawing` usage.
- Windows-only APIs and tools, including WMI, registry, `kernel32.dll`, `iphlpapi.dll`, `netsh.exe`, `avrdude.exe`, `autotrace.exe`, CH341 driver installer, updater elevation, hardcoded path separators, and bundled native dependencies.
- Existing test project status and runnable tests.
- Linux build/test command results.

## Out of Scope
- Do not change production code.
- Do not retarget projects.
- Do not add new app projects.
- Do not fix build failures in this task.

## Implementation Requirements
- Create or update a concise inventory document at `docs/linux-port-inventory.md`.
- Include exact commands run and important output snippets.
- Include a prioritized blocker list grouped by build system, UI, platform APIs, serial/native dependencies, external tools, persistence, and packaging.
- Include a recommended execution order that matches the numbered prompt tasks.
- Use local repository inspection as the source of truth.

## Tests
Run these commands and record the results:
- `dotnet --info`
- `dotnet build LaserGRBL.sln --no-restore`
- `find . -maxdepth 3 -type f \( -name '*.sln' -o -name '*.csproj' -o -name 'Directory.Build.*' -o -name 'global.json' \) -print`
- `rg -n "DllImport|Registry|ManagementObject|SerialPort|GetPortNames|Process\.Start|Application\.ExecutablePath|netsh\.exe|avrdude\.exe|autotrace\.exe|CH341SER|BinaryFormatter|System\.Windows\.Forms|System\.Drawing" LaserGRBL LaserGRBL.Tests`

If `dotnet build` fails because the SDK writes to a read-only home directory, rerun it with `DOTNET_CLI_HOME=/tmp/dotnet-home`.

## Checkpoint Report
Create `docs/checkpoints/00-port-inventory-and-baseline.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests/checks are complete and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 00: Port inventory and baseline`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- `docs/linux-port-inventory.md` exists and covers every scope item.
- Required commands were run or have a concrete documented blocker.
- The checkpoint exists and contains a comprehensive execution report.
- A task-specific commit exists and has been pushed.
