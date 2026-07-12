# Task 13 Checkpoint: Avalonia Preview Renderer

## Summary

Implemented the interim Avalonia 2D preview foundation with UI-independent render models, transform state, progress segmentation, semantic color mapping, and a custom Avalonia drawing control integrated with the main workflow.

## Implemented Changes

- Added `IJobPreviewRenderer`, `GCodePreviewRenderer`, `PreviewSceneModel`, `PreviewRenderStyle`, `PreviewInteractionState`, and viewport transform helpers as the renderer boundary for Task 13B to extend.
- Added `JobPreviewControl`, an Avalonia `Control` that draws preview background, grid, remaining path, completed path, machine cursor, and status text.
- Integrated preview state into `MainWorkflowViewModel`; loading/reopening/appending G-code rebuilds the preview scene, running a job updates progress, and machine-position changes refresh the cursor.
- Added preview controls for zoom in/out, pan, and auto-fit in the main window.
- Mapped each semantic color scheme into preview-specific render style values, including background, grid, path, completed path, cursor, text, and border.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 133 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Added tests for render-model bounds, zoom/pan coordinate transforms, progress segmentation, empty/no-file state, renderer-interface use without WinForms or SharpGL controls, semantic color-scheme mapping, preview-control smoke construction, and workflow-to-preview integration.

## Manual Validation

No manual GUI screenshot was captured in this headless run. The Avalonia project builds the XAML/control, and tests exercise the renderer boundary, scene state, transform math, and control smoke path.

## Remaining Risks

- This is intentionally a reliable 2D/Avalonia preview foundation, not final 3D/OpenGL parity. Task 13B remains required for SharpGL/3D replacement parity.
- Arc rendering is approximated as linear endpoint movement at the render-model layer; richer legacy parity belongs with the later preview parity work.
- Pixel-level nonblank screenshot validation is not yet automated because the current environment is headless.

## Commit And Push

Implementation commit: `adc1e06` (`Task 13: Avalonia preview renderer`)

Push: complete on `feature/13-preview-2d`

## Completion Status

Complete for Task 13 implementation and PR handoff.
