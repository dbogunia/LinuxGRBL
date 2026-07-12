# Task 13B Checkpoint: Avalonia 3D OpenGL Renderer

## Summary

Added the Avalonia-side 3D preview model, camera state, OpenGL host boundary, failure diagnostics, visible fallback renderer, and follow-up immediate OpenGL draw backend without WinForms or SharpGL WinForms controls.

## Implemented Changes

- Added `Preview3DSceneModel`, 3D lines, bounds, camera state, projection helpers, 3D semantic style mapping, and `Preview3DSceneBuilder`.
- Added `JobPreview3DControl` based on Avalonia `OpenGlControlBase`; it tracks OpenGL lifecycle status and exposes clear fallback diagnostics when no active context is available.
- Added `OpenGlImmediatePreviewRenderer`, which resolves required GL entry points from Avalonia `GlInterface.GetProcAddress` and renders axes, remaining path, completed path, and machine cursor from `OnOpenGlRender`.
- Added defensive failure reporting when required GL functions are unavailable, so core-profile or broken-context failures surface in the existing 3D/OpenGL fallback diagnostic.
- Integrated the 3D scene, camera, status, zoom, rotate, tilt, and reset controls into `MainWorkflowViewModel` and the main preview UI.
- Kept the Task 13 2D renderer as the usable fallback while 3D scene state remains available.
- Audited the legacy `GrblPanel3D` and `Obj3D` path: the old host is tightly coupled to WinForms, `System.Drawing`, SharpGL display lists, custom drawing threads, and Windows-era render-context providers, so it was not copied directly.

## Test Evidence

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 140 tests outside the sandbox for the original PR #16 partial checkpoint. The sandboxed VSTest runner remains blocked by local socket permission.

Added tests for 3D scene generation from G-code, bounds, camera rotation/zoom/pan projection, progress segmentation, empty/no-file state, OpenGL initialization failure path through an injectable context factory, 3D preview color mapping for each named scheme, and 3D control smoke construction.

Follow-up draw-backend verification:

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1` completed with zero warnings/errors after adding the GL draw backend.

Added tests for immediate renderer draw-call emission against a fake `IOpenGlApi` and missing required GL function diagnostics.

`DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false` passed 142 tests outside the sandbox after the draw-backend follow-up. The sandboxed VSTest runner still aborts with `SocketException (13): Permission denied`.

## Manual Validation

No GPU/display manual validation was performed in this headless run. The Avalonia OpenGL host type compiles and the draw backend is wired through `OnOpenGlRender`, but actual Linux OpenGL context creation and nonblank GPU rendering still require a display/GPU session.

## Remaining Risks

- Release blocker: rotate, pan, zoom, progress, machine position, and color state are modeled and rendered through the immediate GL backend, but must still be validated against a real Linux OpenGL context and compared with legacy `GrblPanel3D`.
- The follow-up renderer uses compatibility-profile immediate OpenGL calls (`glBegin`/`glEnd`) to complete the first real draw backend quickly. If Avalonia provides only a core-profile context on a target Linux GPU, the fallback diagnostic should trip and a shader/buffer-backed renderer will be required.
- Legacy SharpGL display-list rendering was not ported directly because it depends on WinForms/SharpGL-specific infrastructure.

## Commit And Push

Implementation commit: `1d36e1d` (`Task 13B: Avalonia 3D OpenGL renderer`)

Push: complete on `feature/13b-opengl-preview`

Follow-up draw backend branch: `feature/13b-opengl-draw-backend`

## Completion Status

Task 13B now has a real Avalonia OpenGL draw backend in addition to the tested 3D scene/camera/fallback/OpenGL-host boundary. Final parity remains release-blocking until manual Linux GPU validation confirms nonblank rendering and legacy behavior parity.
