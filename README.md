# PolitiCities
Add more politics to your Cities Skylines city. 

A Cities: Skylines 1 code mod (C# DLL), scaffolded on the official ICities API with Harmony 2.x patching via the CitiesHarmony dependency.

## Requirements

- Windows with Cities: Skylines installed via Steam at the default path (otherwise pass `/p:ManagedDLLPath=<game>\Cities_Data\Managed` to the build)
- Visual Studio 2022 (Community, ".NET desktop development" workload) or any .NET SDK (`dotnet build` works too)

## Build and deploy

Build in Visual Studio (F6) or with `dotnet build`. The `DeployToModDirectory` target copies the output to
`%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\PolitiCities\` automatically.

`AssemblyVersion` is `1.0.*`, so every rebuild bumps the version and the running game hot-reloads the mod without a restart.

## Test in game

Enable the mod in Content Manager > Mods. Debug output: F7 in-game panel (`DebugOutputPanel`), `Cities_Data\output_log.txt` (`Debug.Log`), and the ModTools workshop mod (Ctrl+Q menu, Ctrl+E scene explorer).

## Publish to Steam Workshop

Content Manager > Mods > enable PolitiCities > Share. A `PreviewImage.png` placed next to the DLL is picked up as the Workshop thumbnail automatically. List the CitiesHarmony workshop item (id 2040656402) as a required dependency; the mod also auto-subscribes to it at runtime via `CitiesHarmony.API`.

## Project layout

- `Mod.cs` — `IUserMod` entry point: name/description, Harmony init, options panel hook. Keep `HarmonyLib` references out of this class so the mod loads even without CitiesHarmony.
- `Patcher.cs` — static `PatchAll`/`UnpatchAll` (Harmony 2.x). Add `[HarmonyPatch]` classes anywhere in the assembly; `PatchAll` picks them up.
- `Loading.cs` — `LoadingExtensionBase`; `OnLevelLoaded(LoadMode mode)` is the main per-save hook (the game auto-discovers all extension interfaces in the mod assembly).
- `Settings.cs` — mod options panel via `IUserMod.OnSettingsUI`.

## Key facts for contributors

- Target framework is `net35` (game runs Unity 5.6.7 / Mono). References come from the game's `Cities_Data\Managed` folder: `ICities`, `Assembly-CSharp`, `ColossalManaged`, `UnityEngine` (add more as needed, with `Private=False`).
- Savegame persistence: implement `ISerializableDataExtension` (use `SerializableDataExtensionBase`) with `IDataContainer`; validate IDs in `AfterDeserialize`.
- Heavy per-tick work belongs in `IThreadingExtension` (simulation thread); never call Unity APIs off the main thread.
- Reference docs: [Cities: Skylines Wiki developer guides](https://skylines.paradoxwikis.com/Developer_guides), [CitiesHarmony repo + ExampleMod](https://github.com/boformer/CitiesHarmony), [cslmodding.info](https://cslmodding.info/tutorials/).
