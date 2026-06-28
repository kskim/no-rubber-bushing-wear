# No Rubber Bushing Wear

[한국어 README](README.ko.md)

A small `Car Mechanic Simulator 2021` BepInEx plugin with two focused quality-of-life changes:

- `Rubber Bushing` and `Small Rubber Bushing` never count as faulty.
- On the repair screen, hover a car part and press `B` to buy one replacement immediately.

The main goal is to remove repetitive diagnostic friction while leaving normal repair, damage, inventory, and save behavior intact.

## Features

### No Rubber Bushing Wear

- Affects only:
  - `Rubber Bushing`
  - `Small Rubber Bushing`
- Keeps all other parts on vanilla behavior.
- Prevents rubber bushings from appearing as faulty during diagnostics.
- Does not edit save files.
- Does not modify game files.
- Does not require a configuration file.

Known localization keys:

```text
tuleja_1     = Rubber Bushing
tulejaMala_1 = Small Rubber Bushing
```

### QuickShop

- On the repair or assembly screen, hover a car part with the mouse.
- Press `B`.
- The mod buys one replacement part and adds it to your inventory.

QuickShop uses vanilla game data for part IDs, prices, money, and inventory items. It does not bypass money checks.

## How It Works

CMS 2021 on Steam is an IL2CPP Unity game. This mod runs as a BepInEx IL2CPP + Harmony plugin.

For rubber bushings, the plugin patches the generated interop condition accessors for `PartData` and `BodyPartData`. When the current part is identified as a rubber bushing, condition reads are returned as `100` and condition writes are skipped for that part only.

For QuickShop, the plugin patches:

- `PartScript.SetMouseOver(bool)`
- `PartScript.Update`
- `PartScript.OnDisable`
- `PartScript.OnDestroy`

The patch remembers the hovered repair-screen `PartScript`, listens for `B`, reads the part ID and price from vanilla data, adds a new `Item` to the vanilla inventory, and subtracts the vanilla price from player money.

## Requirements

- Windows/Steam version of `Car Mechanic Simulator 2021`
- .NET SDK 9 or newer for building
- BepInEx Unity IL2CPP x64

Important: CMS 2021 uses `GameAssembly.dll`, so Mono BepInEx builds are not enough. The game root should contain:

```text
BepInEx\core\BepInEx.Unity.IL2CPP.dll
BepInEx\core\Il2CppInterop.Runtime.dll
```

For the tested CMS 2021 setup, set `UnityLogListening = false` in `BepInEx\config\BepInEx.cfg` if the IL2CPP chainloader fails while setting up Unity logging.

## Build

If the game is installed at the path used by this project:

```powershell
dotnet build src\NoRubberBushingWear.csproj -c Release
```

For a different install path:

```powershell
dotnet build src\NoRubberBushingWear.csproj -c Release -p:GameRoot="C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2021"
```

Build output:

```text
src\bin\Release\NoRubberBushingWear.dll
```

## Install

1. Install BepInEx Unity IL2CPP x64 into the CMS 2021 game root.
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
No Rubber Bushing Wear 1.1.0
QuickShop enabled
```

## Validation Checklist

- New saves load and play normally.
- Existing saves load and play normally.
- Rubber Bushings never appear as faulty during diagnostics.
- Small Rubber Bushings never appear as faulty during diagnostics.
- Story missions and repair orders can still be completed.
- Other suspension components can still fail normally.
- On the repair screen, hovering a car part and pressing `B` buys one replacement item.
- The player money decreases by the vanilla part price.
- The purchased item appears in inventory.
- BepInEx logs show no new errors or warnings from this plugin.

## Limitations

- This is a runtime Harmony patch, not a save editor.
- QuickShop targets repair-screen car part hover. It does not add a hotkey inside the parts shop UI.
- The implementation intentionally avoids changing unrelated systems.
- Game updates may rename or restructure internal methods, requiring revalidation.
- No configuration file is provided by design.

## Development Notes

Research notes are kept in [docs/research.md](docs/research.md).

The currently tested local setup used:

- CMS 2021 Steam install
- BepInEx Unity.IL2CPP win-x64 `6.0.0-be.752`
- `UnityLogListening = false`
- .NET SDK `9.0.315`
