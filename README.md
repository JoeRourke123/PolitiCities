# PolitiCities
Add more politics to your Cities Skylines city. 

A Cities: Skylines 1 code mod (C# DLL), scaffolded on the official ICities API with Harmony 2.x patching via the CitiesHarmony dependency.

## Requirements

- A Cities: Skylines install to copy the reference DLLs from (see `lib/README.md`)
- For local builds: Windows with the game installed via Steam at the default path, or any OS with `lib/` populated and the .NET SDK (`dotnet build` works on Linux/macOS too)

## Build and deploy

`dotnet build` or F6 in Visual Studio. On Windows the `DeployToModDirectory` target copies the output to
`%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\PolitiCities\` automatically; on other OSes it is skipped.

`AssemblyVersion` is `1.0.*`, so every rebuild bumps the version and the running game hot-reloads the mod without a restart.

## Install on Linux / SteamOS

The DLL is platform-neutral (the game runs it on Mono), so the same release zip works everywhere. Extract it to:

- Native Linux/SteamOS build: `~/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/PolitiCities/`
- If you run the Windows version through Proton: `~/.local/share/Steam/steamapps/compatdata/255710/pfx/drive_c/users/steamuser/AppData/Local/Colossal Order/Cities_Skylines/Addons/Mods/PolitiCities/`

The zip contains a `PolitiCities/` folder — place that folder (with `PolitiCities.dll` and `CitiesHarmony.API.dll` inside) in the path above, then enable the mod in Content Manager > Mods.

## Test in game

Debug output: F7 in-game panel (`DebugOutputPanel`), `Cities_Data\output_log.txt` (`Debug.Log`), and the ModTools workshop mod (Ctrl+Q menu, Ctrl+E scene explorer).

## CI, releases, and Workshop publishing

GitHub Actions workflows live in `.github/workflows/`:

- `build.yml` — runs on every push/PR: restores, builds `net35` on `ubuntu-latest`, packages `dist/PolitiCities.zip`, and uploads it as the `PolitiCities` artifact. Requires the game reference DLLs in `lib/` (see `lib/README.md`).
- `release.yml` — on a `v*` tag, builds and attaches `PolitiCities-<tag>.zip` to a GitHub Release. Cut a release with `git tag v1.0.0 && git push origin v1.0.0`.
- `workshop-upload.yml` — **manual only** (`workflow_dispatch`); uploads the packaged mod folder to the Steam Workshop (app 255710) via steamcmd. It never runs on push. Before using it, add these repository secrets (Settings > Secrets and variables > Actions): `STEAM_USERNAME`, `STEAM_PASSWORD`, and optionally `STEAM_GUARD` (a 5-char Steam Guard code, if your account has one). The Steam account must own Cities: Skylines. Run it from the Actions tab, pass an existing Workshop item ID (or `0` to create a new item) and a visibility.

The in-game alternative is still available: enable the local mod in Content Manager > Mods > Share. A `PreviewImage.png` next to the DLL is picked up as the Workshop thumbnail automatically (also used as `previewfile` by the upload workflow). List the CitiesHarmony workshop item (id 2040656402) as a required dependency; the mod also auto-subscribes to it at runtime via `CitiesHarmony.API`.

## Project layout

- `Mod.cs` — `IUserMod` entry point: name/description, Harmony init, options panel hook. Keep `HarmonyLib` references out of this class so the mod loads even without CitiesHarmony.
- `Patcher.cs` — static `PatchAll`/`UnpatchAll` (Harmony 2.x). Add `[HarmonyPatch]` classes anywhere in the assembly; `PatchAll` picks them up.
- `Loading.cs` — `LoadingExtensionBase`; `OnLevelLoaded(LoadMode mode)` is the main per-save hook (the game auto-discovers all extension interfaces in the mod assembly).
- `Settings.cs` — mod options panel via `IUserMod.OnSettingsUI`.
- `lib/` — game reference DLLs used for building (see `lib/README.md`).
- `.github/workflows/` — build / release / workshop-upload automation.
- `Politics/` — the political simulation: `Party`/`Council` (model), `PoliticalState` (per-save state), `PoliticalData` (savegame persistence via `SerializableDataExtensionBase`), `PoliticalSimulation` (simulation tick via `ThreadingExtensionBase`), `CouncilPanel` + `HotkeyHandler` (UI, open with the `P` key in game).
- `docs/DESIGN.md` — feature design, systems map, and build order.

## Key facts for contributors

- Target framework is `net35` (game runs Unity 5.6.7 / Mono). References come from the game's `Cities_Data\Managed` folder: `ICities`, `Assembly-CSharp`, `ColossalManaged`, `UnityEngine` (add more as needed, with `Private=False`).
- Savegame persistence: implement `ISerializableDataExtension` (use `SerializableDataExtensionBase`) with `IDataContainer`; validate IDs in `AfterDeserialize`.
- Heavy per-tick work belongs in `IThreadingExtension` (simulation thread); never call Unity APIs off the main thread.
- Reference docs: [Cities: Skylines Wiki developer guides](https://skylines.paradoxwikis.com/Developer_guides), [CitiesHarmony repo + ExampleMod](https://github.com/boformer/CitiesHarmony), [cslmodding.info](https://cslmodding.info/tutorials/).
