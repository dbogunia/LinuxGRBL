# Localization and resx migration

Task 17 moves the Linux Avalonia port away from implicit WinForms `.resx` lookup and into an explicit UI-independent localization boundary.

## Inventory

Current repository inventory:

- Total `.resx` files: 416.
- `Strings*.resx` files: 20.
- `MainForm*.resx` files: 20.
- `SettingsForm*.resx` files: 20.

The legacy tree also contains many form-specific designer resources. Raster, SVG, and converter forms have localized text mixed with WinForms metadata, images, sizes, fonts, colors, and serialized designer values. Those files are inventory inputs, not runtime dependencies for the Avalonia shell.

## Migrated scope

The ported Avalonia workflows now resolve visible shell strings through `LocalizationCatalog`:

- application title and startup log messages;
- disconnected and firmware status text;
- startup diagnostics for unavailable sleep inhibition and secret storage;
- connection/device summary text;
- dialog/tool hub group names and descriptions.

The catalog is independent of WinForms and `System.Windows.Forms`. It currently ships `en` as the complete fallback resource and `pl-PL` as a partial migrated culture. Missing Polish keys deterministically fall back through `pl-PL -> pl -> en`.

## Culture persistence

`PortSettings` now stores a `Language` value and uses settings schema version 2. Old schema 1 settings are normalized at load time:

- missing or blank language becomes `en`;
- schema version is advanced to the current version;
- existing firmware, streaming, color scheme, and recent files are preserved.

This keeps language selection stable across app restarts without requiring WinForms resources.

## Missing key behavior

`LocalizationCatalog.Get` follows this order:

1. exact culture, for example `pl-PL`;
2. neutral culture, for example `pl`;
3. English fallback;
4. the key itself.

When a key cannot be resolved, the catalog records it in `MissingKeys` so tests and diagnostics can detect untranslated or mistyped entries.

## Non-text resources

Non-text legacy `.resx` resources are intentionally not loaded through the localization catalog.

Skipped categories:

- `System.Drawing.Color`, `System.Drawing.Font`, `System.Drawing.Point`, `System.Drawing.Size`, and other WinForms layout metadata;
- serialized WinForms control state, designer-only values, and component metadata;
- embedded `Bitmap`, `Icon`, image list, and toolbar resources tied to WinForms controls;
- SharpGL and WinForms host resources that belong to the legacy UI surface;
- form-specific dimensions and DPI assumptions.

Replacement strategy in the Linux port:

- icons, desktop files, MIME assets, and package resources live under the packaging/Avalonia asset path;
- raster image decoding is handled by the SkiaSharp-backed `LaserGRBL.Core.Raster` boundary;
- 2D and 3D preview rendering use the Avalonia preview model and OpenGL renderer;
- future ported screens should add only their user-visible strings to `LocalizationCatalog` or a generated catalog layer, not copy WinForms designer metadata.

This is deliberate. Porting unused layout metadata would add risk without improving the Linux MVP.

## Remaining work

- Add more cultures only when the corresponding Avalonia screens are ported.
- Replace the hand-authored catalog with generated resources if the translated surface grows enough to justify it.
- Run a UI language smoke test once broader settings UI exposes language selection.
