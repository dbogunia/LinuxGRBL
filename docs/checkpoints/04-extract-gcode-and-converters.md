# Task 04 Checkpoint: Extract G-code And Converters

## Summary

Extracted Linux-runnable, UI-independent G-code/job/import primitives and selected SVG/raster algorithms into `LaserGRBL.Core`.

## Implemented Changes

- G-code extension routing, line parsing, XY bounds, append/replace composition, header/passes/footer rendering, and asynchronous G-code import results.
- SVG color-filter parser without `System.Drawing`, including style/presentation attributes and legacy color thresholds.
- Bezier flattening with a Core-owned SVG point type rather than WPF `Point`.
- Headless ordered Bayer dithering over 8-bit grayscale matrices.

## Test Evidence

`dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed without warnings/errors. `dotnet test LaserGRBL.Linux.sln --no-build` passed 85 tests.

## Deliberate Follow-up Boundaries

- Full SVG rendering/path conversion and bitmap raster processing still use legacy `System.Drawing` dependencies; Task 15B selects the supported image backend before they are migrated.
- Autotrace/process integration is assigned to Task 09.
- Avalonia import/options dialogs are assigned to Task 14.
- `.lps` compatibility and migration are assigned to Tasks 18 and 22.

## Completion Status

Complete for the headless extraction enabled by the current image boundary. Remaining GDI-dependent conversion paths are explicitly documented rather than copied into the Linux Core.
