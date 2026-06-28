# Research Notes

## Local CMS 2021 Install

- Game root: `D:\SteamLibrary\steamapps\common\Car Mechanic Simulator 2021`
- The installed game is an IL2CPP Unity build:
  - `GameAssembly.dll` exists.
  - `Car Mechanic Simulator 2021_Data\il2cpp_data\Metadata\global-metadata.dat` exists.
  - `Car Mechanic Simulator 2021_Data\Managed` does not exist.
- The original `Assembly-CSharp.dll` plan does not match this installed build. Runtime patching still uses BepInEx + Harmony, but it requires an IL2CPP-capable BepInEx distribution and generated interop assemblies.

## Rubber Bushing Identifiers

Found in `Car Mechanic Simulator 2021_Data\StreamingAssets\Localizations\English.txt`:

```text
!tuleja_1=Rubber Bushing
!tulejaMala_1=Small Rubber Bushing
```

The mod treats both localization keys as Rubber Bushing parts.

## Relevant Metadata Symbols

`global-metadata.dat` contains game symbols that point to the safest behavior layer:

- `CarPart`
- `GetCondition`
- `SetCondition`
- `GetQuality`
- `SetQuality`
- `SetRandomPartsConditions`
- `CheckIfItemExistAndHaveGoodCondition`
- `PartListOrderItem`
- `GetExaminedCarParts`
- `GetNotExaminedCarParts`

The implementation therefore patches condition/fault/damage candidates in `Assembly-CSharp` at runtime and only changes results when the instance or arguments identify `tuleja_1`, `tulejaMala_1`, `Rubber Bushing`, or `Small Rubber Bushing`.

## Installed Tooling

- Installed BepInEx IL2CPP: `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.783+c58c42d`
- Confirmed core files:
  - `BepInEx.Core.dll`
  - `BepInEx.Unity.IL2CPP.dll`
  - `Il2CppInterop.Runtime.dll`
  - `0Harmony.dll`
- The previous Mono-style BepInEx install was moved to a timestamped `_bepinex_mono_backup_*` folder in the game root.
- `6.0.0-be.784+0523d6f` was tried first, but failed during IL2CPP interop generation with an `AsmResolver.DotNet.ModuleDefinition` `MissingMethodException`, so it was replaced with `6.0.0-be.783+c58c42d`.
- The mod builds cleanly with .NET SDK 9.0 and has been copied to `BepInEx\plugins\NoRubberBushingWear.dll`.

## Remaining Runtime Validation

Launch the game once so BepInEx can generate IL2CPP interop assemblies and load the plugin. Then check `BepInEx\LogOutput.log` for:

```text
No Rubber Bushing Wear loaded.
```
