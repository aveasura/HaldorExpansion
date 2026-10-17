# Haldor Expansion

[English](README.md) | [Русский](README.ru.md) | [Changelog](CHANGELOG.md)

**Version: 1.3.0**

Haldor Expansion is a Valheim mod for **BepInEx 5 + Jotunn** that expands Haldor's shop with **13 unique pieces of equipment**, distinctive combat mechanics, and configurable prices, quantities, and world-progression requirements. Version 1.3.0 also enables upgrades for **12 artifacts** at the **Forge of Possibilities**, with **11 playable awakened powers** unlocked at quality 10.

Every relic offers **powerful advantages with meaningful drawbacks**. The mod supports **English and Russian**, multiplayer, and dedicated servers. Haldor can also buy trophies, cores, and idols.

## What's new in 1.3.0

- **Berserker:** new Greataxe, Helmet, and Cloak with escalating Wrath, Blood Debt, Reckoning, and a devastating meteor-storm ability, **Ragnarök**.
- **Forge of Possibilities:** upgrade 12 unique artifacts **beyond quality 10**. At quality **10+**, 11 artifacts awaken new abilities or passive effects. Their powers stay active at higher qualities. **Hel's Oath's awakening is still in development.**
- **Facebreaker:** a new Bone Crushers ability with a custom casting animation and impact sounds. Pull enemies from both sides, slow them, and stagger them when both sides are caught.
- **Haldor's Idols:** the regular Haldor Idol can drop alongside existing idols. The new **Haldor's Adamantine Idol** offers a safer upgrade attempt: on failure, equipment loses one quality level instead of being destroyed (minimum quality 1).
- **Idol trading:** Haldor buys and sells **all 16 vanilla idols and both custom HaldorExpansion idols**, one at a time, with individually configurable prices and availability.
- **Improved Haldor shop:** six category tabs, better item previews, scrolling tooltips, and faster navigation.
- **Artifact lore:** 12 relics and both Haldor idols feature Norse-inspired stories in English and Russian, with hidden connections between their legends.
- **In-game settings:** optional **BepInEx ConfigurationManager (F1)** support, a customizable Blood Debt HUD, and live updates to configuration files without restarting.
- **Combat and quality-of-life fixes:** refined existing equipment interactions, fixed equipment visibility issues, and improved configuration migration.

### 🎬 Ragnarök — Ability Showcase

