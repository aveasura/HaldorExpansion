# Haldor Expansion

[English](README.md) | [Русский](README.ru.md)

Valheim mod for BepInEx + Jotunn that expands Haldor's trading and adds unique items with custom combat mechanics.

## Installation for players

For normal installation, you do not need to change the code, configure `.csproj`, create `HaldorExpansion.Local.props`, or copy PNG files separately.

Just place the compiled file:

```text
HaldorExpansion.dll
```

into the plugin folder:

```text
BepInEx/plugins/aveasura-HaldorExpansion/
```

Item icons are already embedded inside `HaldorExpansion.dll` as embedded resources.

> `HaldorExpansion.Local.props` is only needed by developers for local project builds. Players do not need it.

## Important for multiplayer / dedicated server

This mod changes gameplay and adds networked mechanics, so on a dedicated server it must be installed both on the server and on all clients.

It is recommended to use the same mod version on the server and for all players.

## Features

- new items sold by Haldor;
- the ability to sell trophies and some rare materials, including `SurtlingCore`, `BlackCore`, and `MoltenCore`;
- unique items with custom combat mechanics;
- custom armor and cloaks with risky effects;
- multiplayer / dedicated server support via `NetworkCompatibility(EveryoneMustHaveMod, Minor)`;
- English and Russian localization for added items and effects.

## Added unique items

- `Bone Crushers` - cestus with an active ability, protective barrier, and shockwave;
- `Cuirass of Silent Reckoning` - armor that delays part of dangerous incoming damage;
- `Cloak of the Wounded Beast` - cloak with risky regeneration at low health;
- `Cloak of Burned Resolve` - cloak that redirects part of incoming damage into stamina loss;
- `Corrupted Brisingamen` - accessory for increased carry weight;
- `Crossbow of the Shadow Hunt` - heavy crossbow for a powerful opening shot;
- `Pit King's Cuirass` - item for aggressive cestus-focused gameplay.

## Recommended playstyle

The mod works best on higher difficulty, where additional items feel less like free power and more like risky survival tools.

For a more hardcore playthrough, it pairs well with:

- `Smoothbrain-CreatureLevelAndLootControl` - stronger creatures;
- `warpalicious-Monster_Modifiers` - additional creature modifiers;
- `ASharpPen-Custom_Raids` - more dangerous and frequent raids.

These mods are not required dependencies.

## Compatibility

Required dependencies:

- BepInEx 5;
- Jotunn.

The mod was tested in a multiplayer / dedicated server environment together with other gameplay mods, including:

- `Smoothbrain-CreatureLevelAndLootControl`;
- `warpalicious-Monster_Modifiers`;
- `ASharpPen-Custom_Raids`;
- `Azumatt-AzuExtendedPlayerInventory`;
- `Azumatt-AzuSkillTweaks`;
- `rendl0449-CraftFromContainers`;
- `Advize-PlantEverything`;
- `Marf-FuelEternal`.

Full compatibility with every version of third-party mods is not guaranteed, but no conflicts were found in my test setup.

## Runtime assets

Item icons are embedded inside `HaldorExpansion.dll` as embedded resources.

Players do not need to copy PNG files separately.

Source PNG files are stored in the project folder:

```text
HaldorExpansion/Assets/Icons/
```

They are only needed when building the project from source.

## Development requirements

- Valheim;
- BepInEx 5;
- Jotunn;
- .NET Framework 4.8;
- Visual Studio or Rider.

The project uses local paths to Valheim/r2modman DLLs in `HaldorExpansion/HaldorExpansion.csproj`. These paths are stored as MSBuild properties:

- `R2ModmanProfileDir`;
- `ValheimInstallDir`;
- `ValheimDedicatedServerDir`;
- `ClientPluginDeployDir`;
- `ServerPluginDeployDir`.

If your folders are different, use `HaldorExpansion.Local.props` as described below in the Local build paths section.

## Local build paths

If your Valheim, r2modman, or dedicated server paths are different from the defaults in `.csproj`, create a local settings file:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

You can do this by copying the provided example:

```text
HaldorExpansion/HaldorExpansion.Local.props.example
```

and renaming the copy to:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

After that, change the paths inside `HaldorExpansion.Local.props` for your machine.

`HaldorExpansion.Local.props` should not be committed to Git because it contains machine-specific local paths.

## Status

Current version: working development/test build. The codebase is still being improved and cleaned up.
