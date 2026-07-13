# Task 17: Localization and resx migration

## Summary

Task 17 adds a Linux-port localization boundary for Avalonia-visible text and documents how legacy WinForms `.resx` resources are handled.

## Implemented changes

- Added `LocalizationCatalog` with deterministic culture fallback and missing-key tracking.
- Routed Avalonia shell diagnostics, status text, log messages, and dialog/tool group labels through the catalog.
- Added persisted `PortSettings.Language` with settings schema version 2.
- Updated `JsonSettingsStore` to accept older schemas and normalize them to the current settings shape.
- Documented the `.resx` inventory and intentional non-text resource skips.

## Resource policy

Migrated text scope covers the current Avalonia shell, main workflow shell text, diagnostics, and dialog/tool hub labels.

Non-text WinForms resources are intentionally not migrated into the localization catalog:

- `System.Drawing.Color`, fonts, points, sizes, and layout metadata;
- serialized WinForms designer/control metadata;
- `Bitmap`, `Icon`, image list, and toolbar resources;
- legacy SharpGL/WinForms host resources.

Linux replacements live in Avalonia assets, packaging assets, SkiaSharp raster support, and the ported preview renderers.

## Tests

Verification completed:

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln -m:1`
  - Result: pass, 0 warnings, 0 errors.
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln -m:1 /nr:false`
  - Result: pass, 199/199 tests.

Coverage added:

- culture fallback order;
- missing key detection;
- Polish string lookup with English fallback;
- persisted language selection;
- old schema normalization;
- localized Avalonia shell/tool labels;
- documented non-text `.resx` policy.

## Remaining risks

- Polish catalog is intentionally partial; unsupported keys fall back to English.
- Broad legacy WinForms form translations are not ported until the matching Avalonia screens exist.
- Manual UI smoke for language switching remains future work when settings UI exposes the selector.
- Existing release blockers remain: real nonblank OpenGL/GPU validation, real USB GRBL hardware validation, and clean-install hardware smoke.

## Completion status

Implementation commit:

- `f450a60` - `Task 17: Localization and resx migration`

Completion status:

- Local implementation complete.
- Build and tests pass.
- Branch push, PR link, and CI result are recorded in the PR handoff.
