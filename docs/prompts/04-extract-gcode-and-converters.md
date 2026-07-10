# Task 04: Extract G-Code And Converters

## Goal
Move G-code parsing/generation and import/conversion logic into UI-independent projects with tests.

## Context
LaserGRBL contains G-code file handling, SVG conversion, raster conversion, dithering, Hershey text, Clipper, potrace/autotrace integration, and related helpers mixed with WinForms forms and `System.Drawing`.

## Scope
- Extract UI-independent G-code file parsing, bounds calculation, job metadata, and command models.
- Preserve file classification and routing for `.nc`, `.cnc`, `.tap`, `.gcode`, `.ngc`, raster images, SVG, and LaserGRBL `.lps` projects without embedding file dialogs in core.
- Extract SVG-to-G-code conversion logic and preserve color filtering behavior.
- Extract raster conversion and dithering logic where possible.
- Keep external process integration, such as autotrace, behind platform contracts.
- Move existing `BezierToolsTests` to the Linux-port test project.

## Out of Scope
- Do not implement Avalonia import dialogs.
- Do not rewrite conversion algorithms unless needed for buildability.
- Do not remove legacy UI forms.

## Implementation Requirements
- `LaserGRBL.Core` must contain algorithmic logic that can run headlessly.
- Any remaining `System.Drawing` usage must either be isolated behind a compatibility package/platform adapter or documented as a follow-up blocker.
- Preserve existing default GRBL header/footer/pass behavior.
- Keep conversion outputs stable where existing tests or sample files can verify them.
- Keep append-versus-replace job composition explicit in the extracted API, including conversion imports that add generated G-code to an existing job.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Existing Bezier tools.
- G-code bounds and line parsing.
- SVG color filter parsing.
- At least one raster/dithering algorithm if extracted in this task.
- Missing autotrace dependency result if platform contract is touched.
- Supported file-extension routing and append behavior.

## Checkpoint Report
Create `docs/checkpoints/04-extract-gcode-and-converters.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/04-gcode-converters`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 04: Extract G-code and converters`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Extracted conversion logic builds and is covered by tests.
- UI-specific conversion forms remain outside core.
- The checkpoint exists, and the commit has been pushed.
