# Task 10 Checkpoint: Linux WiFi Services

## Summary

Implemented Linux WiFi discovery and explicit NetworkManager configuration support behind the shared `IWifiService` boundary.

## Implemented Changes

- `LinuxWifiService` lists WiFi networks with `nmcli -t -f IN-USE,SSID,SIGNAL,DEVICE dev wifi list`.
- `LinuxWifiService` exposes Linux network interfaces through a testable `INetworkInterfaceProvider`, filtering loopback/down interfaces and identifying wireless interfaces.
- WiFi connection requests build an explicit `nmcli dev wifi connect ... password ... ifname ...` argument list without shell-concatenating SSIDs, passwords, or interface names.
- Network-changing WiFi configuration is separate from read-only discovery and supports dry runs for validation.
- Missing `nmcli`, unavailable NetworkManager, and permission/polkit failures return user-readable `OperationResult` errors.
- `nmcli` terse output parsing handles escaped field separators and selects the strongest duplicate SSID.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 114 tests, including parser, interface filtering, missing `nmcli`, permission formatting, dry-run configuration, and argument-list construction coverage.

## Remaining Risks

- No physical WiFi hardware or real NetworkManager configuration change was performed.
- Future Avalonia UI must keep connection changes explicit and avoid running configuration commands from passive discovery flows.

## Commit And Push

Commit: `f9fef18` (`Task 10: Linux WiFi services`)

Push: pending

## Completion Status

Complete for Task 10 implementation once push is recorded.
