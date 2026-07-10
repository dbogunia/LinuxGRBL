# Task 13: Avalonia Preview Renderer

## Goal
Rebuild job preview rendering for Avalonia while preserving existing LaserGRBL preview behavior.

## Context
The legacy preview uses WinForms controls, `System.Drawing`, and SharpGL/WinForms rendering. The Linux port needs a preview renderer that works in Avalonia and can be validated on Linux.

3D/OpenGL preview parity is required for final Linux-port completeness. This task may implement the reliable 2D/Avalonia preview foundation first, but it must not close the overall preview parity story without the dedicated 3D/OpenGL work in Task 13B.

## Scope
- Implement an Avalonia preview component and rendering model.
- Render G-code/job bounds, path lines, progress, cursor/machine position, grid/rulers, and color schemes.
- Support zoom, pan, auto-fit, and coordinate inspection.
- Integrate with the main workflow view-model.
- Define renderer interfaces and scene models that allow the required 3D/OpenGL renderer from Task 13B to plug in without rebuilding the main workflow UI.

## Out of Scope
- Do not implement final 3D/OpenGL parity in this task; Task 13B owns that work.
- Do not port unrelated dialogs.
- Do not rewrite G-code parsing algorithms.

## Implementation Requirements
- Start with Avalonia drawing/canvas for reliability.
- Treat 2D preview as an interim-compatible renderer, not a replacement for required 3D/SharpGL parity.
- Add a preview renderer boundary such as `IJobPreviewRenderer`, a UI-independent `PreviewSceneModel`, and interaction state for zoom/pan/progress that Task 13B can extend for rotation/camera behavior.
- Keep renderer state separate from UI input handling where possible.
- Ensure text and controls do not overlap at normal desktop sizes.
- Renderer must show a nonblank preview for a sample G-code file.
- Consume the complete named ColorScheme semantic palette used by legacy preview, command, and log rendering; do not reduce it to generic light/dark colors.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Render model bounds calculation.
- Zoom/pan transformations.
- Progress path segmentation.
- Empty/no-file render state.
- Nonblank preview smoke test if screenshot/canvas testing is feasible.
- Renderer interface can be exercised with fake scene data and does not depend on WinForms or SharpGL WinForms controls.
- Each named color scheme maps its preview semantic colors into the render model.

Document any manual screenshot validation performed.

## Checkpoint Report
Create `docs/checkpoints/13-avalonia-preview-renderer.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/13-preview-2d`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 13: Avalonia preview renderer`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Avalonia preview renders useful job geometry and supports expected interactions.
- The implementation leaves a clear, tested extension point for Task 13B 3D/OpenGL rendering.
- Required renderer tests pass.
- The checkpoint exists, and the commit has been pushed.
