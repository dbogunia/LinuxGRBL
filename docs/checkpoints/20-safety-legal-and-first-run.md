# Task 20 Checkpoint: Safety, Legal, And First Run

Implemented the Linux-port safety/legal first-run gate with persisted acknowledgement state, localized warning keys, countdown state, and risky-operation blocking in view-model paths.

## Implemented Changes

- Added `SafetyAcknowledgementState` to `PortSettings` and bumped settings schema to version 3.
- Added `SafetyAcknowledgementService` with explicit requirements for:
  - job start,
  - firmware flashing,
  - reset,
  - abort,
  - laser test commands.
- Added `SafetyCountdown` for UI-independent countdown completion/cancellation tests.
- Added `ISafetyGate` and a permissive test/default gate for isolated view-model construction.
- Wired the real Avalonia bootstrap to use persisted settings-backed `SafetyAcknowledgementService`.
- Gated `MainWorkflowViewModel.RunJobAsync`, `ResetAsync`, and `StopAsync` when the real safety gate is supplied.
- Gated `FirmwareFlashToolViewModel.FlashAsync` when the real safety gate is supplied.
- Added English and Polish localization keys for safety warnings.
- Documented acknowledgement scope and changed-warning policy in `docs/safety-legal-first-run.md`.

## Tests Run

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

The plain sandboxed VSTest path still cannot run because local socket creation is denied, matching earlier checkpoints.

## Test Evidence

- Build passed with 0 warnings and 0 errors.
- Test run passed: 222/222 tests.
- New coverage includes:
  - first-run acknowledgement persistence through settings,
  - safety countdown completion and cancellation,
  - job start blocked before required acknowledgement,
  - firmware flash blocked before required acknowledgement,
  - reset/abort blocked before required acknowledgement,
  - corrupt settings fail closed for risky operations,
  - localized safety/legal message lookup.

## Changed Or Removed Warnings

No legacy warning was intentionally weakened or removed. The Linux port still lacks the full visual dialog parity of the legacy WinForms app, but risky operation paths now fail closed in the real bootstrap until acknowledgement state is present.

## Git

- Branch: `feature/20-safety-first-run`
- Implementation commit: pending until commit is created.
- Push: pending until branch is pushed.

## Remaining Risks

- The acknowledgement UI itself is still service/view-model level; a polished native dialog flow remains future UI work.
- Laser-test command UI is not currently wired in the Linux MVP; the service exposes the gate for that future operation.
- Per-session/per-operation dialog UX needs final parity review in Task 22.
- Manual Linux GPU/display validation, USB GRBL hardware validation, and clean-install hardware validation remain release-blocking per prior checkpoints.

## Completion Status

Task 20 implementation and tests are complete. Commit and push metadata will be recorded after the final commit is created and pushed.
