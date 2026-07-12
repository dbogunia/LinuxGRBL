# Image And GDI Compatibility

Task 15B removes undocumented production reliance on `System.Drawing.Common` from the Linux-port projects. The legacy WinForms project remains a reference source only.

## Linux-Port Inventory

| Project | System.Drawing/GDI Use | Decision |
| --- | --- | --- |
| `LaserGRBL.Core` | none before Task 15B | Add SkiaSharp-backed image abstraction for raster decode/conversion. |
| `LaserGRBL.Platform` | none | Keep platform services image-free. |
| `LaserGRBL.Avalonia` | Avalonia brushes, pens, drawing context, Skia-backed UI through Avalonia | Supported UI rendering path; not a conversion backend. |
| `LaserGRBL.Linux.Tests` | none | Tests create fixtures through SkiaSharp, not GDI. |

## Legacy GDI Inventory

The legacy `LaserGRBL/` project contains broad GDI/WinForms image usage:

- WinForms painting and preview: `PreviewForm`, `ConnectLogForm`, custom controls and designer files.
- Raster conversion and image manipulation paths under `RasterConverter`, `CsPotrace`, `SvgConverter`, and related import dialogs.
- SVG icon rendering in `SvgIcons/SvgIcons.cs`.
- Generator forms and resources with embedded `System.Drawing.Bitmap`, `System.Drawing.Icon`, `System.Drawing.Size`, `System.Drawing.Point`, and `System.Drawing.Font` values.
- Font-dependent UI/dialog resources and labels using Windows-era font names such as Microsoft Sans Serif.

Decision: legacy GDI code remains isolated in the legacy project and is not a Linux runtime dependency. Ported Linux conversion paths must use `LaserGRBL.Core.Raster` abstractions or a documented replacement.

## Selected Backend

SkiaSharp 3.119.4 is the selected Linux image backend because Avalonia already uses SkiaSharp in the Linux UI stack and the native Linux assets are already part of the restored package graph.

Task 15B adds:

- `IImageDecoder`
- `SkiaImageDecoder`
- `DecodedImage`
- `RgbaPixel`
- `RasterImageConverter`
- `FontFallbackResolver`

The decoder supports the legacy-routed raster extensions currently recognized by `GCodeFileRouter`: BMP, PNG, JPEG/JPG, and GIF.

## Compatibility Behavior

- Alpha is composited on white before grayscale conversion. This is deterministic and avoids platform-specific GDI alpha behavior.
- Grayscale uses the common weighted formula `0.299 R + 0.587 G + 0.114 B`.
- Dithering continues to use the existing `BayerDither` ordered matrix.
- Unsupported file extensions return `OperationResult` failures.
- Corrupt or undecodable images return actionable `OperationResult` failures instead of throwing.
- Non-ASCII file paths are supported through normal .NET file APIs and covered by tests.
- Missing legacy fonts resolve deterministically to `DejaVu Sans`, then the first available family, then `sans-serif`.

## Deliberate Differences

- Linux does not use GDI+ codec discovery, bitmap locking, or Windows font enumeration.
- Pixel-perfect byte parity with legacy GDI is not claimed. Fixture tests protect stable domain output: dimensions, decode success/failure, alpha-to-grayscale behavior, and deterministic dither output.
- SVG geometry remains handled by the existing UI-independent SVG helpers. Full SVG rendering/text-to-path parity remains part of later feature-parity validation.

## Release Notes

`System.Drawing.Common` must not be introduced as a production Linux conversion dependency. Any future image path added to Linux projects should depend on `IImageDecoder`, `RasterImageConverter`, Avalonia UI rendering, or another explicitly supported backend with tests and notices.
