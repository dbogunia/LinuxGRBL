# Task 15A: Linux CI And Support Matrix

## Goal

Make the Linux port reproducibly buildable and testable, with an explicit support contract for runtime, architecture, display stack, and graphics driver capabilities.

## Context

The port will replace a .NET Framework WinForms application with a .NET 8 Avalonia application and an OpenGL preview. A successful developer-machine build is not evidence that a distributable Linux application works across the environments it claims to support.

## Scope

- Create `docs/linux-support-matrix.md` defining supported .NET version, Linux distributions or baseline, `linux-x64` and any supported `linux-arm64` target, display servers (Wayland/X11), and minimum tested OpenGL/Mesa capability.
- Add CI configuration that restores, builds, and tests `LaserGRBL.Linux.sln` on a clean Linux runner.
- Make package/metadata validation part of CI where package tooling is available; otherwise validate scripts and manifests without publishing an artifact.
- Add a documented, repeatable headless UI/renderer smoke-test strategy and state which checks require a graphical runner or manual hardware.
- Pin SDK and package versions sufficiently to make CI results reproducible, and fail clearly when an unsupported runtime environment is selected.

## Out Of Scope

- Do not claim support for an architecture or distribution that is not built and tested.
- Do not require physical laser hardware in ordinary CI.
- Do not publish releases from this task.

## Implementation Requirements

- CI must run restore, build, and tests from a fresh checkout without using the legacy .NET Framework project as a build target.
- The support matrix must distinguish supported, tested-but-not-supported, and untested environments.
- The renderer smoke test must report a useful diagnostic when no usable display/OpenGL implementation exists; it must not silently pass because rendering was skipped.
- Document how maintainers reproduce the CI commands locally.

## Tests

- Run the exact CI restore, build, test, and package-metadata commands locally where possible.
- Run at least one headless UI or renderer smoke test, or record the precise host limitation and validate its non-graphical fallback path.
- Confirm that every CI command targets `LaserGRBL.Linux.sln`, not the legacy solution.

## Checkpoint Report

Create `docs/checkpoints/15a-linux-ci-and-support-matrix.md` with the support matrix, CI evidence, unavailable environment checks, remaining risks, and completion status.

## Acceptance Criteria

- A support matrix and reproducible Linux CI workflow exist.
- CI validates build and tests from a clean Linux environment.
- Graphics/display limitations and unsupported architectures are explicit rather than implicit release risks.
