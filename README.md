# Haldor Expansion

[English](README.md) | [Русский](README.ru.md) | [Changelog](CHANGELOG.md)

**Version: 1.3.0 (unreleased)**

Haldor Expansion is a Valheim mod for **BepInEx 5 + Jotunn** that expands Haldor's trading, adds 13 unique items with custom combat mechanics, and lets you configure item availability, prices, quantities, and world-progression requirements. It also supports selling trophies and selected rare materials.

The equipment is designed around **powerful advantages with meaningful drawbacks**, including several complementary playstyles. The mod supports **English and Russian**, multiplayer, and dedicated servers.

## What's new in 1.3.0

- **Berserker equipment:** a Greataxe, Helmet, and Cloak built around Wrath, Blood Debt, Reckoning, and the meteor-based Ragnarök ability.
- **Live configuration updates:** changes to `general.cfg`, `balance.cfg`, and `trading.yml` are detected without restarting the game.
- **Haldor's shop categories:** optional **All**, **Equipment**, **Resources**, and **Other** tabs.
- **Artifact lore:** original interconnected Norse-inspired stories for 12 relics. Haldor's item previews show lore; inventory tooltips explain the mechanics.
- **Scrollable descriptions:** use the **mouse wheel**, **Page Up**, or **Page Down** to read longer inventory tooltips.
- **Combat and quality-of-life improvements:** updated Delayed Doom/Norn Thread interactions, equipment rendering on first load, configuration migration, and shop performance.

See the [changelog](CHANGELOG.md) for the full list of changes from 1.2.2.

## Installation

Install **BepInEx 5** and **Jotunn** (mod managers can handle these dependencies), then place the compiled mod in:

```text
BepInEx/plugins/aveasura-HaldorExpansion/HaldorExpansion.dll
```

No additional PNG files are required: item icons are embedded in the DLL. You do **not** need to edit `.csproj` or create `HaldorExpansion.Local.props` to play; those are developer-only build settings.

### Multiplayer / dedicated server

Install the same HaldorExpansion version **on the dedicated server and on every player's client**. The mod uses `NetworkCompatibility(EveryoneMustHaveMod, Minor)`.

The server is authoritative for synchronized combat-balance settings, shop prices, stock configuration, and progression requirements. Connected clients use the server's values without permanently overwriting their own local configuration. Shop-tab visibility and local input preferences can be adjusted individually.

## Haldor's shop and item descriptions

Haldor sells unique equipment, resources, and crafting materials. Depending on world progression and `trading.yml`, some products may be hidden until their requirements are met.

The shop can show **All / Equipment / Resources / Other** categories. Disable the tabs in `general.cfg` to restore the traditional shop layout. Disabling and re-enabling a product in the trading configuration preserves its original position in the catalog.

For the 12 relics with stories, **shop previews emphasize the legend**, while the item's **inventory tooltip includes its story and mechanical details**. The Crossbow of the Shadow Hunt retains a conventional description pending a future redesign.

Long item descriptions scroll using the mouse wheel while the pointer is over the tooltip, or with **Page Up / Page Down**. This also helps on smaller displays.

## Unique equipment

These are the **default purchase prices**. You can change them in `trading.yml`.

| Item | Coins | Main role |
| --- | ---: | --- |
| **Berserker Greataxe** | 2500 | Missing-health damage, Wrath, Blood Debt, Reckoning |
| **Berserker Helmet** | 2500 | Wrath-based defense and lifesteal |
| **Berserker Cloak** | 2500 | Faster Blood Debt generation; unlocks Ragnarök at the cost of survivability |
| **Bone Crushers** | 2500 | Cestus that turns damage suffered into a barrier and shockwave |
| **Pit King's Cuirass** | 4000 | Health bonus and Steel Heart synergy with Bone Crushers |
| **Hel's Oath** | 2500 | Sacrifices maximum health for stamina; Hel's Touch and Hel's Embrace |
| **Pelt of Helheim** | 2500 | Very Resistant to Pierce when the Hel/Fenris synergy is complete |
| **Cuirass of Silent Reckoning** | 4000 | Delays part of dangerous damage instead of preventing it outright |
| **Thread of the Norns** | 6666 | One-use protection from a lethal hit |
| **Cloak of the Wounded Beast** | 2500 | Stronger healing at low health, but increased incoming damage |
| **Cloak of Burned Resolve** | 2500 | Redirects part of incoming damage into stamina |
| **Corrupted Brisingamen** | 1600 | Increases carrying capacity |
| **Crossbow of the Shadow Hunt** | 2500 | Powerful opening shots with a mobility trade-off |

### Berserker — Wrath and Blood Debt

The **Greataxe, Helmet, and Cloak** complement each other, but they are **not a formal armor set** and do not require specific chest armor or leggings.