[![Ragnarök Ability Showcase](https://img.youtube.com/vi/U9n6rRKwHu8/hqdefault.jpg)](https://youtu.be/U9n6rRKwHu8)

*Watch the Berserker unleash Ragnarök in-game.*

See the [changelog](CHANGELOG.md) for the full list of changes since 1.2.2.

## Installation

Install **BepInEx 5** and **Jotunn** (mod managers can handle these dependencies), then place the compiled mod in:

```text
BepInEx/plugins/aveasura-HaldorExpansion/HaldorExpansion.dll
```

No additional PNG files are required: item icons are embedded in the DLL. You do **not** need to edit `.csproj` or create `HaldorExpansion.Local.props` to play. Those are developer-only build settings.

### Multiplayer / dedicated server

Install the same HaldorExpansion version **on the dedicated server and on every player's client**. The mod uses `NetworkCompatibility(EveryoneMustHaveMod, Minor)`.

The server is authoritative for synchronized combat-balance settings, shop prices, stock configuration, and progression requirements. Connected clients use the server's values without permanently overwriting their own local configuration. Shop-tab visibility and local input preferences can be adjusted individually.

## Haldor's shop and item descriptions

Haldor sells unique equipment, resources, and crafting materials. Depending on world progression and `trading.yml`, some products may be hidden until their requirements are met.

The shop offers **six tabs: All / Equipment / Wood / Ores and Metals / Resources / Other**. All idols appear in **Other** (there is no separate idol tab). Category visibility is configurable in `general.cfg` or ConfigurationManager (F1). Hiding a tab does not hide its goods from **All** unless **Hide items from disabled tabs** is also enabled. Disable the entire tab interface to restore Haldor's traditional layout. Disabling and re-enabling an item retains its catalog order.

For the 12 relics with stories **and both Haldor idols**, **shop previews emphasize the legend**, while the item's **inventory tooltip includes its story and mechanical details**. The Crossbow of the Shadow Hunt retains a conventional description pending a future redesign.

Long item descriptions scroll using the mouse wheel while the pointer is over the tooltip, or with **Page Up / Page Down**. This also helps on smaller displays.

## Unique equipment

These are the **default purchase prices**. You can change them in `trading.yml`.

| Item | Coins | Main role |
| --- |------:| --- |
| **Berserker Greataxe** |  2500 | Missing-health damage, Wrath, Blood Debt, Reckoning |
| **Berserker Helmet** |  2500 | Wrath-based defense and lifesteal |
| **Berserker Cloak** |  2500 | Faster Blood Debt generation. Unlocks Ragnarök at the cost of survivability |
| **Bone Crushers** |  2500 | Cestus that turns damage suffered into a barrier and shockwave |
| **Pit King's Mantle** |  3500 | Health bonus and Steel Heart synergy with Bone Crushers |
| **Hel's Oath** |  2500 | Sacrifices maximum health for stamina. Grants Hel's Touch and Hel's Embrace |
| **Pelt of Helheim** |  2500 | Very Resistant to Pierce when the Hel/Fenris synergy is complete |
| **Cuirass of Silent Reckoning** |  3500 | Delays part of dangerous damage instead of preventing it outright |
| **Thread of the Norns** |  6666 | One-use protection from a lethal hit |
| **Cloak of the Wounded Beast** |  2500 | Stronger healing at low health, but increased incoming damage |
| **Cloak of Burned Resolve** |  2500 | Redirects part of incoming damage into stamina |
| **Corrupted Brisingamen** |  1600 | Increases carrying capacity |
| **Crossbow of the Shadow Hunt** |  2500 | Powerful opening shots with a mobility trade-off |

### Berserker — Wrath and Blood Debt

The **Greataxe, Helmet, and Cloak** complement each other, but they are **not a formal armor set** and do not require specific chest armor or leggings.

- **Bloodthirst:** the Greataxe deals **+0.2% weapon damage for each point of missing HP**.
- **Wrath I–V:** valid Greataxe hits build up to five stages. Each stage grants attack speed and reduces stamina costs. Wrath gradually fades without new hits. By default, the stage I–V attack-speed bonuses are **+20/+30/+38/+45/+50%**, with stamina costs reduced by **up to 35% at Wrath V** (configurable).
- **Blood Debt:** generated by eligible damage suffered and Greataxe hits. It gradually fades outside combat. Its value appears on a dedicated HUD indicator.
- **Reckoning:** with **Wrath II or higher** and sufficient Blood Debt (**100** by default), the Greataxe's secondary attack becomes a powerful strike. On a successful hit it spends some Blood Debt, deals extra damage, and restores health from direct damage dealt.
- **Berserker Helmet:** increases defense as Wrath rises (up to **+25 armor**, **75% less stagger buildup**, and **5% less incoming damage** at Wrath V). Qualifying Greataxe hits restore health based on actual enemy HP lost (up to **5%** at Wrath V).
- **Berserker Cloak:** increases incoming damage by **30%**, but boosts eligible Blood Debt generation by **50%**.
- **Ragnarök:** requires the **Greataxe in hand, Cloak equipped, Wrath V, and enough Blood Debt** (**500** by default). Press **E** by default (configurable) to call down a dynamically scaled rain of **12–30 meteors**. Casting consumes all Wrath and Blood Debt and causes **Exhaustion** (by default, **-40% movement speed, -25% attack speed, and -40% stamina regeneration for 7 seconds**).

Most Berserker combat settings, including thresholds, bonuses, and the Ragnarök key, can be adjusted in the configuration. Berserker equipment can be repaired at a Forge.

### Steel Heart — Bone Crushers and Pit King's Mantle

**Bone Crushers** are fast, aggressive cestus weapons. Taking real HP damage builds **Battle Fervor**. The charge gradually fades if no additional damage is taken. At full charge, activate **Retribution** with the **middle mouse button (Mouse2)** by default. It releases a nearby shockwave and grants a temporary protective barrier. Charge cannot build while the barrier is active.

**Facebreaker:** hold the **right mouse button (Mouse1 / RMB)** to aim, then release it to pull nearby enemies inward from both sides. Pulled enemies are slowed for **4 seconds**. When enemies are caught on **both sides**, pulled targets also stagger. The ability uses a custom animation and synchronized impact sounds. Default cooldown: **5 seconds**. The key can be changed. While Facebreaker is ready, its default RMB binding replaces blocking while aiming.

Bone Crushers also grant **+10 maximum health**, **+90% attack speed**, and **-8 armor** while equipped.

**Pit King's Mantle** is chest armor despite its name. It provides **+20 maximum health** alone, or **+60** with Bone Crushers. Together, the two items grant **+70 maximum health in total** and form the **Steel Heart** set. The set increases Retribution's barrier strength and shockwave damage by **50%**.

### Hel's Oath and Pelt of Helheim

**Hel's Oath** is a high-risk bow that converts **50% of maximum health into maximum stamina** and grants **+20 Bows skill** while equipped.

When wielding the bow with the **full vanilla Fenris set**, dealing bow damage awakens **Hel's Touch**. It grows to five stages while avoiding combat hits. Taking a qualifying hit removes one stage. Every stage provides **+5% movement speed** and **-4% stamina cost**, reaching **+25% / -20%** at Touch V. Breaking the equipment requirements removes the bonuses immediately. Stored stages have a brief grace period before decaying.

Normal bow hits charge **Hel's Embrace** based on outgoing damage before target armor and resistances. The required charge scales with world boss progression. Press **Mouse2** by default when fully charged: the **next arrow** becomes empowered and explodes on impact (**80% Lightning / 20% Frost**). Damage scales with the health/stamina trade-off, and **Hel's Touch V** greatly strengthens the explosion. The empowered arrow does not charge the next ability.

**Pelt of Helheim** activates **Helheim's Ward** only while the Pelt, full Fenris set, Hel's Oath, and active **Hel's Touch V** are all present. The effect grants **Very Resistant to Pierce** and disappears when the requirements are lost.

### Other relics

- **Cuirass of Silent Reckoning (Delayed Doom):** triggers when a single post-defense hit exceeds **50% of maximum HP** or would otherwise be lethal. **50% of that damage hits immediately**, while **70% of the original hit** returns as damage debt over **6 seconds**. This is borrowed time, not immunity: extremely strong hits can still kill you.
- **Thread of the Norns:** a one-use trinket that is destroyed when saving the wearer from lethal damage. Health becomes **50% of its pre-hit value**, all accumulated Delayed Doom debt is cleared, and health loss is prevented for **3 seconds**. Remaining damage-over-time effects may resume after the protection ends.
- **Cloak of the Wounded Beast:** increases incoming damage by **20%**, but periodically heals the wearer more strongly at lower health. Healing is halved during relevant fire, poison, or spirit damage-over-time effects, and is also halved when combined with the Cuirass of Silent Reckoning.
- **Cloak of Burned Resolve:** redirects **25% of incoming damage** into stamina. If there is insufficient stamina, the remaining HP damage is increased by **75%**. Stamina is consumed to absorb damage. This is not a free damage reduction.
- **Corrupted Brisingamen:** adds **+150 carrying capacity**. It has no additional curse or drawback.
- **Crossbow of the Shadow Hunt:** a heavy crossbow designed for powerful opening/backstab shots with a movement-speed penalty.

## Forge of Possibilities and Awakened Potential

**12 of the 13 unique items** can be upgraded at the Forge of Possibilities. The **Haldor Idol** uses the normal success/failure rules: an unsuccessful upgrade **may destroy the item**. The **Adamantine Haldor Idol** offers a separate upgrade option for equipment already supported by the forge. It uses the **same success chance** as the regular Haldor Idol: success increases quality by **1**, while failure decreases it by **1**, but **never below quality 1**. The equipment **cannot be destroyed** by an adamantine attempt. Each attempt consumes **one idol**, including failed attempts at quality 1. The **Thread of the Norns** is not one of the upgradeable artifacts.

**Quality 10 is the awakening threshold, not the upgrade cap.** You can continue upgrading above 10, and awakened effects remain available at **quality 11, 12, and beyond** while the relevant item is equipped and its other requirements are met. If an adamantine failure lowers an item from quality 10 to 9, its awakening becomes inactive until it reaches quality 10 again. The awakening belongs to the **particular upgraded item**, not to all copies of that artifact.

<details>
<summary>Show awakened abilities at quality +10 (spoilers)</summary>

| Artifact | Awakened effect at quality 10+                                                                                      |
| --- |---------------------------------------------------------------------------------------------------------------------|
| **Berserker Greataxe** | **Bloody Harvest:** Greataxe kills grant **30 Blood Debt**.                                                         |
| **Berserker Helmet** | **Feast of the Fallen:** kills with the Greataxe during Wrath heal **10% of missing health**.                       |
| **Berserker Cloak** | Ragnarök **preserves Wrath**. Blood Debt consumption and Exhaustion remain.                                         |
| **Bone Crushers** | Normal attacks heal **3% of actual enemy HP lost**.                                                                 |
| **Pit King's Mantle** | With Bone Crushers, raises the Steel Heart barrier bonus to **+100%** and increases Retribution damage accordingly. |
| **Hel's Oath** | **Ghosts of the Last Hunt — work in progress.** No awakened ability yet.                                            |
| **Pelt of Helheim** | **+80 maximum stamina** and **80% less fall damage**.                                                               |
| **Cuirass of Silent Reckoning** | With active delayed-damage debt and health at **25% or below**, restores **2% of maximum HP per second**.           |
| **Cloak of the Wounded Beast** | **+30 maximum health** and **+30 effective Unarmed skill** (capped at 100).                                         |
| **Cloak of Burned Resolve** | Slight resistance to slashing damage (**25% reduction** at the resistance stage).                                   |
| **Corrupted Brisingamen** | **Blessing of the Rejected Goddess:** **+40% stamina regeneration**.                                                |
| **Crossbow of the Shadow Hunt** | **+60 effective Sneak** and **+30 effective Crossbows** (each capped at 100).                                       |

</details>

Awakened abilities do not require a separate activation key. The effect is displayed in each item's tooltip as its quality changes. **Hel's Oath** can still be upgraded above +10. Its awakening will be introduced in a future update.

## Trading: resources, idols, and sellable items

Haldor sells crafting materials at configurable prices. The amounts below are the **defaults for a newly generated configuration**. Prices apply to the **whole stack shown**, not necessarily one unit. An entry marked *full stack* uses the item's maximum stack size.

| Materials | Default stack | Price (coins) |
| --- | ---: | ---: |
| Copper / Tin / Iron / Silver / Flametal | 30 each | 150 / 150 / 250 / 450 / 1000 |
| Black Metal | Full stack | 200 |
| Stone / Grausten / Black Marble | Full stack | 25 / 50 / 50 |
| Wood / Core Wood / Ancient Bark / Fine Wood | Full stack | 25 / 50 / 100 / 200 |
| Yggdrasil Wood / Ashwood | Full stack | 300 / 100 |
| Red Jute / Blue Jute / Linen Thread | Full stack | 300 / 300 / 800 |
| Sap / Refined Eitr / Soft Tissue | Full stack | 400 / 900 / 400 |
| Resin / Greydwarf Eye / Crystal / Obsidian / Needle / Tar | Full stack | 100 / 100 / 250 / 250 / 250 / 250 |
| Feathers | 100 | 150 |

### Idols

Haldor **buys and sells 18 idols**: all **16 standard battle/protection idols**, the **Haldor Idol**, and **Haldor's Adamantine Idol**. Idols appear under the **Other** shop tab, not in a separate category. Each idol is purchased **one at a time**, even if its inventory stack size is 20.

| Idol | Haldor sells to you | Haldor buys from you |
| --- | ---: | ---: |
| Each of the 16 standard idols | **1000 coins per idol** | **300 coins per idol** |
| **Haldor Idol** | **1000 coins per idol** | **300 coins per idol** |
| **Haldor's Adamantine Idol** | **4000 coins per idol** | **2500 coins per idol** |

All these prices are configurable **individually** in `trading.yml` or via **F1** with the optional ConfigurationManager. Each idol's availability and world-progression requirement are independently configurable. The regular Haldor Idol can drop alongside standard idols. The Adamantine Haldor Idol has an **independent 1% chance per eligible chest** by default (configurable in `balance.cfg`); these additional rolls do not replace standard loot or regular Haldor Idol drops.

You can also **sell supported trophies** (including Deep North and boss trophies) and **Surtling Core, Black Core, and Molten Core** to Haldor. Prices can be configured per item, with a fallback for trophies lacking an individual entry. Setting a **sell** price to `0` disables selling that item. Your existing/migrated prices may differ from the defaults listed here.

## Configuration (1.3.0)

On first launch, the mod generates three files in:

```text
BepInEx/config/aveasura-HaldorExpansion/
├── general.cfg   # Local controls and shop UI
├── balance.cfg   # Gameplay and ability balance
└── trading.yml   # Shop inventory, prices, progression and sell prices
```

### Live reloading

Changes to these files are automatically checked while the game runs. The default **configuration check interval is 3 seconds**, and the minimum is **0.5 seconds**. The system checks file metadata before performing a full read and waits for a stable change before applying it.

Edit the files on the **server** to change authoritative multiplayer settings. A client's local `balance.cfg` and `trading.yml` cannot override active server values. Invalid edits are rejected and the last accepted settings remain in use. See the BepInEx log for details.

### `general.cfg`

For example, edit the existing generated entries:

```ini
[Configuration]
Config check interval (seconds) = 3

[Trading UI]
Enable Haldor tabs = true
```

Set `Enable Haldor tabs = false` to restore the original shop layout. To remove goods from a disabled category as well, turn on `Hide items from disabled tabs`. Disabling the **Other** category with that option also hides idols.

**Optional F1 configuration:** install **BepInEx ConfigurationManager** to edit mod settings in-game. In its F1 interface, **Trading UI** appears directly below **Compatibility**. You can adjust idol buy/sell prices, individual purchase availability, tab visibility, and the Blood Debt HUD without manually editing text files. ConfigurationManager is optional. The mod works without it.

### `balance.cfg`

Contains synchronized settings for Hel's Oath, the Berserker's Wrath/Blood Debt/Reckoning/Ragnarök, and other configurable combat effects. For example, Hel's Oath provides **Damage for full charge**, **Explosion radius**, **Embrace coefficient**, **Touch V Embrace coefficient**, and **Touch stack interval**. Ragnarök has configurable thresholds, casting time, Exhaustion, and its ability key. **Forge of Possibilities → Adamantine Idol chest drop chance** controls the independent chest drop rate (**1%** by default; `0` disables it, maximum `100`).

**Edit the entries already generated by the mod**, rather than copying old configuration sections from 1.2.x. The **Ragnarök key** and other local button bindings are stored with input settings, not in `trading.yml`.

### `trading.yml`

The file is generated with **all recognized items** under `shop:` and `sell:`. The example below is **only an excerpt from a generated file**. Do not replace the complete document with it:

```yaml
shop:
  Bone_crushers:
    price: 2500
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  aveasura_he_idol:
    price: 1000
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  aveasura_he_adamantine_idol:
    price: 4000
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  Iron:
    price: 250
    enabled: true
    stack: 30
    requiredGlobalKey: "defeated_gdking"

sell:
  TrophyEikthyr:
    price: 70
  SurtlingCore:
    price: 50
  aveasura_he_idol:
    price: 300
  aveasura_he_adamantine_idol:
    price: 2500
```

In this **example**, Iron is locked behind the Elder. The default value in your generated file may be different. Keep the original entries for all other products and trophies.

- `price`: shop purchase price for the **entire stack** (**per one idol** for idols). In `shop:`, `0` means **free**. In `sell:`, `0` **disables selling**.
- `enabled`: `false` hides the item from Haldor's **buy** list.
- `stack`: how many units are sold together. `0` uses the prefab's maximum stack size. Other values cannot exceed it. **All idols are always sold as `stack: 1`, even if a different value is entered.**
- `requiredGlobalKey`: required world key, or `""` for no restriction.
- Item identifiers such as `Bone_crushers` are **internal prefab IDs**, not localized display names. Keep them unchanged.
- Preserve `shop:` and `sell:`, all generated item entries, field names, and indentation (**spaces only, no tabs**). This file supports a restricted YAML format, not every YAML feature.

Common boss progression keys:

```text
defeated_eikthyr     Eikthyr
defeated_gdking      The Elder
defeated_bonemass    Bonemass
defeated_dragon      Moder
defeated_goblinking  Yagluth
defeated_queen       The Queen
defeated_fader       Fader
defeated_frozenking  Kall Fimbulbringer / Deep North progression
```

Use the actual keys supported by your installed Valheim version and world.

### Upgrading from 1.2.x

Version 1.3.0 migrates the old configuration into the new files when possible. The previous config is **backed up**, and upgrades can add newly introduced settings while retaining existing values. Backups and format metadata are stored inside the dedicated HaldorExpansion config directory.

Avoid deleting individual entries from a migrated configuration: the runtime validates its expected structure. If an edit is invalid, check the BepInEx log and restore the last known-good backup. Keep your own copy before major manual changes.

## Compatibility and optional mods

HaldorExpansion requires **BepInEx 5** and **Jotunn**. It was previously used in local and dedicated-server environments with mods including:

- `Smoothbrain-CreatureLevelAndLootControl`
- `warpalicious-Monster_Modifiers`
- `ASharpPen-Custom_Raids`
- `Azumatt-AzuExtendedPlayerInventory`
- `Azumatt-AzuSkillTweaks`
- `rendl0449-CraftFromContainers`
- `Advize-PlantEverything`
- `Marf-FuelEternal`
- `...`

These are **not required dependencies**. Compatibility depends on the versions installed. Earlier testing is not a guarantee for every 1.3.0 mod combination.

## Building from source

Requirements: Valheim, BepInEx 5, Jotunn, **.NET Framework 4.8**, and Visual Studio or Rider.

The C# project is located at `HaldorExpansion/HaldorExpansion.csproj`. Developer-specific Valheim / r2modman / dedicated-server paths can be supplied through the optional file:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

Copy `HaldorExpansion/HaldorExpansion.Local.props.example`, rename it to `HaldorExpansion.Local.props`, and adjust the paths for your machine. The file is local-only and should **not** be committed to Git.

Available MSBuild path properties include `R2ModmanProfileDir`, `ValheimInstallDir`, `ValheimDedicatedServerDir`, `ClientPluginDeployDir`, and `ServerPluginDeployDir`.

Source PNGs are in `HaldorExpansion/Assets/Icons/` and are embedded in the DLL when built. Players do not need these files.

### Contact & Support

Have questions, found a bug, or want to share feedback?

You can reach me on Discord: **@aveasura**

Bug reports, suggestions, and feedback are always welcome!

## ❤️ Support Development

HaldorExpansion is free and will always remain free.

If you enjoy the mod and would like to support its continued development, you can make a one-time donation or become a supporter on [Boosty](https://boosty.to/aveasura).

Your support is entirely optional, but always appreciated. Thank you! 🍻