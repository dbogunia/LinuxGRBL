# Task 07 Checkpoint: Linux Serial Service

## Summary

Implemented the Linux serial-device service, including a real managed `System.IO.Ports` connection and a deterministic in-memory substitute for tests.

## Implemented Changes

- Enumerates `/dev/ttyUSB*`, `/dev/ttyACM*`, and stable `/dev/serial/by-id/*` paths; unrelated Linux TTY devices are excluded.
- Opens, writes newline-terminated ASCII commands to, reads lines from, flushes, and disposes physical serial connections.
- Supports baud rate, data bits, DTR, RTS, newline, and read-timeout options.
- Reports serial permission failures with Linux group/udev guidance.
- Provides `InMemorySerialPortService` and `ISerialConnection` for hardware-independent consumers and tests.

## Test Evidence

`dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors. `dotnet test LaserGRBL.Linux.sln --no-build` validates enumeration, options, buffer discard, fake writes, and an unavailable physical device without requiring serial hardware.

## Remaining Risks

- A physical GRBL-controller smoke test still needs to be run on a target Linux distribution, including the expected user group or udev rule.
- Hardware-specific signal behaviour (DTR/RTS) is intentionally not emulated by the in-memory connection.

## Completion Status

Complete for Task 07 implementation; physical-device validation remains an integration check.
