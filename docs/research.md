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

## QuickShop Hook Points

The generated IL2CPP interop metadata exposes repair-screen part hover through `PartScript`.

Relevant symbols:

- `PartScript.GetIDWithTuned`
- `PartScript.GetID`
- `PartScript.ActionMount(bool)`
- `PartScript.SetMouseOver(bool)`
- `GameInventory.Instance`
- `GameInventory.GetItemPropertyCached`
- `PartProperty.Price`
- `Inventory.GetItems(string)`
- `Inventory.Add(Item, bool)`
- `GlobalData.PlayerMoney`
- `GlobalData.AddPlayerMoney`

When `ActionMount(bool)` starts, the QuickShop patch checks whether the required part already exists in `Inventory`. It also tracks mount lookup depth and patches `Inventory.GetItem(string)` / `Inventory.GetItems(string)` so empty lookups during the active mount flow can be populated with one newly bought vanilla `Item`. If missing and affordable, it adds the item to `Inventory` and subtracts the vanilla price before the original mount flow continues.

## Installed Tooling

- Installed BepInEx IL2CPP: `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752+dd0655f`
- Confirmed core files:
  - `BepInEx.Core.dll`
  - `BepInEx.Unity.IL2CPP.dll`
  - `Il2CppInterop.Runtime.dll`
  - `0Harmony.dll`
- The previous Mono-style BepInEx install was moved to a timestamped `_bepinex_mono_backup_*` folder in the game root.
- `6.0.0-be.784+0523d6f` was tried first, but failed during IL2CPP interop generation with an `AsmResolver.DotNet.ModuleDefinition` `MissingMethodException`, so it was replaced with `6.0.0-be.783+c58c42d`.
- `6.0.0-be.783+c58c42d` generated interop assemblies successfully, but failed before plugin loading during IL2CPP chainloader setup with `System.InvalidOperationException: Sequence contains no elements`, so it was replaced with `6.0.0-be.752+dd0655f`.
- The mod builds cleanly with .NET SDK 9.0 and has been copied to `BepInEx\plugins\NoRubberBushingWear.dll`.
- `UnityLogListening = false` is required in `BepInEx\config\BepInEx.cfg` on this install because the chainloader fails while registering the Unity log callback.
- An early runtime scanner patched too broadly and caused a stack overflow in `BaseItem.GetConditionToShow`. The active implementation only patches safe `PartData`/`BodyPartData` condition accessors and does not invoke property getters while identifying parts.

## Runtime Validation

The game has been launched successfully with BepInEx Unity.IL2CPP `6.0.0-be.752` and the narrowed rubber bushing patches.

After adding QuickShop, launch the game again and check `BepInEx\LogOutput.log` for:

```text
No Rubber Bushing Wear 1.1.0
QuickShop enabled
```

Manual QuickShop validation still needs an in-game repair-screen check: try to install a part that is missing from inventory; one replacement item should be purchased automatically, money should decrease by the vanilla price, and the install attempt should continue.
