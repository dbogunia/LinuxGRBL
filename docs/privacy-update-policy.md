# Telemetry, Update, And Privacy Policy

The Linux port does not send telemetry by default. Outbound network-like behavior is routed through explicit privacy settings and service gates.

## Defaults

| Area | Default | Setting | Behavior |
| --- | --- | --- | --- |
| Telemetry umbrella | Off | `Privacy.TelemetryEnabled` | Usage and laser statistics require this plus their own opt-in flags. |
| Update checks | Off | `Privacy.UpdateChecksEnabled` | Disabled checks do not call the manifest client. |
| Material DB updates | Off | `Privacy.MaterialUpdatesEnabled` | Download/update calls are policy-blocked until explicitly enabled. |
| Usage statistics | Off | `Privacy.UsageStatisticsEnabled` | Also requires telemetry umbrella opt-in. |
| Laser statistics upload | Off | `Privacy.LaserStatisticsEnabled` | Also requires telemetry umbrella opt-in. |
| Telegram notifications | Off | `Privacy.TelegramNotificationsEnabled` | Requires explicit re-entry of the token and secure secret storage. |
| External URL opening | User initiated only | platform URL service | Routed through `IExternalUrlService`; only `http` and `https` URLs are allowed. |

Settings are stored in `PortSettings.Privacy` with schema version 4. Secret values are not stored in JSON settings.

## Policy Gate

`PrivacyPolicyService` checks `OutboundNetworkPurpose` before HTTP-like or external platform calls:

- update check;
- material database update;
- usage statistics;
- laser statistics;
- Telegram notification;
- external URL open.

Disabled policy flags return a failure result before the supplied outbound operation is invoked.

## Update Checks

`ReleaseManifestUpdateService` remains cancellable and non-blocking. It never downloads, executes, elevates, or installs an updater.

For available releases, manifests must provide:

- HTTPS manifest URL;
- valid semantic version newer than the current app version;
- HTTPS release URL;
- artifact version matching the manifest version;
- 64-character hexadecimal SHA256 value.

Malformed JSON, offline/timeout/cancellation failures, version mismatch, non-HTTPS URLs, and missing/invalid integrity metadata return failure results without crashing startup.

## Telegram

Legacy Windows DPAPI Telegram ciphertext is not portable and is never decrypted on Linux. Users must explicitly re-enter a token after enabling Telegram notifications.

New tokens go through `ISecretStore`. If secure storage is unavailable, Telegram remains disabled with a user-facing failure result and the token is not persisted in settings.

## Inventory

| Legacy/network path | Linux policy |
| --- | --- |
| Update checks | Off by default; HTTPS/integrity gated. |
| Material DB downloads/updates | Off by default; policy-gated separately from telemetry. |
| `UsageStats` | Off by default; requires telemetry umbrella plus usage-stat opt-in. |
| Laser statistics upload | Off by default; requires telemetry umbrella plus laser-stat opt-in. |
| Telegram notifications | Off by default; requires secure secret storage and re-entered token. |
| External URL/file opening | Routed through platform service; no silent open during startup. |
| Support bundles | Local zip only; no automatic upload. |
