# Task 03: Extract GRBL Core

## Goal
Move GRBL state, status parsing, command streaming, and communication orchestration into `LaserGRBL.Core` without WinForms dependencies.

## Context
The legacy `LaserGRBL/Core/GrblCore.cs` owns the machine state and streaming behavior but directly references WinForms types, UI forms, and `System.Web`. The Linux app needs this behavior available from a UI-independent library.

## Scope
- Extract protocol/state-machine code needed for connection, status updates, jogging, streaming, hold/resume/reset, queue handling, overrides, configuration parsing, and emulator-driven workflows.
- Preserve the `Grbl`, `Smoothie`, `Marlin`, and `VigoWork` firmware strategies behind a UI-independent firmware/protocol selection boundary.
- Replace legacy high-resolution Windows timing and timer-resolution calls with `IMonotonicClock` for stream watchdogs, connection timeouts, job duration, and laser-life accounting.
- Replace WinForms synchronization with `IUiDispatcher`.
- Replace direct UI callbacks with events or observable state models that Avalonia view-models can consume later.
- Keep compatibility with existing communication abstractions or introduce a clean adapter.

## Out of Scope
- Do not implement the Avalonia main workflow UI.
- Do not port every dialog-specific behavior.
- Do not remove the legacy WinForms project.

## Implementation Requirements
- `LaserGRBL.Core` must not reference `System.Windows.Forms`.
- Core state changes must be testable without a UI thread.
- Preserve GRBL version/status models and command semantics.
- Preserve firmware-specific polling, reset/open behavior, status parsing, and true-jog capability rules; no non-GRBL firmware may silently fall back to GRBL semantics.
- Any behavior temporarily not extracted must be documented with a concrete reason and follow-up task reference.
- Avoid broad rewrites unrelated to core extraction.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add or migrate tests for:
- GRBL status line parsing.
- GRBL 0.9/1.1/hal version detection where existing code supports it.
- Queue/streaming behavior with a fake communication channel.
- Jog command generation.
- Emulator connection lifecycle where feasible.
- Smoothie, Marlin, and Vigo status parsing and their firmware-specific command behavior.
- Streaming timeout and duration behavior with a fake monotonic clock.

## Checkpoint Report
Create `docs/checkpoints/03-extract-grbl-core.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 03: Extract GRBL core`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Core GRBL behavior builds in `LaserGRBL.Core` without WinForms references.
- Required tests pass.
- The checkpoint exists, and the commit has been pushed.
