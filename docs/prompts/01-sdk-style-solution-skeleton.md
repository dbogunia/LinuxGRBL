# Task 01: SDK-Style Solution Skeleton

## Goal
Introduce a modern SDK-style solution structure for the Linux port while keeping the legacy WinForms project intact for reference.

## Context
The current main app is a non-SDK `.NETFramework v4.0` WinForms project. Linux porting needs new SDK-style projects that can build with the installed .NET SDK on Linux before behavior is migrated.

## Scope
Add:
- `LaserGRBL.Core` as an SDK-style class library targeting `net8.0`.
- `LaserGRBL.Platform` as an SDK-style class library targeting `net8.0`.
- `LaserGRBL.Avalonia` as an SDK-style Avalonia desktop app targeting `net8.0`.
- Updated or additional SDK-style test project targeting `net8.0`.
- Solution entries for the new projects.

## Out of Scope
- Do not migrate substantial production logic yet.
- Do not delete the legacy `LaserGRBL` project.
- Do not attempt feature parity UI implementation.

## Implementation Requirements
- Keep project names and namespaces predictable: `LaserGRBL.Core`, `LaserGRBL.Platform`, `LaserGRBL.Avalonia`.
- Wire references as: Avalonia app references Core and Platform; Platform references Core only if needed; tests reference Core and Platform as needed.
- Use current stable package versions already available through normal restore. Prefer minimal package additions.
- Add a short `docs/linux-port-build.md` section explaining how to build the new skeleton.
- Ensure the legacy project remains untouched except for solution metadata if needed.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test`

If the legacy project still prevents solution build, create a Linux-port solution such as `LaserGRBL.Linux.sln` containing only new projects and tests, then run restore/build/test against that solution. Document this explicitly.

## Checkpoint Report
Create `docs/checkpoints/01-sdk-style-solution-skeleton.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/01-sdk-skeleton`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 01: SDK-style solution skeleton`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- New SDK-style projects exist and are included in a buildable Linux-port solution.
- Build/test commands pass for the new solution or any skipped legacy build is documented as expected.
- The checkpoint exists, and the commit has been pushed.
