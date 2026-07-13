# Task 18 Checkpoint: User Data Compatibility

Implemented the Linux-port user data compatibility layer with explicit format decisions, safe JSON import/export paths, backup behavior, unsupported legacy binary handling, and tests for successful and failed migrations.

## Implemented Changes

- Added `LaserGRBL.Core.UserData` models for user data migration bundles, custom buttons, hotkeys, materials, usage counters, project data, and compatibility decisions.
- Added `UserDataCompatibilityService` with:
  - versioned `user-data.json` aggregate import/export,
  - custom button JSON import/export,
  - hotkey import/export with conflict preservation,
  - material JSON import/export and non-portable binary database skip behavior,
  - usage/lifetime counter JSON import/export,
  - GRBL config text round-trip validation,
  - `.lps` JSON project import/export with embedded-image payload support,
  - DPAPI Telegram credential skip/re-entry result,
  - `.bak` creation before overwriting existing Linux data files,
  - idempotent re-run behavior for preserved legacy binary files.
- Documented the compatibility table and JSON troubleshooting format in `docs/user-data-compatibility.md`.
- Kept legacy `BinaryFormatter`, encrypted, password-protected, and DPAPI data out of automatic Linux deserialization.

## Tests Run

- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build LaserGRBL.Linux.sln --no-restore -m:1 /nr:false`
- `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test LaserGRBL.Linux.sln --no-build -m:1 /nr:false`

## Test Evidence

- Build passed with 0 warnings and 0 errors.
- Test run passed: 209/209 tests.
- New coverage includes:
  - settings migration from supported JSON sample,
  - custom button round-trip and invalid file handling,
  - hotkey conflict preservation,
  - material import and binary database skip behavior,
  - usage/lifetime counter round-trip and invalid serializer failure,
  - GRBL config text round-trip and invalid line reporting,
  - `.lps` project JSON round-trip with embedded-image data and invalid binary input,
  - DPAPI Telegram re-entry-required result,
  - backup creation before migration/export,
  - idempotent migration re-run.

## Git

- Branch: `feature/18-user-data-compatibility`
- Implementation commit: `7d44c70` (`Task 18: User data compatibility`)
- Metadata commit: `7cb486c` (`Record Task 18 checkpoint metadata`)
- Push: branch pushed to `origin/feature/18-user-data-compatibility`; PR #26 opened at https://github.com/dbogunia/LinuxGRBL/pull/26.

## Remaining Risks

- Arbitrary legacy `BinaryFormatter` object graphs remain intentionally unsupported in the Linux runtime. Users must preserve and manually migrate those files or re-export via Windows LaserGRBL.
- Password-protected or encrypted material databases are not decrypted by the port.
- Legacy binary `.lps` project parity still depends on manual re-save/export or a future dedicated offline converter.
- Newly entered Telegram tokens must be handled by Task 21 secret-storage policy before Telegram notifications are enabled.
- Manual Linux GPU/display validation, USB GRBL hardware validation, and clean-install hardware validation remain release-blocking per prior checkpoints.

## Completion Status

Task 18 implementation and tests are complete. Commit and push metadata will be recorded after the final commit is created and pushed.
