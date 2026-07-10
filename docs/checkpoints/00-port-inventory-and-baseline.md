# Task 00 Checkpoint: Port Inventory And Baseline

## Summary

Created the Linux-port baseline inventory and refreshed the documented execution order against the local legacy repository. No production source, target framework, or project structure was changed.

## Evidence

- Host: Linux Mint 22.3, .NET SDK 8.0.422, `linux-x64`.
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.sln --no-restore` fails with `MSB3644` for `.NETFramework v4.0` before compilation.
- Legacy application: 139 WinForms-using source files, 239 `System.Drawing`-using source files, 54 designer files, 416 `.resx` files.
- The legacy solution contains only `LaserGRBL`; the WindowsForms `netcoreapp3.1` test project is not part of it.

## Remaining Risks

- No Linux SDK-style projects or Linux-runnable tests exist yet; Task 01 is the immediate blocker.
- Serial safety, OpenGL parity, GDI/image replacement, and package/device access remain release-required work.

## Completion Status

Complete for Task 00. The factual inventory is in `docs/linux-port-inventory.md`; implementation begins with Task 01.
