# Task 13B: Avalonia 3D OpenGL Renderer

## Goal
Port the legacy 3D/OpenGL preview behavior to the Linux Avalonia port. 3D/SharpGL parity is required for final Linux-port completeness.

## Context
The legacy app includes SharpGL, `GrblPanel3D`, `Obj3D`, OpenGL render-context providers, and 3D preview interactions tied to WinForms controls. The Linux port must provide equivalent 3D preview functionality through an Avalonia-compatible OpenGL host or an equivalent OpenGL renderer.

## Scope
- Port or replace the required behavior from `GrblPanel3D`, `Obj3D`, and SharpGL-dependent preview code.
- Implement an Avalonia-compatible OpenGL preview host.
- Render job geometry, machine/cursor position, progress, bounds, grid/axes, and the complete named ColorScheme semantic palette.
- Support rotate, pan, zoom, auto-fit/reset view, and interaction state persistence where the legacy UI supports it.
- Integrate with the renderer boundary and scene model from Task 13.
- Provide a user-visible fallback/error state when OpenGL context creation fails.

## Out of Scope
- Do not use WinForms controls in the Avalonia project.
- Do not remove the 2D preview renderer if it is useful as a fallback.
- Do not rewrite G-code parsing unless required to build a UI-independent scene model.
- Do not mark 3D as optional or deferred for final parity.

## Implementation Requirements
- Prefer Avalonia OpenGL integration for context hosting; reuse SharpGL model/rendering code only where it is not WinForms-specific.
- Keep G-code/job geometry parsing in core or another UI-independent layer.
- Keep OpenGL host code in the Avalonia project.
- Any reused SharpGL code must be audited for `System.Windows.Forms`, `System.Drawing`, and Windows-only assumptions.
- If OpenGL cannot initialize, the UI must show a clear diagnostic and keep the app usable where possible.
- Missing 3D/OpenGL behavior is a release blocker for final parity and must be recorded as such in the checkpoint.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- 3D scene model generation from sample G-code.
- Bounds, transforms, camera/rotation state, and progress segmentation.
- Empty and invalid file render states.
- OpenGL host initialization failure path using a fake or injectable context factory.
- Nonblank 3D preview smoke test if screenshot/canvas testing is feasible.
- At least one semantic preview-color assertion for each supported named color scheme.

Manual or gated checks:
- Rotate, pan, zoom, auto-fit/reset, machine position, progress, and color/theme rendering on Linux.
- Compare required behavior against the legacy WinForms `GrblPanel3D`.
- Document GPU/headless limitations and exact failure output.

## Checkpoint Report
Create `docs/checkpoints/13b-avalonia-3d-opengl-renderer.md` with summary, implemented changes, tests run, test evidence, manual validation evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/13b-opengl-preview`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 13B: Avalonia 3D OpenGL renderer`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Avalonia has a working 3D/OpenGL preview path on Linux.
- The 3D renderer does not depend on WinForms controls.
- Required 3D interactions and render states are implemented or marked as release blockers with exact evidence.
- Required tests pass and manual/gated validation is documented.
- The checkpoint exists, and the commit has been pushed.
