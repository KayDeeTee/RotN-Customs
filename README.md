# RotN-Customs
Adds loading custom songs to the Rift of the Necrodancer demo, practice mode is currently broken too.

Requires bepinex https://github.com/BepInEx/BepInEx

## Plugin Location

`steamapps/common/Rift of the NecroDancer Demo/BepInEx/plugins`

## Song Locations

Windows `%Appdata%/LocalLow/Brace Yourself Games/Rift of the NecroDancer Demo/CustomTracks`

Linux `~/.local/share/Steam/steamapps/compatdata/3029150/pfx/drive_c/users/steamuser/AppData/LocalLow/Brace Yourself Games/Rift of the NecroDancer Demo/CustomTracks`

## Charter

https://github.com/KayDeeTee/RotN-Charter

# Compiling
add `Assembly-Csharp.dll`, `Unity.TextMeshPro.dll`, and `UnityEngine.UI.dll` from your games install to a folder called `lib/` in the root folder, then run `dotnet build` in your terminal from the root folder.
