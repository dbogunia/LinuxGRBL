# Task 23 Checkpoint: OpenGL Render Validation

Validated the Avalonia/OpenGL 3D preview on a real Linux display/GPU and captured repeatable local evidence.

## Implemented Changes

- Replaced the immediate-mode `glBegin`/`glEnd` preview draw path with a shader/VBO/VAO path that works with the core-profile OpenGL context exposed by Avalonia on the validation host.
- Added optional OpenGL diagnostics through `LASERGRBL_OPENGL_DIAGNOSTICS_PATH`; preview diagnostics are best-effort and never affect rendering.
- Added `scripts/validate-opengl-preview.sh` and `scripts/validate-opengl-preview.py` to launch the published app with sample G-code, capture the LaserGRBL window, crop the 3D preview, and check nonblank pixel thresholds.
- Updated readiness and parity docs to close the real GPU/display nonblank render blocker while keeping USB GRBL hardware and clean-install serial hardware validation as release blockers.

## Validation Evidence

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet publish LaserGRBL.Avalonia/LaserGRBL.Avalonia.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/publish/linux-x64 --no-restore`
- `scripts/validate-opengl-preview.sh linux-x64`

Pixel report from `artifacts/opengl-validation/pixel-report.txt`:

```text
window=1180x760
preview_crop=416x358+750+98
unique_colors=13
non_background_ratio=0.999993
non_dominant_ratio=0.014000
colored_ratio=0.994870
bright_ratio=0.012798
dark_ratio=0.000000
status=pass
```

OpenGL diagnostics from `artifacts/opengl-validation/opengl-diagnostics.log` recorded:

- Avalonia OpenGL context initialized with `GlVersion { Type = OpenGL, Major = 4, Minor = 0, IsCompatibilityProfile = False }`.
- Avalonia OpenGL rendered frames.

Host `glxinfo -B` evidence:

- Direct rendering: yes.
- OpenGL vendor: NVIDIA Corporation.
- OpenGL renderer: NVIDIA GeForce GTX 1050 Ti/PCIe/SSE2.
- OpenGL core profile version: 4.6.0 NVIDIA 535.309.01.

## Remaining Risks

- Release blocker: real USB GRBL hardware validation.
- Release blocker: clean-install package/device validation with serial hardware.
- High: detailed manual 3D interaction parity and legacy SharpGL behavior comparison are still pending.
- High: real firmware flashing, WiFi configuration, and audio behavior still require safe hardware/environment validation.

## Completion Status

Task 23 closes the real nonblank Avalonia/OpenGL render blocker on the validation host. The Linux port remains not release-ready until the serial hardware and clean-install hardware blockers are closed.
