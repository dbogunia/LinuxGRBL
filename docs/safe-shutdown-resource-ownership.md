# Safe Shutdown And Resource Ownership

Task 15D defines the Linux session ownership and shutdown contract for active machine-control sessions.

## Hardware Safety Statement

Application shutdown is a best-effort software safety path only. It does not replace a physical emergency stop, interlock, power disconnect, firmware alarm handling, or operator supervision.

If the transport is already disconnected, LinuxGRBL must not claim that it stopped the machine. It records the failed best-effort result and requires the user to verify the machine before continuing.

## Lifecycle Table

| State | Ordinary Close | Cancellation/Logout | Transport Failure | Resource Handling |
| --- | --- | --- | --- | --- |
| Disconnected | no controller command | no controller command | no controller command | no lock held |
| Connecting | disconnect session | disconnect session | mark unexpected disconnect | release lock/transport |
| Idle | disconnect session | disconnect session | mark unexpected disconnect | release lock/transport |
| Active job | bounded `M5` abort attempt, then disconnect | bounded `M5` abort attempt, then disconnect | do not send after known disconnect | release lock/transport |
| Hold | disconnect session | disconnect session | mark unexpected disconnect | release lock/transport |
| Jog | bounded shutdown via normal session close; continuous jog state is discarded | same | do not send after known disconnect | release lock/transport |
| Alarm/failure | preserve issue in diagnostics | preserve issue in diagnostics | preserve unexpected disconnect | release lock/transport |

## Ownership

Machine resources are locked by configured device/endpoint identity before opening the transport. For serial connections this is the selected device path, preferably `/dev/serial/by-id/*`.

The Linux implementation uses an advisory file lock under the app cache directory. A competing instance receives a clear conflict result instead of silently opening the same configured device.

Locks are released on normal disconnect and workflow disposal. If a process terminates, OS file locks are released with the process.

## Recovery

Restart recovery fails closed unless all of the following are verified:

- previous job identity
- last known machine position
- homing state
- explicit user acknowledgement after restart

Persisted state may support diagnostics and UX hints, but it is not trusted as proof that a machine can safely resume unattended.

## Diagnostics

Failed best-effort safety commands remain visible in the shutdown result and UI/log diagnostics. Permission and ownership conflicts include the affected device or endpoint.
