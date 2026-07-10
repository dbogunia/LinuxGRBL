# Building The Linux Port

The legacy `LaserGRBL.sln` and `LaserGRBL/LaserGRBL.csproj` remain Windows/.NET Framework reference artifacts. Build only `LaserGRBL.Linux.sln` for the Linux port.

## Prerequisites

- .NET SDK 8.0 or later compatible with the repository's package policy.
- Network access for the initial NuGet restore.
- A graphical Linux session only when starting the Avalonia skeleton; build and test are headless.

## Commands

```bash
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore LaserGRBL.Linux.sln
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet run --project LaserGRBL.Avalonia/LaserGRBL.Avalonia.csproj
```

`Directory.Packages.props` centrally pins the initial Avalonia and test-package versions. Tasks after Task 01 migrate behavior into these projects; they do not retarget the legacy WinForms project.
