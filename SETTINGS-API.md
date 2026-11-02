# Accord settings interface

This fork retains the original RotationSolver plugin identity and all upstream behaviour. Use one installed RSR build at a time; it is not a second engine identity.

The only runtime addition is two local IPC endpoints under RotationSolverReborn:

- AccordReadSettings() returns JSON with Success, ApiVersion 1 and Settings. It lists writable scalar global settings, including the stored boolean values of ConditionBoolean settings. Version, complex collections, rotation-specific settings and per-action configurations are excluded.
- AccordSetSetting(name, valueJson) accepts an exact global property name and an explicit JSON scalar. It validates the JSON type, integer range, enum name and finite floating-point values, preserves ConditionBoolean wrappers, applies the value to the existing live configuration, and saves through RSR. A failed save restores the previous in-memory value. Responses include Success and, on failure, Error. Calls must run on Dalamud's framework thread. There are no implicit boolean toggles or setting changes at startup.

This is a small settings access foundation, not a settings-policy redesign. It does not impose additional UI ranges beyond the native property type. Existing native commands and IPC remain available. Job/action-specific API coverage can be added when Accord's new requirements are decided. No migration, hotbar, intent execution or session machinery is included.

Run dotnet run --project tests/Settings.Tests.csproj -c Release. In-game API/persistence verification remains pending.
