# Feature Parity And SharpGL Decision

Task 22 uses `docs/linux-port-plan-coverage.md`, the task checkpoints, and local tests as the final parity checklist for the Linux port. Task 23 adds real graphical evidence for the Avalonia/OpenGL path. The Linux port is a buildable and testable MVP, but it is not release-complete until the remaining hardware and clean-install blockers are closed.

## SharpGL/3D Decision

| Legacy area | Linux decision | Status | Evidence | Release impact |
| --- | --- | --- | --- | --- |
| SharpGL WinForms host | Replaced, not copied. The legacy host depends on WinForms, `System.Drawing`, display lists, custom drawing threads, and Windows-era render-context assumptions. | Replaced | `JobPreview3DControl`, `Preview3DSceneModel`, `OpenGlImmediatePreviewRenderer`, Task 13B checkpoint. | Acceptable architecture replacement. |
| `GrblPanel3D` / `Obj3D` behavior | Rebuilt as Avalonia 3D scene/camera/projection models and shader/VBO OpenGL draw backend. | Partial | Tests cover scene bounds, camera rotate/tilt/zoom, progress segmentation, color mapping, fallback diagnostics, draw uploads, and Task 23 nonblank GPU screenshot evidence. | Manual legacy behavior comparison still required. |
| Avalonia OpenGL host | Implemented through `OpenGlControlBase`. | Implemented | `glxinfo -B`, `scripts/validate-opengl-preview.sh linux-x64`, `artifacts/opengl-validation/opengl-diagnostics.log`, and nonblank screenshot crop. | Real GPU/display nonblank proof complete on the validation host. |
| 3D preview interactions | Rotate, tilt, zoom, auto-fit/reset, progress, machine cursor state exist in view-model/model tests. | Partial | `AvaloniaOpenGlPreviewTests`, `OpenGlImmediatePreviewRendererTests`, Task 13B checkpoint, Task 23 screenshot evidence. | Detailed manual interaction parity still required before release notes claim full SharpGL parity. |
| OpenGL failure fallback | Implemented as visible diagnostic fallback while app remains usable. | Implemented | `OpenGlPreviewContextStatus.Failure`, `JobPreview3DControl` fallback text, tests with fake failure. | Acceptable, but not proof of successful rendering. |

**Decision:** required SharpGL/3D behavior is replaced by an Avalonia/OpenGL path, and Task 23 proves a real nonblank Linux GPU render. Final SharpGL behavior parity is still incomplete until detailed interaction evidence and legacy behavior comparison are recorded.

## Feature Parity Matrix

