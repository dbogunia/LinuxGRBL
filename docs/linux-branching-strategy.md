# Linux Port Branching Strategy

## Protected Integration Branch

`master` is the protected integration branch. It must remain buildable with `LaserGRBL.Linux.sln` and must not receive direct pushes. Every change reaches it through a pull request with one approval and required CI checks.

## Task Branches

Create one short-lived branch per port task from an up-to-date `master`:

```text
feature/<task-id>-<short-slug>
```

Examples: `feature/03-core-streaming`, `feature/13b-opengl-preview`, and `feature/15d-safe-shutdown`.

Each task branch contains only the task scope, its tests, and its checkpoint. Rebase or merge the current `master` before requesting review; resolve conflicts in the task branch. Do not stack dependent task branches unless the prerequisite cannot be merged first; if stacking is unavoidable, state the dependency explicitly in the pull request.

Independent work after Tasks 01-02 may proceed in parallel, but dependent work follows the numbered port plan. In particular, merge platform/core contracts before UI workflows, and merge all required implementation work before end-to-end validation.

## Pull Requests And Releases

- Push task branches and open a PR to `master`; use a task-specific title and link the checkpoint.
- CI must validate the Linux-only solution. A failing, skipped, or missing required check blocks merge.
- Delete the task branch after merge.
- Create `release/linux-v<version>` from `master` only after Task 16. Allow only release-blocking fixes, validation, packaging, and documentation there.
- Create `hotfix/<short-slug>` from the released tag for urgent production fixes; merge the approved fix back to `master` and any active release branch.
- Tag `v0.1.0-linux-mvp` after Task 16 and `v1.0.0-linux` only after Task 22 closes all `required` matrix entries.
