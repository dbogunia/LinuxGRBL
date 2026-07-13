# Safety, Legal, And First Run

Task 20 ports the first-run safety gate for the Linux application. Risky machine operations fail closed unless the required acknowledgement state is present in versioned settings.

## Acknowledgement Policy

| Workflow | Scope | Linux behavior |
| --- | --- | --- |
| First-run safety notice | One-time | Must be accepted before starting jobs, reset/abort operations, laser test commands, or firmware flashing. |
| Legal/safety notice version | Per-version | Stored as `linux-port-safety-v1`; version mismatch blocks risky operations until acknowledged again. |
| Firmware flashing warning | Per-version | Stored as `linux-port-firmware-v1`; firmware flashing stays blocked until acknowledged for the current warning version. |
| Reset warning | Per-session | Service exposes a per-session requirement; current view-model gate also requires the persisted first-run/legal acknowledgement before reset. |
| Abort warning | Per-operation | Service exposes a per-operation requirement; current view-model gate also requires the persisted first-run/legal acknowledgement before abort. |
| Laser test warning | Per-operation | Service exposes the requirement for future laser-test UI. No laser-test command is currently wired in the Linux MVP. |

## Implemented Gate

`SafetyAcknowledgementService` owns the fail-closed decision for risky operations. It accepts a `SafetyAcknowledgementState` from `PortSettings` and returns `OperationResult` failures instead of throwing.

Blocked operations:

- job start;
- firmware flash initiation;
- soft reset;
- abort/stop;
- future laser test commands through the same service contract.

`PortSettings` schema is now version 3 and stores `SafetyAcknowledgements`. Corrupt settings load through the existing fallback path, which normalizes to an empty acknowledgement state. That means risky operations remain blocked after corrupt settings rather than accidentally becoming allowed.

## Countdown

`SafetyCountdown` provides a UI-independent countdown state machine for warning dialogs:

- ticks advance explicitly;
- completion is testable without timers;
- cancellation returns a failure result.

## Localization

Safety message keys are available through `LocalizationCatalog`:

- `Safety.StartJob`
- `Safety.FirmwareFlash`
- `Safety.Reset`
- `Safety.Abort`
- `Safety.LaserTest`

English and Polish strings are present for the Linux-port safety gate.

## Changed Legacy Behavior

No legacy warning was intentionally weakened. The Linux port does not yet implement every legacy dialog visually, but the risky-operation gate is stricter than the previous placeholders: risky commands are blocked in the real bootstrap until the persisted acknowledgement state is present.
