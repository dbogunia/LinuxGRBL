# Task 15D Checkpoint: Safe Shutdown And Resource Ownership

## Summary

Added explicit Linux-safe session shutdown, resource ownership, and fail-closed recovery boundaries for active machine-control sessions. The implementation is UI-independent at the core lifecycle layer and wired into the Avalonia workflow for serial resource locking.

## Implemented Changes

- Added `MachineShutdownCoordinator` for bounded shutdown on:
  - ordinary application close
  - cancellation/logout
  - transport failure
- Added `MachineShutdownResult` so failed best-effort safety actions are visible to diagnostics.
- Added fail-closed `MachineRecoveryPolicy` for restart/resume decisions.
- Added `IMachineResourceLockProvider` and `IMachineResourceLock` contracts.
- Added `FileMachineResourceLockProvider` for advisory per-device/endpoint locking.
- Wired resource locking into `MainWorkflowViewModel.ConnectAsync`, `DisconnectAsync`, and disposal.
- Added clearer UI diagnostics that include both `OperationError.Message` and `OperationError.Detail`.
- Added a PTY-backed serial smoke test using Unix `openpty`.
- Added `docs/safe-shutdown-resource-ownership.md` with lifecycle table, hardware safety statement, ownership model, and recovery policy.

## Lifecycle State Table

| State | Shutdown Behavior | Safety Command | Result |
| --- | --- | --- | --- |
| Disconnected | no-op close | none | no lock held |
| Connecting | disconnect session | none | lock/transport released |
| Idle | disconnect session | none | lock/transport released |
| Active job | bounded abort attempt | `M5` | result records success/failure |
| Hold | disconnect session | none | lock/transport released |
| Jog | normal session close; jog intent discarded | none unless an active job is present | lock/transport released |
| Transport failure | mark unexpected disconnect | none after known disconnect | lock/transport released |

## Hardware Safety Statement

Application shutdown is a best-effort software safety path only. It does not replace a physical emergency stop, interlock, power disconnect, firmware alarm handling, or operator supervision.

When the transport is already disconnected, LinuxGRBL must not claim that it stopped the machine. It records the failed or impossible best-effort result and requires user verification before continuing.

## Resource Ownership

The Avalonia workflow now acquires a machine resource lock before opening the selected serial device. A competing instance receives a user-visible ownership conflict instead of silently sharing the configured endpoint.

Locks are released on disconnect and disposal. OS file locks are recoverable after process termination.

## Recovery Policy

Resume after restart is refused unless all are verified:

- job identity
- last known machine position
- homing state
- explicit user acknowledgement

Persisted state is diagnostic and advisory; it is not trusted as proof that unattended resume is safe.

## Test Evidence

Local commands run:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1`

Completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

Passed 192 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Test coverage added:

- idle shutdown disconnects and disposes without safety command.
- active-job shutdown sends `M5` and records manual abort.
- failed safety write is visible while resources are still released.
- transport failure records unexpected disconnect and does not send after known disconnect.
- restart recovery refuses unsafe resume until identity/position/homing/acknowledgement are verified.
- file lock blocks competing resource ownership and releases on disposal.
- Avalonia workflow acquires and releases the resource lock around connect/disconnect.
- PTY smoke test opens a Unix pseudo-terminal and verifies serial write behavior through `SystemSerialConnection`.

## Remaining Risks

- PTY smoke does not replace testing with real GRBL hardware and physical emergency stop/interlock behavior.
- Crash recovery persistence is policy-only in Task 15D; richer persisted restart UX remains later validation/release work.
- Manual Linux GPU/display validation from Task 13B is still not verified and remains release-blocking until Task 16/22 records real graphical evidence.
- Clean-install hardware validation from Task 15C remains release-blocking.

## Commit And Push

Implementation branch: `feature/15d-safe-shutdown`

Implementation commit: `eb929dc` (`Task 15D: Safe shutdown and resource ownership`)

Push: pending until this checkpoint metadata is committed.

## Completion Status

Task 15D is implemented locally. Safe shutdown, transport failure handling, recovery refusal, resource ownership, and PTY serial smoke coverage are explicit and Linux-testable.
