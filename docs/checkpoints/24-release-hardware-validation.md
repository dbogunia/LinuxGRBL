# Task 24 Checkpoint: Release Hardware Validation

Prepared a repeatable release hardware validation runner for the remaining Linux release blockers.

## Implemented Changes

- Added `scripts/validate-release-hardware.sh`.
- The runner verifies the release tarball checksum, extracts into a clean validation directory, installs desktop/MIME integration into an isolated `XDG_DATA_HOME`, and performs a clean-install startup/file-open smoke with sample G-code.
- The runner detects serial hardware in this order:
  - explicit device argument;
  - stable `/dev/serial/by-id/*`;
  - fallback `/dev/ttyUSB*` or `/dev/ttyACM*`.
- Missing serial hardware exits with status `3` and writes a blocked report instead of pretending the release blocker is closed.
- Present readable/writable hardware records the detected device and appends the manual GRBL workflow evidence checklist.

## Current Validation Host Result

The current validation host has no detected serial hardware:

- `/dev/serial/by-id` is absent.
- `/dev/ttyUSB0`, `/dev/ttyUSB1`, `/dev/ttyACM0`, and `/dev/ttyACM1` are absent.

Therefore real USB GRBL workflow validation and clean-install package/device validation remain release-blocking until a physical controller is attached.

## Expected Usage

```bash
scripts/build-linux-tarball.sh linux-x64 0.1.0
scripts/validate-release-hardware.sh linux-x64 0.1.0
```

With an explicit device:

```bash
scripts/validate-release-hardware.sh linux-x64 0.1.0 /dev/serial/by-id/<controller>
```

The report is written to `artifacts/release-hardware-validation/report.md`.

## Remaining Risks

- Release blocker: real USB GRBL workflow validation still requires physical hardware.
- Release blocker: clean-install package/device validation with serial hardware still requires physical hardware.
- High: firmware flashing, WiFi configuration, audio behavior, and full manual 3D interaction parity still require safe environment-specific validation.

## Completion Status

Task 24 does not close the hardware blockers. It makes the final validation repeatable and fail-closed so the release can only be marked ready after real hardware evidence exists.
