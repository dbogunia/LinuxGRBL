# Task 15D: Safe Shutdown And Resource Ownership

## Goal

Define and test safe ownership, cancellation, shutdown, crash recovery, and single-device access semantics for active machine-control sessions.

## Context

The legacy core can abort a program with `M5`, close communications, and preserve job state, but its lifecycle is entangled with WinForms. Linux introduces additional termination paths (window close, session logout, SIGTERM, transport disappearance) and must prevent accidental competing access to a controller.

## Scope

- Define an explicit asynchronous session lifecycle: connect, active job, hold, abort, disconnect, disposal, and failure states.
- On ordinary application close, cancellation, and transport failure, execute a bounded best-effort safety sequence appropriate to the active firmware, record its result, and release resources deterministically.
- Define crash/restart recovery: what state is persisted, what state is deliberately not trusted, and what explicit user acknowledgement/re-homing is required before resume.
- Prevent simultaneous ownership of the same serial device or configured network endpoint by multiple application instances, with a clear user-facing conflict result.
- Add a PTY-based serial integration harness for open/read/write/cancellation/disconnect/permission scenarios and use it in Linux tests where feasible.

## Out Of Scope

- Do not claim that application shutdown replaces a physical emergency stop, interlock, or firmware safety feature.
- Do not reintroduce `SincroStart` multi-instance coordinated running; it remains deferred under Task 22.
- Do not send unsafe commands after a transport is known to be disconnected.

## Implementation Requirements

- The core lifecycle must not depend on UI-thread shutdown timing.
- Safety commands and cleanup must have explicit timeout/cancellation results; failure to send them must be logged and visible in diagnostics.
- Device/endpoint locks must be released on normal disposal and be safely recoverable after process termination.
- Resume must fail closed when position, homing, job identity, or acknowledgement requirements cannot be verified.
- Documentation must state hardware safety limitations prominently.

## Tests

- Test normal close during idle, active job, hold, and jog states using fake firmware transports.
- Test transport loss, cancellation timeout, failed safety-command write, restart recovery refusal, and resource-lock contention.
- Run PTY tests for disconnect and cancellation where supported by the CI host.
- Run `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`.

## Checkpoint Report

Create `docs/checkpoints/15d-safe-shutdown-and-resource-ownership.md` with lifecycle state table, PTY/fake-transport evidence, hardware-safety statement, and remaining risks.

## Branch And Pull Request

Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/15d-safe-shutdown`, push that branch, and open a pull request to protected `master`. Do not push directly to `master`.

## Acceptance Criteria

- Shutdown, transport loss, and recovery behavior are explicit and Linux-testable.
- Two instances cannot silently control the same configured device/endpoint.
- Safety limitations and failed best-effort actions are visible to users and support diagnostics.
