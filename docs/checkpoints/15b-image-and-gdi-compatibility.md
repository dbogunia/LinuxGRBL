# Task 15B Checkpoint: Image And GDI Compatibility

## Summary

Added a supported Linux image compatibility boundary for the ported projects and documented the remaining legacy GDI surface. Linux production image decode/conversion now uses SkiaSharp instead of `System.Drawing.Common`.

## Implemented Changes

- Added centrally pinned `SkiaSharp` and `SkiaSharp.NativeAssets.Linux` `3.119.4` package versions.
- Added `LaserGRBL.Core.Raster` image compatibility abstractions:
  - `IImageDecoder`
  - `SkiaImageDecoder`
  - `DecodedImage`
  - `RgbaPixel`
  - `RasterImageConverter`
  - `FontFallbackResolver`
- Added Linux-runnable fixture tests for:
  - PNG, JPEG, GIF, and BMP decode.
  - Transparent alpha compositing on white.
  - Deterministic Bayer dither output.
  - Non-ASCII image paths.
  - Corrupt image failures.
  - Unsupported format failures.
  - Deterministic legacy font fallback.
- Documented the GDI inventory and compatibility decisions in `docs/image-gdi-compatibility.md`.
- Updated Linux package notices and manifest to include SkiaSharp native assets as a required bundled dependency.

## GDI Inventory

Linux-port projects do not use `System.Drawing.Common` as a production image backend.

- `LaserGRBL.Core`: no previous GDI dependency; Task 15B adds a SkiaSharp-backed raster boundary.
- `LaserGRBL.Platform`: no image or GDI dependency.
- `LaserGRBL.Avalonia`: uses Avalonia drawing APIs and Avalonia's Skia-backed UI path; this is UI rendering, not conversion logic.
- `LaserGRBL.Linux.Tests`: uses SkiaSharp only for deterministic test fixture generation.

The legacy `LaserGRBL/` WinForms project still contains broad GDI usage in preview forms, raster conversion, SVG icon paths, generator forms, resources, font resources, and designer-generated code. That code remains isolated in the legacy project and is not a Linux runtime dependency.

## Selected Backend

SkiaSharp `3.119.4` is the selected Linux image backend because Avalonia already depends on SkiaSharp for the Linux UI stack, native Linux assets are available in the restore graph, and it avoids unsupported `System.Drawing.Common` behavior on Linux.

Supported raster decode extensions for ported Linux paths:

- `.bmp`
- `.png`
- `.jpg`
- `.jpeg`
- `.gif`

## Compatibility Decisions

- Alpha is composited on white before grayscale conversion.
- Grayscale uses `0.299 R + 0.587 G + 0.114 B`.
- Dithering uses the existing `BayerDither` ordered matrix.
- Unsupported extensions return `OperationResult` failures.
- Corrupt files return actionable decode failures instead of throwing.
- Non-ASCII paths use normal .NET file APIs and are covered by tests.
- Missing legacy fonts resolve to `DejaVu Sans`, then the first installed family, then `sans-serif`.
- Pixel-perfect legacy GDI bitmap parity is not claimed; tests protect stable domain behavior and approved tolerances.

## Test Evidence

Local commands run:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore LaserGRBL.Linux.sln`

Passed outside the sandbox after adding SkiaSharp package references.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1`

Completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

Passed 175 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Package verification:

`scripts/build-linux-tarball.sh linux-x64 0.1.0`

Produced `artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz`.

`sha256sum -c artifacts/linuxgrbl-avalonia-0.1.0-linux-x64.tar.gz.sha256`

Returned `OK`.

## Remaining Risks

- Full legacy raster/SVG/font parity is not proven by Task 15B; this task establishes the supported Linux boundary and regression fixtures.
- SVG text-to-path and font rendering parity remain part of later feature-parity validation.
- Task 15B does not remove or rewrite legacy WinForms GDI code.
- Manual Linux GPU/display validation from Task 13B is still not verified and remains release-blocking until Task 16/22 records real graphical evidence.

## Commit And Push

Implementation branch: `feature/15b-image-gdi-compatibility`

Implementation commit: `4341f0a` (`Task 15B: Image and GDI compatibility`)

Push: pending until this checkpoint metadata is committed.

## Completion Status

Task 15B is implemented locally. The Linux port now has a documented, tested SkiaSharp raster compatibility boundary and does not rely on undocumented GDI+ behavior for production Linux image conversion.
