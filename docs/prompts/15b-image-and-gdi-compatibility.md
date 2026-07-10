# Task 15B: Image And GDI Compatibility

## Goal

Remove production reliance on unsupported `System.Drawing`/GDI+ behavior in the Linux port while preserving conversion and preview output semantics.

## Context

Legacy image, SVG, font, and raster conversion code uses `System.Drawing` and `System.Drawing.Drawing2D` broadly. In modern .NET on Linux, this cannot be treated as a supported production dependency merely because it appears to work on one machine.

## Scope

- Inventory every Linux-port code path that requires `System.Drawing`, GDI+, WinForms image types, font enumeration, image codecs, or bitmap locking.
- Define and implement a supported image abstraction/back-end for each ported path (for example SkiaSharp or ImageSharp), keeping UI rendering separate from conversion algorithms.
- Explicitly decide the fate of unported GDI-dependent legacy code: migrate, isolate in the legacy project, replace, or declare unsupported.
- Add golden/fixture tests for raster conversion, SVG geometry, size calculation, colour/alpha handling, and image codec failures.
- Document font fallback and deterministic handling when a legacy font is unavailable on Linux.

## Out Of Scope

- Do not redesign raster/SVG algorithms or silently change generated G-code semantics.
- Do not leave `System.Drawing` as an undocumented Linux runtime prerequisite.

## Implementation Requirements

- New Linux-port projects must not use `System.Drawing.Common` as their production image-rendering/conversion implementation.
- Fixture tests must compare stable domain output or approved tolerances, not platform-specific bitmap bytes alone.
- Unsupported source formats, corrupt images, missing codecs, and unavailable fonts must produce actionable user-facing failures without crashing the app.
- Record any deliberate visual difference and its impact in the compatibility documentation.

## Tests

- Run conversion tests on Linux for representative BMP, PNG, JPEG, GIF, and SVG inputs supported by the legacy application.
- Test transparent images, non-ASCII paths, absent fonts, corrupt files, and a deterministic golden conversion.
- Run `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`.

## Checkpoint Report

Create `docs/checkpoints/15b-image-and-gdi-compatibility.md` with the GDI inventory, selected back-end, fixture evidence, compatibility differences, and completion status.

## Acceptance Criteria

- Each ported image path has a supported Linux implementation or an explicit user-visible support decision.
- Conversion behavior is protected by Linux-runnable fixtures.
- The final port does not depend on undocumented GDI+ behavior.