- **Bloodthirst:** the Greataxe deals **+0.2% weapon damage for each point of missing HP**.
- **Wrath I–V:** valid Greataxe hits build up to five stages. Each stage grants attack speed and reduces stamina costs; Wrath gradually fades without new hits. By default, the stage I–V attack-speed bonuses are **+20/+30/+38/+45/+50%**, with **-5% stamina cost per stage** (configurable).
- **Blood Debt:** generated by eligible damage suffered and Greataxe hits; gradually fades outside combat. Its value appears on a dedicated HUD indicator.
- **Reckoning:** with **Wrath II or higher** and sufficient Blood Debt (**100** by default), the Greataxe's secondary attack becomes a powerful strike. On a successful hit it spends some Blood Debt, deals extra damage, and restores health from direct damage dealt.
- **Berserker Helmet:** each Wrath stage grants **+4 armor**, reduces stagger buildup and incoming damage, and restores health equal to **1% of actual HP damage dealt by qualifying Greataxe hits** (up to 5% at Wrath V).
- **Berserker Cloak:** increases incoming damage by **30%**, but boosts eligible Blood Debt generation by **50%**.
- **Ragnarök:** requires the **Greataxe in hand, Cloak equipped, Wrath V, and enough Blood Debt** (**500** by default). Press **E** by default (configurable) to call down a dynamically scaled rain of **12–30 meteors**. Casting consumes all Wrath and Blood Debt and causes **Exhaustion** (by default, **-40% movement speed, -25% attack speed, and -40% stamina regeneration for 7 seconds**).

Most Berserker combat settings, including thresholds, bonuses, and the Ragnarök key, can be adjusted in the configuration. Berserker equipment can be repaired at a Forge

### Steel Heart — Bone Crushers and Pit King's Cuirass

**Bone Crushers** are fast, aggressive cestus weapons. Taking real HP damage builds **Battle Fervor**; the charge gradually fades if no additional damage is taken. At full charge, activate **Counterstrike** with the **middle mouse button (Mouse2)** by default. It releases a nearby shockwave and grants a temporary protective barrier. Charge cannot build while the barrier is active.

Bone Crushers also grant **+10 maximum health**, **+90% attack speed**, and **-8 armor** while equipped.

**Pit King's Cuirass** is chest armor despite its story-inspired name. It provides **+20 maximum health** alone, or **+60** with Bone Crushers. Together, the two items grant **+70 maximum health in total** and form the **Steel Heart** set. The set increases Counterstrike's barrier strength and shockwave damage by **50%**.

### Hel's Oath and Pelt of Helheim

**Hel's Oath** is a high-risk bow that converts **50% of maximum health into maximum stamina** and grants **+20 Bows skill** while equipped.

When wielding the bow with the **full vanilla Fenris set**, dealing bow damage awakens **Hel's Touch**. It grows to five stages while avoiding combat hits; taking a qualifying hit removes one stage. Every stage provides **+5% movement speed** and **-4% stamina cost**, reaching **+25% / -20%** at Touch V. Breaking the equipment requirements removes the bonuses immediately; stored stages have a brief grace period before decaying.

Normal bow hits charge **Hel's Embrace** based on outgoing damage before target armor and resistances. The required charge scales with world boss progression. Press **Mouse2** by default when fully charged: the **next arrow** becomes empowered and explodes on impact (**80% Lightning / 20% Frost**). Damage scales with the health/stamina trade-off, and **Hel's Touch V** greatly strengthens the explosion. The empowered arrow does not charge the next ability.

**Pelt of Helheim** activates **Helheim's Ward** only while the Pelt, full Fenris set, Hel's Oath, and active **Hel's Touch V** are all present. The effect grants **Very Resistant to Pierce** and disappears when the requirements are lost.

### Other relics

- **Cuirass of Silent Reckoning (Delayed Doom):** triggers when a single post-defense hit exceeds **50% of maximum HP** or would otherwise be lethal. **50% of that damage hits immediately**, while **70% of the original hit** returns as damage debt over **6 seconds**. This is borrowed time, not immunity: extremely strong hits can still kill you.
- **Thread of the Norns:** a one-use trinket that is destroyed when saving the wearer from lethal damage. Health becomes **50% of its pre-hit value**, all accumulated Delayed Doom debt is cleared, and health loss is prevented for **3 seconds**. Remaining damage-over-time effects may resume after the protection ends.
- **Cloak of the Wounded Beast:** increases incoming damage by **20%**, but periodically heals the wearer more strongly at lower health. Healing is halved during relevant fire, poison, or spirit damage-over-time effects, and is also halved when combined with the Cuirass of Silent Reckoning.
- **Cloak of Burned Resolve:** redirects **25% of incoming damage** into stamina. If there is insufficient stamina, the remaining HP damage is increased by **75%**. Stamina is consumed to absorb damage; this is not a free damage reduction.
- **Corrupted Brisingamen:** adds **+150 carrying capacity**. It has no additional curse or drawback.
- **Crossbow of the Shadow Hunt:** a heavy crossbow designed for powerful opening/backstab shots with a movement-speed penalty.

## Trading: resources and sellable items

