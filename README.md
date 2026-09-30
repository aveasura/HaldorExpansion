# Haldor Expansion

[English](README.md) | [Русский](README.ru.md)

Valheim mod for BepInEx + Jotunn that expands Haldor with configurable trading, trophy/core selling, world-progression requirements, and unique items with custom combat mechanics.

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

Shop prices, world-progression requirements, trophy sell prices, and core sell prices are synchronized from the server. While connected to a dedicated server, the server configuration is authoritative.

## Features

- new unique items sold by Haldor;
- additional resources and crafting materials sold by Haldor;
- configurable prices for all items added to Haldor's shop;
- configurable world-progression requirements for all shop items;
- configurable trophy sell prices;
- configurable sell prices for `SurtlingCore`, `BlackCore`, and `MoltenCore`;
- trophy selling, including Deep North trophies;
- fallback sell price for unlisted trophies;
- unique items with custom combat mechanics;
- custom armor and cloaks with risk-reward effects;
- custom active ability for the Bone Crushers cestus;
- server-synchronized shop and sell-price configuration;
- multiplayer / dedicated server support via `NetworkCompatibility(EveryoneMustHaveMod, Minor)`;
- English and Russian localization for added items and effects.

## Added unique items

Default shop prices are shown below. All of these prices can be changed in the config.

| Item | Default price | Description |
| --- | ---: | --- |
| `Bone Crushers` | 2500 | Cestus with an active ability, protective barrier, and shockwave. |
| `Cuirass of Silent Reckoning` | 4000 | Armor that delays part of dangerous incoming damage. |
| `Cloak of the Wounded Beast` | 2500 | Cloak with risky regeneration that becomes stronger at low health. |
| `Cloak of Burned Resolve` | 2500 | Cloak that redirects part of incoming damage into stamina loss. |
| `Corrupted Brisingamen` | 1600 | Accessory that increases maximum carry weight. |
| `Thread of the Norns` | 6666 | One-use trinket: prevents lethal damage, is destroyed, and leaves the wearer at half of their pre-hit health. |
| `Crossbow of the Shadow Hunt` | 2500 | Heavy crossbow for powerful opening shots. |
| `Pit King's Cuirass` | 4000 | Cestus-focused armor for aggressive close combat. |

## Configuration

The configuration file is created automatically after the mod starts.

### Shop prices

Every item added to Haldor's shop has its own configurable price.

Example:

```ini
Grit Cestus Price = 2500
Iron Price = 225
Silver Price = 550
```

These values are synchronized from the server in multiplayer.

### World progression requirements

Every item added to Haldor's shop can optionally require a Valheim world global key before it appears.

Example:

```ini
Grit Cestus RequiredGlobalKey = defeated_bonemass
Silver RequiredGlobalKey = defeated_gdking
```

Leave the value empty to keep the item available immediately:

```ini
Grit Cestus RequiredGlobalKey =
```

Common boss progression keys:

```text
defeated_eikthyr     - Eikthyr
defeated_gdking      - The Elder
defeated_bonemass    - Bonemass
defeated_dragon      - Moder
defeated_goblinking  - Yagluth
defeated_queen       - The Queen
defeated_fader       - Fader
defeated_frozenking  - Kall Fimbulbringer
```

### Trophy and core sell prices

Supported trophies have individual configurable sell prices.

Example:

```ini
Eikthyr Trophy Price = 70
Bonemass Trophy Price = 150
Fader Trophy Price = 1000
```

Set a trophy sell price to `0` to disable selling that trophy.

A fallback is also available for trophy prefabs that are not explicitly listed:

```ini
Unlisted Trophy Price = 10
```

This can cover trophies added by other mods or future game updates.

The three additional rare materials also have configurable sell values:

```ini
Surtling Core Price = 50
Black Core Price = 100
Molten Core Price = 150
```

Set a value to `0` to disable selling that item.

### Cestus ability key

The Bone Crushers active ability uses the middle mouse button by default:

```ini
Ability Key = Mouse2
```

Common mouse values:

```text
Mouse0 = Left Mouse Button
Mouse1 = Right Mouse Button
Mouse2 = Middle Mouse Button
```

Other Unity `KeyCode` values can also be used.

### Multiplayer configuration

Shop prices, progression requirements, trophy sell prices, and core sell prices are synchronized from the server.

On a dedicated server, the server configuration is authoritative. If a client has different local values, the server values are used while connected. The client's local config file is not permanently overwritten.

## Recommended playstyle

The mod works best on higher difficulty, where the additional items feel less like free power and more like risky survival tools.

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

Current version: **1.3.11**.

The mod supports Valheim 1.0 and has been tested in both local and dedicated-server environments.
