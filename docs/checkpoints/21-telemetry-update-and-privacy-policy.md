# Task 21 Checkpoint: Telemetry, Update, And Privacy Policy

Implemented a Linux-port privacy policy layer so update checks, telemetry-like behavior, Material DB updates, usage/laser statistics, Telegram notifications, and external URL opening are explicit, opt-in, and testable.

## Implemented Changes

- Added `PrivacySettings` to `PortSettings` and bumped settings schema to version 4.
- Added `OutboundNetworkPurpose` and `PrivacyPolicyService` to gate outbound operations before HTTP/platform calls.
- Added `IExternalUrlService`, `PrivacyAwareExternalUrlService`, and `UnavailableExternalUrlService`.
- Added `TelegramNotificationService` that stores newly entered tokens only through `ISecretStore`.
- Kept Telegram disabled when secure secret storage is unavailable.
- Updated Avalonia bootstrap so update checks use persisted privacy settings and remain off by default.
- Included privacy settings in support bundle settings summaries without secret material.
- Updated user data compatibility documentation to show privacy settings in the versioned settings JSON.
- Hardened `ReleaseManifestUpdateService`:
  - disabled checks do not call the manifest client,
  - manifest URL must be HTTPS,
  - release URL must be HTTPS,
  - available releases require matching artifact version,
  - available releases require a valid SHA256 integrity value,
  - malformed/offline/cancelled/timeout failures remain nonfatal results.
- Documented the network/privacy inventory and policy in `docs/privacy-update-policy.md`.

## Tests Run

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

The plain sandboxed VSTest path still cannot run because local socket creation is denied, matching earlier checkpoints.

## Test Evidence

- Build passed with 0 warnings and 0 errors.
- Test run passed: 230/230 tests.
- New coverage includes:
  - telemetry and optional network features disabled by default,
  - no outbound calls when each policy flag is disabled,
  - separate opt-in behavior for update checks, Material DB updates, usage statistics, laser statistics, and Telegram notifications,
  - update available and no-update behavior,
  - malformed manifest, timeout, cancellation, and offline failures,
  - release URL HTTPS enforcement,
  - manifest URL HTTPS enforcement,
  - artifact version mismatch,
  - missing/invalid SHA256 integrity information,
  - external URL open success/failure through a fake platform service,
  - unavailable secret store and explicit Telegram token re-entry behavior.
  - privacy settings included in support bundle settings summary without token values.

## Git

- Branch: `feature/21-privacy-updates`
- Implementation commit: pending until commit is created.
- Push: pending until branch is pushed.

## Remaining Risks

- Material DB update, usage statistics, laser statistics, and Telegram sender implementations remain policy-gated service paths; no new endpoints were added in this task.
- A real Linux secure secret store is still unavailable in the current bootstrap, so Telegram remains disabled there.
- External URL opening has a platform contract and safety wrapper, but no desktop implementation is wired yet.
- Manual Linux GPU/display validation, USB GRBL hardware validation, and clean-install hardware validation remain release-blocking per prior checkpoints.

## Completion Status

Task 21 implementation and tests are complete. Commit and push metadata will be recorded after the final commit is created and pushed.