Haldor also sells materials at configurable prices. These default prices are **per stack**, not necessarily per individual unit.

| Materials | Default stack | Price (coins) |
| --- | ---: | ---: |
| Iron / Copper / Tin / Silver / Flametal | 30 each | 225 / 310 / 180 / 550 / 800 |
| Stone / Grausten / Black Marble | Full stack | 20 / 25 / 40 |
| Wood / Core Wood / Ancient Bark / Fine Wood | Full stack | 10 / 10 / 30 / 150 |
| Yggdrasil Wood / Ashwood / Blackwood / Charred Wood | Full stack | 250 / 30 / 100 / 50 |
| Red Jute / Blue Jute | Full stack | 250 / 250 |
| Resin / Greydwarf Eye / Tar / Crystal / Needle | Full stack | 100 / 200 / 250 / 250 / 250 |
| Feathers | 100 | 150 |

You can also **sell supported trophies** (including Deep North and boss trophies) and **SurtlingCore, BlackCore, and MoltenCore** to Haldor. Each supported item has its own price; trophies without a specific entry can use a configurable fallback price. Setting a **sell** price to `0` disables selling that item.

## Configuration (1.3.0)

On first launch, the mod generates three files in:

```text
BepInEx/config/aveasura-HaldorExpansion/
├── general.cfg   # Local controls and shop UI
├── balance.cfg   # Gameplay and ability balance
└── trading.yml   # Shop inventory, prices, progression and sell prices
```

### Live reloading

Changes to these files are automatically checked while the game runs. The default **configuration check interval is 2 seconds**, and the minimum is **0.5 seconds**. The system checks file metadata before performing a full read and waits for a stable change before applying it.

Edit the files on the **server** to change authoritative multiplayer settings. A client's local `balance.cfg` and `trading.yml` cannot override active server values. Invalid edits are rejected and the last accepted settings remain in use; see the BepInEx log for details.

### `general.cfg`

For example, edit the existing generated entries:

```ini
[Configuration]
Config check interval (seconds) = 2

[Trading UI]
Enable Haldor tabs = true
```

Set `Enable Haldor tabs = false` to use Haldor's original shop interface without changing the items for sale.

### `balance.cfg`

Contains synchronized settings for Hel's Oath, the Berserker's Wrath/Blood Debt/Reckoning/Ragnarök, and other configurable combat effects. For example, Hel's Oath provides **Damage for full charge**, **Explosion radius**, **Embrace coefficient**, **Touch V Embrace coefficient**, and **Touch stack interval**. Ragnarök has configurable thresholds, casting time, Exhaustion, and its ability key.

**Edit the entries already generated by the mod**, rather than copying old configuration sections from 1.2.x. The **Ragnarök key** and other local button bindings are stored with input settings, not in `trading.yml`.

### `trading.yml`

The file is generated with **all recognized items** under `shop:` and `sell:`. The example below is **only an excerpt from a generated file**; do not replace the complete document with it:

```yaml
shop:
  Bone_crushers:
    price: 2500
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  Iron:
    price: 225
    enabled: true
    stack: 30
    requiredGlobalKey: "defeated_gdking"

sell:
  TrophyEikthyr:
    price: 70
  SurtlingCore:
    price: 50
```

In this **example**, Iron is locked behind the Elder; the default value in your generated file may be different. Keep the original entries for all other products and trophies.

- `price`: shop purchase price for the **entire stack**. In `shop:`, `0` means **free**; in `sell:`, `0` **disables selling**.
- `enabled`: `false` hides the item from Haldor's **buy** list.
- `stack`: how many units are sold together. `0` uses the prefab's maximum stack size; other values cannot exceed it.
- `requiredGlobalKey`: required world key, or `""` for no restriction.
- Item identifiers such as `Bone_crushers` are **internal prefab IDs**, not localized display names. Keep them unchanged.
- Preserve `shop:` and `sell:`, all generated item entries, field names, and indentation (**spaces only; no tabs**). This file supports a restricted YAML format, not every YAML feature.

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

These are **not required dependencies**. Compatibility depends on the versions installed; earlier testing is not a guarantee for every 1.3.0 mod combination.

## Building from source

Requirements: Valheim, BepInEx 5, Jotunn, **.NET Framework 4.8**, and Visual Studio or Rider.

The C# project is located at `HaldorExpansion/HaldorExpansion.csproj`. Developer-specific Valheim / r2modman / dedicated-server paths can be supplied through the optional file:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

Copy `HaldorExpansion/HaldorExpansion.Local.props.example`, rename it to `HaldorExpansion.Local.props`, and adjust the paths for your machine. The file is local-only and should **not** be committed to Git.

Available MSBuild path properties include `R2ModmanProfileDir`, `ValheimInstallDir`, `ValheimDedicatedServerDir`, `ClientPluginDeployDir`, and `ServerPluginDeployDir`.

Source PNGs are in `HaldorExpansion/Assets/Icons/` and are embedded in the DLL when built. Players do not need these files.