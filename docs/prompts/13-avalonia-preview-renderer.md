# Task 13: Avalonia Preview Renderer

## Goal
Rebuild job preview rendering for Avalonia while preserving existing LaserGRBL preview behavior.

## Context
The legacy preview uses WinForms controls, `System.Drawing`, and SharpGL/WinForms rendering. The Linux port needs a preview renderer that works in Avalonia and can be validated on Linux.

## Scope
- Implement an Avalonia preview component and rendering model.
- Render G-code/job bounds, path lines, progress, cursor/machine position, grid/rulers, and color schemes.
- Support zoom, pan, auto-fit, and coordinate inspection.
- Integrate with the main workflow view-model.

## Out of Scope
- Do not require OpenGL acceleration in this task unless CPU/Avalonia drawing cannot meet basic acceptance criteria.
- Do not port unrelated dialogs.
- Do not rewrite G-code parsing algorithms.

## Implementation Requirements
- Start with Avalonia drawing/canvas for reliability.
- Keep renderer state separate from UI input handling where possible.
- Ensure text and controls do not overlap at normal desktop sizes.
- Renderer must show a nonblank preview for a sample G-code file.
- Keep color/theme inputs compatible with settings.

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

Document any manual screenshot validation performed.

## Checkpoint Report
Create `docs/checkpoints/13-avalonia-preview-renderer.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 13: Avalonia preview renderer`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Avalonia preview renders useful job geometry and supports expected interactions.
- Required renderer tests pass.
- The checkpoint exists, and the commit has been pushed.
