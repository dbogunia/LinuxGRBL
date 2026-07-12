# LinuxGRBL Linux Package Notices

This package is a Linux Avalonia port artifact for the LaserGRBL codebase.

## Bundled Managed Dependencies

The Linux tarball is produced by `dotnet publish` from the SDK-style projects and uses centrally pinned NuGet versions from `Directory.Packages.props`.

- Avalonia 12.0.5
- Avalonia.Desktop 12.0.5
- Avalonia.Themes.Fluent 12.0.5
- SkiaSharp 3.119.4
- SkiaSharp.NativeAssets.Linux 3.119.4
- System.IO.Ports 8.0.0

When published self-contained, the selected .NET runtime for the target RID is bundled by the .NET SDK. The package manifest records this as `self-contained`.

## Optional Host Tools

These tools are not bundled by Task 15. They are discovered at runtime or used only when the related feature is invoked.

- `pw-play`, `paplay`, or `aplay` for sound playback.
- `avrdude` for firmware flashing.
- `autotrace` for optional conversion paths.
- `NetworkManager`/`nmcli` for WiFi discovery and configuration.

## Legacy Windows Binaries

The Linux package path must not ship Windows-only binaries from the legacy tree, including `Firmware/avrdude.exe`, `Firmware/libusb0.dll`, `Autotrace/autotrace.exe`, or `Driver/CH341SER.EXE`.

## Integrity

The tarball build script writes a sibling `.sha256` file. Release publication should publish both files together and document the expected SHA256 value.
