# No Rubber Bushing Wear

[한국어 README](README.ko.md)

A tiny `Car Mechanic Simulator 2021` mod that prevents `Rubber Bushing` and `Small Rubber Bushing` parts from ever being treated as faulty.

The goal is simple: remove repetitive diagnostic friction from rubber bushings while leaving every other game mechanic untouched.

## What It Does

- Affects only:
  - `Rubber Bushing`
  - `Small Rubber Bushing`
- Keeps all other parts on vanilla behavior.
- Prevents rubber bushings from appearing as faulty during diagnostics.
- Does not edit save files.
- Does not modify game files.
- Does not require a configuration file.

## How It Works

The Steam version of CMS 2021 is an IL2CPP Unity build. This mod is a BepInEx IL2CPP + Harmony plugin.

At runtime, the plugin searches the generated interop `Assembly-CSharp` metadata for condition, wear, damage, and diagnostic/fault related methods. It then applies Harmony patches that only change behavior when the current part is identified as a rubber bushing.

Known localization keys:

```text
tuleja_1     = Rubber Bushing
tulejaMala_1 = Small Rubber Bushing
```

When a rubber bushing is detected:

- condition reads are forced to `100`
- diagnostic/fault results are forced to `false`
- damage/wear methods are skipped for that part only

## Requirements

- Windows/Steam version of `Car Mechanic Simulator 2021`
- .NET SDK 9 or newer
- BepInEx IL2CPP for Windows x64

Important: CMS 2021 uses `GameAssembly.dll`, which means it is an IL2CPP game. Mono BepInEx builds are not enough. The game root should contain:

```text
BepInEx\core\BepInEx.Unity.IL2CPP.dll
BepInEx\core\Il2CppInterop.Runtime.dll
```

## Build

If the game is installed at the default path used by this project:

```powershell
dotnet build -c Release
```

For a different install path:

```powershell
dotnet build -c Release -p:GameRoot="C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2021"
```

Build output:

```text
src\bin\Release\NoRubberBushingWear.dll
```

## Install

1. Install BepInEx IL2CPP x64 into the CMS 2021 game root.
2. Run the game once so BepInEx can generate IL2CPP interop assemblies.
3. Copy the built DLL to:

```text
Car Mechanic Simulator 2021\BepInEx\plugins\NoRubberBushingWear.dll
```

4. Launch the game and check:

```text
BepInEx\LogOutput.log
```

You should see:

```text
No Rubber Bushing Wear loaded
```

## Validation Checklist

- New saves load and play normally.
- Existing saves load and play normally.
- Rubber Bushings never appear as faulty during diagnostics.
- Small Rubber Bushings never appear as faulty during diagnostics.
- Story missions and repair orders can still be completed.
- Other suspension components can still fail normally.
- BepInEx logs show no new errors or warnings from this plugin.

## Limitations

- This is a runtime Harmony patch, not a save editor.
- The implementation intentionally avoids changing unrelated systems.
- Game updates may rename or restructure internal methods, requiring revalidation.
- No configuration file is provided by design.

## Development Notes

Research notes are kept in [docs/research.md](docs/research.md).

The currently tested local setup used:

- CMS 2021 Steam install
- BepInEx Unity.IL2CPP win-x64 `6.0.0-be.784`
- .NET SDK `9.0.315`
