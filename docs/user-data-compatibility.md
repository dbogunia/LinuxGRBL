# User Data Compatibility

Task 18 defines Linux-port behavior for legacy LaserGRBL user data. The port preserves legacy files by default, imports only documented portable formats, and never deserializes arbitrary `BinaryFormatter` graphs during startup.

## Compatibility Table

| Legacy format | Linux-port format | Import behavior | Export behavior | Backup behavior | Failure behavior |
| --- | --- | --- | --- | --- | --- |
| `LaserGRBL.Settings.bin` | `settings.json` and `user-data.json` | Portable JSON samples import and normalize to current settings schema. Arbitrary legacy binary settings are preserved and marked manual migration required. | Versioned JSON settings. | Source legacy file is copied to `.bak` before migration attempts. | Startup continues with defaults and a user-facing manual migration result. |
| `CustomButtons.bin`, `.zbn` | `custom-buttons.json` | JSON button arrays import when label and command are present. Legacy binary `.zbn` stays preserved with unsupported-format guidance. | JSON button array. | Existing Linux file is copied to `.bak` before overwrite. | Invalid JSON returns `Custom button import failed` and leaves existing data unchanged. |
| Hotkey serializer data | `hotkeys.json` | JSON action/gesture bindings import. Gesture conflicts are preserved as `IsConflict = true` so the UI can ask the user to resolve them. | JSON hotkey array. | Existing Linux file is copied to `.bak` before overwrite. | Invalid data returns `Hotkey import failed`; startup is not blocked. |
| `StandardMaterials.psh`, `PSHelper/MaterialDB.*` | `materials.json` | JSON material arrays import and clamp power/speed to supported ranges. Binary/password material databases are skipped with a clear message. | JSON material array. | Existing Linux file is copied to `.bak` before overwrite. | Unsupported database variants return a non-portable material database message. |
| `UsageStats.bin`, `LaserLifeCounter.bin` | `usage-counters.json` | Portable JSON counters import. Legacy binary counters are preserved and skipped. | JSON usage counter object. | Existing Linux file is copied to `.bak` before overwrite. | Invalid counters fail clearly and counters fall back to zero in callers. |
| GRBL config import/export text | Plain text `$` setting lines | `$key=value` lines import and round-trip. | Normalized text with one setting per line. | No destructive migration is performed. | Invalid lines identify the first bad setting. |
| `.lps` project files | `projects/*.lps.json` | JSON project files import with optional embedded-image base64 data. Legacy binary `.lps` files are preserved and require manual re-save/export. | JSON project data. | Existing project JSON is copied to `.bak` before overwrite. | Invalid binary input returns a clear project import error. |
| DPAPI-protected Telegram credentials | `ISecretStore` entry after explicit user re-entry | Windows DPAPI ciphertext is never decrypted, copied to JSON, or treated as portable. | No legacy ciphertext export. New tokens must go through `ISecretStore`. | Source file is preserved only. | Telegram notifications require explicit token re-entry after the user enables them. |

## JSON Formats

`user-data.json` is the aggregate migration container:

```json
{
  "SchemaVersion": 1,
  "Settings": {
    "SchemaVersion": 2,
    "Firmware": "Grbl",
    "StreamingMode": "Buffered",
    "ColorScheme": "Default",
    "Language": "en",
    "RecentFiles": []
  },
  "CustomButtons": [{ "Id": 1, "Label": "Unlock", "Command": "$X" }],
  "Hotkeys": [{ "Action": "Run", "Gesture": "Ctrl+R", "IsConflict": false }],
  "Materials": [{ "Name": "Birch", "Power": 300, "Speed": 1000, "Source": "StandardMaterials.psh" }],
  "UsageCounters": { "JobsRun": 0, "LaserOnTime": "00:00:00", "MachineConnectedTime": "00:00:00" },
  "Project": null
}
```

Project JSON uses:

```json
{
  "Name": "demo",
  "GCodeLines": ["G0 X0", "M5"],
  "EmbeddedImageBase64": null
}
```

The implementation lives in `UserDataCompatibilityService`. It uses `IAppPaths` for all Linux paths, writes through temporary files, backs up existing Linux files before overwrite, and returns `OperationResult` failures instead of throwing for corrupt or unsupported imports.
