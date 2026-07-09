# Task 15: Sound, Update, And Linux Packaging

## Goal
Implement Linux sound playback, update notification behavior, and Linux packaging for the Avalonia port.

## Context
The legacy app uses `.wav` paths and Windows-oriented updater elevation. Linux should play sounds through available platform mechanisms, notify users about updates without privilege escalation, and produce an installable/packageable artifact.

## Scope
- Implement `ISoundService` for Linux with graceful fallback when audio is unavailable.
- Replace updater install/elevation flow with release notification and open-download/open-release behavior.
- Add packaging for at least one Linux target format suitable for this repo, such as AppImage, tarball, `.deb`, or Flatpak manifest.
- Document runtime dependencies such as serial permissions, `avrdude`, autotrace, and audio backend.

## Out of Scope
- Do not implement an auto-updater that writes privileged locations.
- Do not require every Linux package format.
- Do not change core GRBL behavior.

## Implementation Requirements
- Sound playback must not crash if sound files or audio devices are missing.
- Update checks must be cancellable and must not block startup.
- Packaging must include required assets and document optional external tools.
- Package build should be reproducible from documented commands.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Sound missing-file fallback.
- Sound disabled setting.
- Update available/not available/failure results using fake HTTP or fake update service.
- Package metadata validation where practical.

Run the package build command if required tooling is installed. If tooling is missing, document exact missing tools and verify all generated metadata/scripts.

## Checkpoint Report
Create `docs/checkpoints/15-sound-update-and-linux-packaging.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 15: Sound update and Linux packaging`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Sound and update services are implemented with fallbacks.
- At least one Linux packaging path exists and is documented.
- The checkpoint exists, and the commit has been pushed.
