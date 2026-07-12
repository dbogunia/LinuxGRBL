# Task 13B Checkpoint: Avalonia 3D OpenGL Renderer

## Summary

Added the Avalonia-side 3D preview model, camera state, OpenGL host boundary, failure diagnostics, and a visible fallback renderer that reuses the Task 13 scene boundary without WinForms or SharpGL WinForms controls.

## Implemented Changes

- Added `Preview3DSceneModel`, 3D lines, bounds, camera state, projection helpers, 3D semantic style mapping, and `Preview3DSceneBuilder`.
- Added `JobPreview3DControl` based on Avalonia `OpenGlControlBase`; it tracks OpenGL lifecycle status and exposes clear fallback diagnostics when no active context is available.
- Integrated the 3D scene, camera, status, zoom, rotate, tilt, and reset controls into `MainWorkflowViewModel` and the main preview UI.
- Kept the Task 13 2D renderer as the usable fallback while 3D scene state remains available.
- Audited the legacy `GrblPanel3D` and `Obj3D` path: the old host is tightly coupled to WinForms, `System.Drawing`, SharpGL display lists, custom drawing threads, and Windows-era render-context providers, so it was not copied directly.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 140 tests outside the sandbox. The sandboxed VSTest runner remains blocked by local socket permission.

Added tests for 3D scene generation from G-code, bounds, camera rotation/zoom/pan projection, progress segmentation, empty/no-file state, OpenGL initialization failure path through an injectable context factory, 3D preview color mapping for each named scheme, and 3D control smoke construction.

## Manual Validation

No GPU/display manual validation was performed in this headless run. The Avalonia OpenGL host type compiles and tests exercise its scene/status path, but actual Linux OpenGL context creation and nonblank GPU rendering still require a display/GPU session.

## Remaining Risks

- Release blocker: full OpenGL draw-command backend is not complete yet. `JobPreview3DControl` hosts Avalonia OpenGL lifecycle but still uses the visible fallback drawing path for scene visualization.
- Release blocker: rotate, pan, zoom, progress, machine position, and color state are modeled and exposed, but must still be validated against a real Linux OpenGL context and compared with legacy `GrblPanel3D`.
- Legacy SharpGL display-list rendering was not ported directly because it depends on WinForms/SharpGL-specific infrastructure; a shader/buffer-backed Avalonia renderer or carefully isolated SharpGL replacement remains required before final parity can be claimed.

## Commit And Push

Implementation commit: pending

Push: pending

## Completion Status

Partial Task 13B implementation: tested 3D scene/camera/fallback/OpenGL-host boundary is complete, but final 3D/OpenGL parity remains release-blocking until the real GL draw backend and manual Linux validation are completed.