| Area | Linux status | Evidence | User impact / omission | Severity | Follow-up |
| --- | --- | --- | --- | --- | --- |
| Main connect/run/jog/manual command workflow | Implemented with fake/emulator/serial abstractions | Task 12, Task 16, `AvaloniaMainWorkflowTests` | Real USB GRBL workflow not validated on hardware. | Release blocker | Run physical controller smoke: detect port, connect, manual command, run/hold/resume/reset/abort, disconnect. |
| Firmware variants: GRBL, Smoothie, Marlin, VigoWork | Implemented in protocol selection/parsers | Task 03, Task 12 tests | Fake traces only; no real firmware matrix. | High | Run fake trace parity plus hardware where available before release notes. |
| `SincroStart` cross-process Run Multi | Deferred for first Linux release | Inventory and Task 15D notes | Multi-instance synchronized start is unavailable. Ordinary run/resume/hold does not replace this. Legacy has a global named-event dependency; Linux needs an explicit cross-process coordinator and ownership protocol. | Medium | Design a Linux coordinator service/lock protocol after single-device ownership is stable. |
| G-code import, append, reopen | Implemented | Tasks 04/12 tests | Hardware run validation still pending. | High | Include in USB GRBL smoke. |
| Raster/SVG conversion and options | Partial | Tasks 04/09/14/15B tests | Core routing/options covered; full visual/font/text-to-path parity not proven. | Medium | Add fixture corpus for legacy SVG/font cases. |
| `.lps` project files | Partial | Tasks 04/14/18 tests and docs | JSON project path supported; arbitrary legacy binary `.lps` requires manual re-save/export or converter. | High | Build an offline legacy converter if release requires direct binary `.lps` import. |
| Preview 2D | Implemented | Task 13 tests | Arc/detail parity still approximated in model layer. | Medium | Expand preview fixture comparisons. |
| Preview 3D/OpenGL | Partial | Task 13B tests/checkpoint and Task 23 real GPU screenshot/logs | Nonblank render proven on one X11/NVIDIA host; detailed manual interaction parity still pending. | High | Complete rotate/tilt/zoom/reset/progress/cursor manual parity pass. |
| Named color schemes and semantic colors | Implemented | Tasks 11/13/13B/17 tests | Visual inspection still needed. | Low | Include in manual UI pass. |
| Settings | Implemented | Tasks 05/17/20/21 tests | Legacy binary arbitrary graphs intentionally not deserialized. | Medium | Keep migration docs visible. |
| Custom buttons | Implemented for ported model/import-export | Tasks 14/18 tests | Legacy binary `.zbn` remains preservation/manual-migration path. | Medium | Add converter only if demanded by release criteria. |
| Hotkeys | Implemented for bindings/conflicts | Tasks 14/18 tests | Platform/global shortcut parity not claimed. | Medium | Define Linux desktop accelerator scope. |
| Material editor / Material DB | Implemented local model, network updates policy-gated | Tasks 14/18/21 tests | Binary/password DB variants unsupported. | Medium | Add supported import path if legacy fixtures require it. |
| Generators | Covered through option propagation/core conversion tasks | Tasks 04/14 | Full generated-output parity corpus not complete. | Medium | Add golden G-code fixtures for generator families. |
| Firmware flashing | Implemented behind Linux process service and safety gate | Tasks 09/14/20 tests | Real flashing not performed; `avrdude` may be missing. | High | Validate dry-run/tool discovery and real flash only with safe hardware. |
| WiFi / Ortur config | Implemented through `nmcli` service and UI scaffold | Tasks 10/14 tests | No manual WiFi scan/config validation. | Medium | Manual NetworkManager host validation. |
| Logs, diagnostics, support bundle | Implemented | Task 19 tests | Deeper automatic communication logging hooks can expand. | Low | Wire more session events into log channels. |
| Safety/legal/first-run | Implemented service/view-model gates | Task 20 tests | Full visual dialog parity not complete. | Medium | Polish native dialog UX in final UI pass. |
| Localization/resx | Implemented catalog/fallback policy | Task 17 tests | Not every legacy string translated; non-text resx intentionally not ported. | Low | Add strings as screens become real UI. |
| User data compatibility | Implemented with explicit supported/unsupported behavior | Task 18 tests/docs | Arbitrary BinaryFormatter graphs unsupported. | Medium | Optional offline converters for high-demand formats. |
| Secret storage / Telegram | Policy-gated, secure-store required | Tasks 18/21 tests | Current bootstrap uses unavailable secret store, so Telegram remains disabled. | Medium | Add Linux keyring/libsecret implementation. |
| Packaging, desktop/MIME, device/sandbox access | Implemented metadata/tarball/desktop/MIME guidance and fail-closed validation runner | Task 15C tests; Task 24 `scripts/validate-release-hardware.sh` | Clean-install with serial hardware not performed because no controller is attached. | Release blocker | Run the validation runner with a readable/writable serial controller and record manual GRBL workflow evidence. |
| Update/privacy/artifact integrity | Implemented policy-gated off by default | Task 21 tests | No updater execution by design. | Low | Keep release manifest generation aligned with SHA policy. |
| Emulator console | Implemented bounded read-only activity console | Tasks 08/14 tests | GUI inspection still useful. | Low | Include in manual UI pass. |
| Timing / sleep inhibition | Implemented monotonic clock boundary and best-effort inhibitor behavior | Tasks 02/03/12 tests | Real desktop inhibitor unavailable in current bootstrap. | Medium | Add Linux portal/systemd inhibitor implementation. |
| CI/support matrix | Implemented for headless build/test | Task 15A tests/docs | Headless CI does not prove package install behavior; Task 23 adds local graphical evidence. | High | Keep graphical validation script as release evidence; add package/hardware validation. |
| Shutdown/recovery | Implemented safe shutdown/resource ownership contract | Task 15D tests/docs | Rich persisted restart UX remains policy-level. | High | Expand recovery UI after hardware validation. |
| Single-device/endpoint ownership | Implemented file lock/resource ownership | Task 15D/16 tests | Real multi-process hardware conflict validation pending. | High | Add physical device conflict smoke. |
| Image/font/GDI backend | Implemented supported Skia/image backend boundary | Task 15B tests/docs | Pixel-perfect GDI parity not claimed. | Release blocker only if required legacy fixture fails; current backend contract is supported. | Expand fixture corpus for release-sensitive images/fonts. |

## Blocked Items

| Severity | Blocked item | Impacted workflow | Recommended next task |
| --- | --- | --- | --- |
| Release blocker | Real USB GRBL hardware validation missing | Connect, manual command, run/hold/resume/reset/abort/disconnect | Hardware validation pass with stable `/dev/serial/by-id` device. |
| Release blocker | Clean-install package/device validation missing | Package install, desktop/MIME launch, serial permissions, bundled resources | Run `scripts/validate-release-hardware.sh` with serial hardware attached. |
| High | Full 3D interaction and legacy behavior comparison incomplete | Rotate/tilt/zoom/reset/progress/machine cursor, required SharpGL replacement parity | Manual graphical parity pass using the Task 23 validation host/script. |
| High | Firmware/WiFi/audio behavior not manually validated on real environment | Firmware flashing, WiFi configuration, sound cues | Safe hardware/manual validation only. |
| High | Legacy binary `.lps` direct import incomplete | Existing Windows LaserGRBL project users | Offline converter or clear migration tool. |
| Medium | `SincroStart` Run Multi deferred | Users coordinating multiple app instances/machines | Cross-process coordinator design using Linux IPC and resource ownership. |
| Medium | Full visual dialog parity incomplete | First-run/legal/settings/tool dialogs | Manual Avalonia UX pass. |

## Release Verdict

The Linux port is an automated-test-green MVP with broad functional coverage and explicit policies for safety, privacy, diagnostics, packaging, and user data. It is **not release-ready** until the remaining release blockers above are closed. The most important release decision is that 3D/SharpGL parity has a real Avalonia/OpenGL replacement in code and a nonblank GPU-render proof, while detailed interaction parity remains a high-priority manual validation item.
