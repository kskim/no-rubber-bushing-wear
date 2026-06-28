# No Rubber Bushing Wear

[한국어 README](README.ko.md)

A small `Car Mechanic Simulator 2021` BepInEx plugin with two focused quality-of-life changes:

- `Rubber Bushing` and `Small Rubber Bushing` never count as faulty.
- In the parts shop, hover a part and press `B` to buy one immediately.

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

- Open the in-game parts shop.
- Hover a shop part with the mouse.
- Press `B`.
- The mod submits the hovered item through the vanilla shop flow and immediately buys one item.

QuickShop intentionally uses the game's existing shop purchase logic. It does not create items manually, bypass money checks, or alter inventory data directly.

## How It Works

CMS 2021 on Steam is an IL2CPP Unity game. This mod runs as a BepInEx IL2CPP + Harmony plugin.

For rubber bushings, the plugin patches the generated interop condition accessors for `PartData` and `BodyPartData`. When the current part is identified as a rubber bushing, condition reads are returned as `100` and condition writes are skipped for that part only.

For QuickShop, the plugin patches:

- `CMS.UI.Logic.Shop.ShopItem.OnPointerEnter`
- `CMS.UI.Logic.Shop.PartsShopPage.HandleInput`
- `CMS.UI.Windows.ShopBuyWindow.PrepareForItem`

The patch remembers the hovered shop item, listens for `B` while the parts shop is active, then reuses the vanilla `SubmitItem` and `BuyItem` path.

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
- In the parts shop, hovering a part and pressing `B` buys one item.
- Typing in the parts shop search field is not interrupted by QuickShop.
- BepInEx logs show no new errors or warnings from this plugin.

## Limitations

- This is a runtime Harmony patch, not a save editor.
- QuickShop currently targets the parts shop UI. It does not yet buy directly from car-assembly hover targets outside the shop.
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
