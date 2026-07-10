# Task 15: Sound, Update, And Linux Packaging

## Goal
Implement Linux sound playback, update notification behavior, and Linux packaging for the Avalonia port.

## Context
The legacy app uses `.wav` paths and Windows-oriented updater elevation. Linux should play sounds through available platform mechanisms, notify users about updates without privilege escalation, and produce an installable/packageable artifact.

Detailed privacy policy, telemetry behavior, update opt-in/opt-out decisions, and external network-call auditing are handled in Task 21. This task owns the technical sound, update-notification, and packaging mechanisms.

## Scope
- Implement `ISoundService` for Linux with graceful fallback when audio is unavailable.
- Replace updater install/elevation flow with release notification and open-download/open-release behavior.
- Add packaging for at least one Linux target format suitable for this repo, such as AppImage, tarball, `.deb`, or Flatpak manifest.
- Document runtime dependencies such as serial permissions, `avrdude`, autotrace, and audio backend.
- Inventory shipped managed/native dependencies and bundled tools, preserve required third-party license/NOTICE material, and document artifact provenance and integrity verification for the selected package path.

## Out of Scope
- Do not implement an auto-updater that writes privileged locations.
- Do not require every Linux package format.
- Do not change core GRBL behavior.
- Do not introduce telemetry or startup-blocking network checks; Task 21 defines the privacy and update policy.

## Implementation Requirements
- Sound playback must not crash if sound files or audio devices are missing.
- Update checks must be cancellable and must not block startup.
- Update-notification implementation must expose enough service boundaries for Task 21 to enforce privacy and opt-in/opt-out policy.
- Packaging must include required assets and document optional external tools.
- Package build should be reproducible from documented commands.
- Package inputs must use pinned or recorded versions; generated artifacts must include version/build identity and a published checksum or equivalent integrity-verification procedure.
- Do not ship vendored code or binaries without an explicit license/NOTICE decision.

## Tests
Run:
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln`

Add tests for:
- Sound missing-file fallback.
- Sound disabled setting.
- Update available/not available/failure results using fake HTTP or fake update service.
- Package metadata validation where practical.
- Dependency/license inventory and artifact checksum/provenance validation.

Run the package build command if required tooling is installed. If tooling is missing, document exact missing tools and verify all generated metadata/scripts.

## Checkpoint Report
Create `docs/checkpoints/15-sound-update-and-linux-packaging.md` with summary, implemented changes, tests run, test evidence, git commit/push details, remaining risks, and completion status.

## Commit And Push
Follow the [Linux Port Branching Strategy](../linux-branching-strategy.md): work in `feature/15-packaging-sound`, open a pull request to protected `master`, and do not push directly to `master`.

After tests pass and the checkpoint is written:
- Run `git status --short`.
- Stage only files changed for this task.
- Commit with message: `Task 15: Sound update and Linux packaging`.
- Push the current branch to its configured upstream.
- Record commit hash and push result in the checkpoint.

## Acceptance Criteria
- Sound and update services are implemented with fallbacks.
- Update-notification behavior is technically implemented without privileged auto-update or hidden telemetry.
- At least one Linux packaging path exists and is documented.
- Shipped dependencies, license notices, and artifact-integrity verification are documented.
- The checkpoint exists, and the commit has been pushed.
