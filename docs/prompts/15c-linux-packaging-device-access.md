# Task 15C: Linux Packaging And Device Access

## Goal

Bind the chosen Linux package format to a tested device-access, desktop-integration, and runtime-dependency model.

## Context

Serial access is not portable merely because `/dev/ttyUSB*` can be enumerated. udev/group permissions, sandbox confinement, external tools, and desktop launch/file-opening behavior differ materially between a `.deb`, AppImage, tarball, and Flatpak.

## Scope

- Select the supported package format(s) and document their serial, USB, audio, OpenGL, external-tool, and host-file access model.
- Provide least-privilege user guidance and/or package assets for serial access, including stable `/dev/serial/by-id` selection and permission-denied remediation.
- If sandboxed packaging is selected, declare and test the required device/filesystem permissions; do not assume host serial access.
- Add desktop integration required by the selected format: `.desktop` entry, icons, MIME/file association and open-file behavior for supported files such as `.lps` where applicable.
- Define the runtime tool discovery/install policy for `avrdude` and `autotrace` per package format.

## Out Of Scope

- Do not install a broad, insecure udev rule or silently elevate privileges.
- Do not support every Linux package format.
- Do not run real firmware flashing merely to validate packaging.

## Implementation Requirements

- Documentation must map each supported artifact to exact permissions, dependencies, and installation/remediation commands.
- The application must surface permission and missing-tool errors with the device/tool name and an actionable remedy.
- Package metadata must not advertise file associations or sandbox capabilities that are not implemented.
- Verify the package in a clean install environment or clearly label that validation as a release blocker.

## Tests

- Validate package metadata, desktop entry, MIME definition, assets, and tool-discovery behavior.
- Exercise file-open argument handling and permission-denied serial behavior with fakes or an intentionally inaccessible device.
- Perform a clean-install smoke test for every claimed supported package format where the environment is available.

## Checkpoint Report

Create `docs/checkpoints/15c-linux-packaging-device-access.md` with selected formats, permission model, clean-install evidence, and remaining release blockers.

## Acceptance Criteria

- Every supported package format has an explicit, tested device-access model.
- Desktop/file integration and external-tool discovery match the artifact actually shipped.
- No broad privilege escalation or undocumented sandbox escape is required.
