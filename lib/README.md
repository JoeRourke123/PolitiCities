# Game reference DLLs

The build needs the game's managed assemblies as compile-time references. Copy these files here once, from your Cities: Skylines install:

```
ICities.dll
Assembly-CSharp.dll
ColossalManaged.dll
UnityEngine.dll
UnityEngine.UI.dll (optional)
```

Source locations:

- Windows: `<SteamLibrary>\SteamApps\common\Cities_Skylines\Cities_Data\Managed\`
- Linux / SteamOS: `~/.steam/steam/steamapps/common/Cities_Skylines/Cities_Data/Managed/`

The csproj uses this folder automatically when `lib\ICities.dll` exists; otherwise it falls back to the default Steam path on Windows, or you can override with `dotnet build /p:ManagedDLLPath=<path>`.

Note: these DLLs are Colossal Order's game files. Committing them to a public repo is common practice in the CS1 modding community, but if you prefer not to publish them, keep this repo private or exclude `lib/*.dll` and build locally with `/p:ManagedDLLPath=...`.
