# Task 00: Port Inventory And Baseline

## Goal
Create a factual baseline report for the Linux port: current build behavior, project structure, Windows-only dependencies, native binary assumptions, existing tests, and immediate blockers.

## Context
The current repository contains a legacy `LaserGRBL/LaserGRBL.csproj` targeting `.NETFramework v4.0` with WinForms, plus `LaserGRBL.Tests/LaserGRBL.Tests.csproj` targeting `netcoreapp3.1` with `UseWindowsForms`. On Linux with .NET 8, the main project is expected to fail before compilation because .NET Framework reference assemblies are unavailable.

## Scope
Inspect and document:
- Solution/project structure and target frameworks.
- WinForms and `System.Drawing` usage.
- Windows-only APIs and tools, including WMI, registry, `kernel32.dll`, `iphlpapi.dll`, `netsh.exe`, `avrdude.exe`, `autotrace.exe`, CH341 driver installer, updater elevation, hardcoded path separators, and bundled native dependencies.
- Localization and resource assumptions, including `Strings.*.resx`, form-specific `.resx` files, icons, embedded images, and non-text resources.
- User data and compatibility formats, including settings, custom buttons, hotkeys, material databases, bundled presets, and imported/exported user files.
- Logging, diagnostics, telemetry, update checks, Telegram/external notification paths, safety/legal flows, and any first-run behavior.
- Preview/rendering technologies, including SharpGL, `GrblPanel3D`, `Obj3D`, OpenGL render-context assumptions, and required 3D parity work.
- Network and privacy-sensitive paths, including `UsageStats`, `Telegram`, `UrlManager`, update checks, Material DB downloads, laser statistics upload, and external URL/file opening.
- Build/release assumptions: existing CI, supported architectures and desktop stacks, package formats, license/NOTICE obligations for vendored code and binaries, update-artifact integrity, desktop/MIME integration, and device/sandbox permission model.
- Image pipeline assumptions: `System.Drawing`/GDI+, image codecs, font dependencies, SVG rendering, and bitmap-locking paths that cannot be treated as portable modern-.NET behavior.
- Machine session lifecycle: normal close, cancellation, transport loss, crash/restart recovery, and competing access to the same serial device or network endpoint.
- Additional legacy screens and flows, including `WiFiConfigurator/OrturWiFiConfig*`, splash/license/save-option/laser-selector/input-box replacement needs.
- Existing test project status and runnable tests.
- Linux build/test command results.

## Out of Scope
- Do not change production code.
- Do not retarget projects.
- Do not add new app projects.
- Do not fix build failures in this task.

## Implementation Requirements
- Create or update a concise inventory document at `docs/linux-port-inventory.md`.
- Create or update `docs/linux-port-plan-coverage.md` with a legacy-to-port task coverage matrix.
- Include exact commands run and important output snippets.
- Include a prioritized blocker list grouped by build system, UI, platform APIs, serial/native dependencies, external tools, persistence, localization/resources, logging/diagnostics, privacy/update behavior, safety/legal flows, rendering, and packaging.
- Include a recommended execution order that matches the numbered prompt tasks.
- The coverage matrix must mark every major legacy area as covered, partially covered, missing, required, or intentionally deferred.
- The coverage matrix must include a required 3D/SharpGL section for `SharpGL`, `GrblPanel3D`, `Obj3D`, OpenGL hosting, preview interactions, and fallback/error behavior.
- Use local repository inspection as the source of truth.

## Tests
Run these commands and record the results:
- `dotnet --info`
- `dotnet build LaserGRBL.sln --no-restore`
- `find . -maxdepth 3 -type f \( -name '*.sln' -o -name '*.csproj' -o -name 'Directory.Build.*' -o -name 'global.json' \) -print`
- `rg -n "DllImport|Registry|ManagementObject|SerialPort|GetPortNames|Process\.Start|Application\.ExecutablePath|netsh\.exe|avrdude\.exe|autotrace\.exe|CH341SER|BinaryFormatter|System\.Windows\.Forms|System\.Drawing|SharpGL|GrblPanel3D|Obj3D|OpenGL|UsageStats|Telegram|UrlManager|WebClient|SoundPlayer|ResourceManager|CultureInfo|MaterialDB|StandardMaterials|StandardButtons|SafetyCountdown|LegalDisclaimer|Logger|ComLogger|OrturWiFiConfig|InputBox|LaserLifeCounter|UsageStats\.bin" LaserGRBL LaserGRBL.Tests`

If `dotnet build` fails because the SDK writes to a read-only home directory, rerun it with `DOTNET_CLI_HOME=/tmp/dotnet-home`.

## Checkpoint Report
Create `docs/checkpoints/00-port-inventory-and-baseline.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/00-inventory-baseline`, open a pull request to protected `master`, and do not push directly to `master`.

After tests/checks are complete and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 00: Port inventory and baseline`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- `docs/linux-port-inventory.md` exists and covers every scope item.
- `docs/linux-port-plan-coverage.md` exists and maps legacy areas to port tasks, including required 3D/SharpGL parity.
- The inventory explicitly identifies follow-up coverage for Tasks 17-22 where localization, user data compatibility, logging/diagnostics, safety/legal, privacy/update behavior, or SharpGL/feature parity need separate validation.
- Required commands were run or have a concrete documented blocker.
- The checkpoint exists and contains a comprehensive execution report.
- A task-specific commit exists and has been pushed.
