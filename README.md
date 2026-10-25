# Haldor Expansion - Valheim Trading, Relics & Awakened Abilities

**English** · [Русский](README.ru.md) · [Changelog](CHANGELOG.md)

**Turn trophies into coins. Coins into relics. Relics into legends.**

**Haldor Expansion (HaldorExpansion)** is a **Valheim mod** that expands Haldor's trading and equipment progression. Sell trophies and valuable loot, buy rare materials and unique artifacts, collect idols, and awaken new powers at the **Forge of Possibilities**.

There's more to discover beyond trading: become a Berserker and unleash Ragnarök, wield the devastating Bone Crushers, or send the spectral archers of Hel's Oath into battle.

---

## Why return to Haldor?

- **Equipment and resources:** 13 unique items and **31 types of materials** - from metals and timber to Refined Eitr, Bloodgold, Timberwood, and Ice.
- **Coins for your loot:** Haldor buys creature and boss trophies, **Surtling, Black, Molten, and Frost Cores**, as well as idols.
- **Upgrade your favorite relic:** **18 kinds of idols**, **12 upgradeable artifacts**, and awakened abilities unlocked at a configurable quality level.
- **New combat styles:** **Berserker, Wrath, and Ragnarök**; **Bone Crushers and Facebreaker**; **Hel's Oath, Ghosts of the Last Hunt, and Soul Swap**.
- **Flexible customization:** prices, inventory, world-progression requirements, abilities, and interface settings. English and Russian localization, single-player, and dedicated-server support.

### 🎬 Ragnarök in action

[![Ragnarök Ability Showcase](https://img.youtube.com/vi/U9n6rRKwHu8/hqdefault.jpg)](https://youtu.be/U9n6rRKwHu8)

*One of Haldor Expansion's combat abilities.*

**Requires BepInEx 5 and Jotunn.** In multiplayer, the mod must be installed on the server and on every player's client. The server controls prices and gameplay balance; keybindings and the interface can be customized locally.

[Full changelog](CHANGELOG.md) · [Source code and discussions](https://github.com/aveasura/HaldorExpansion)

---

## Haldor's expanded shop - buy, sell & progress

Haldor becomes **a trader worth visiting after every expedition** - not just for equipment, but also for resources, idols, and a way to turn hard-earned loot into coins. Sell the trophies gathered on your adventures, stock up on useful materials, or invest in idols to upgrade your favorite relics.

**Buy from Haldor:** **13 unique equipment items**, **31 types of resources and materials**, and **18 kinds of idols** (16 standard idols, Haldor's Idol, and Haldor's Adamantine Idol). Item availability, stack sizes, prices, and world-progression requirements are configurable.

**Sell to Haldor:** regular creature and boss trophies from the Meadows through the Deep North; **Surtling Cores, Black Cores, Molten Cores, and Frost Cores (Frostcore)**; and all 18 kinds of idols. Trophy prices depend on the item, with a configurable fallback price for other supported trophies.

The shop has **six tabs: All / Equipment / Wood / Ores and Metals / Resources / Other**. Idols currently appear under **Other**; there is no dedicated idol tab yet. Tabs can be customized or disabled to restore Valheim's familiar shop layout. Hiding a tab does not automatically remove its items from **All** - use `Hide items from disabled tabs` for that.

### What can you buy? - resources & materials

The catalogue includes metals, stone, timber, textiles, and rare magical crafting ingredients. Later-game resources include **Bloodgold, Timberwood, Ice, Black Metal, Linen Thread, Sap, Refined Eitr, and Soft Tissue**.

<details>
<summary><strong>Show all 31 resources and default buy prices</strong></summary>

**These are prices paid BY THE PLAYER to Haldor**, not what Haldor pays when buying items from you. Prices are for the **entire stack**, not necessarily a single item. *Full stack* means the item's maximum in-game stack size. Player and server configurations may use different prices.

| Resources available to buy | Stack size | Coins per stack |
| --- | ---: | ---: |
| Copper / Tin / Iron / Silver / Flametal | 30 each | 150 / 150 / 250 / 450 / 1000 |
| Black Metal / Bloodgold | Full stack | 200 / 1500 |
| Stone / Grausten / Black Marble | Full stack | 25 / 50 / 50 |
| Wood / Core Wood / Ancient Bark / Fine Wood | Full stack | 25 / 50 / 100 / 200 |
| Yggdrasil Wood / Ashwood / Timberwood | Full stack | 300 / 100 / 100 |
| Red Jute / Blue Jute / Linen Thread | Full stack | 300 / 300 / 800 |
| Sap / Refined Eitr / Soft Tissue | Full stack | 400 / 900 / 400 |
| Resin / Greydwarf Eye / Crystal / Obsidian / Needle / Tar | Full stack | 100 / 100 / 250 / 250 / 250 / 250 |
| Feathers | Full stack (100) | 150 |
| Ice | Full stack | 400 |

</details>

### What can you sell? - trophies, cores & idols

Haldor buys **creature and boss trophies**, including those from the Deep North. Examples of default prices: **Eikthyr Trophy - 350 coins**, **Yagluth Trophy - 1500**, and **Fader Trophy - 5000**. Other trophies have their own prices; trophies without a specific entry use a configurable fallback (**10 coins** by default).

| Rare material sold to Haldor | Default coins per item |
| --- | ---: |
| Surtling Core | 70 |
| Black Core | 200 |
| Molten Core | 350 |
| Frost Core | 550 |

### Idol trading - buy, sell & upgrade

Haldor **buys and sells all 18 kinds of idols**. Every price below is **per single idol**, not per stack.

| Idol | Buy from Haldor | Sell to Haldor |
| --- | ---: | ---: |
| Any of the **16 standard idols** | **1000** | **300** |
| **Haldor's Idol** | **1000** | **300** |
| **Haldor's Adamantine Idol** | **4000** | **2500** |

Each idol's buy and sell prices, availability, and world-progression requirements can be configured independently. The upgrade mechanics are explained below.

---

## Forge of Possibilities & Awakened Potential

**12 of the 13 unique equipment items** can be upgraded at the Forge of Possibilities. The exception is the **Thread of the Norns**.

| Upgrade material | On success | On failure |
| --- | --- | --- |
| **Haldor's Idol** | Quality increases by **1** | The item **may be destroyed** |
| **Haldor's Adamantine Idol** | Quality increases by **1** | Quality decreases by **1**, but the item **cannot be destroyed** |

**Both idols have the same chance of success.** Adamantine failures never reduce an item's quality below **1**. Each attempt consumes one idol, even if the attempt fails at quality 1.

**The awakening threshold is configurable; the default is quality 10.** The `Quality threshold` setting under `[Awakening / Пробуждение]` in `balance.cfg` sets a shared threshold for all artifacts (**1–1000**). In multiplayer, the server's value applies. Changes also affect existing and equipped items; tooltips and upgrade previews reflect the current requirements. Falling below the configured quality disables awakening until the item qualifies again. This is **not an upgrade cap**: items can be upgraded beyond the threshold. Awakening belongs to **each individual item instance**, not to every copy of the artifact.

<details>
<summary>Show awakened abilities (quality 10+ by default; spoilers)</summary>

| Artifact | Awakened ability |
| --- | --- |
| **Berserker Greataxe** | **Bloody Harvest:** Greataxe kills grant **30 Blood Debt**. |
| **Berserker Helmet** | **Feast of the Fallen:** Greataxe kills during Wrath restore **10% of missing health**. |
| **Berserker Cloak** | Ragnarök **preserves Wrath**. Blood Debt is still consumed, and Exhaustion still applies. |
| **Bone Crushers** | Normal attacks heal for **3% of the HP actually lost by the enemy**. |
| **Pit King's Mantle** | With Bone Crushers, raises the Steel Heart barrier bonus to **+100%** and increases Retribution damage accordingly. |
| **Hel's Oath** | **Ghosts of the Last Hunt:** up to three spectral archers; **Soul Swap** sacrifices a spirit and stamina to teleport. |
| **Pelt of Helheim** | **+80 maximum stamina** and **80% less fall damage**. |
| **Cuirass of Silent Reckoning** | While delayed-damage debt is active and HP is **at or below 25%**, restores **2% of maximum HP per second**. |
| **Cloak of the Wounded Beast** | **+30 maximum HP** and **+30 effective Unarmed skill** (capped at 100). |
| **Cloak of Burned Resolve** | **25% reduction to slash damage** at the resistance stage. |
| **Corrupted Brisingamen** | **Blessing of the Rejected Goddess:** **+40% stamina regeneration**. |
| **Crossbow of the Shadow Hunt** | **+60 effective Sneak** and **+30 effective Crossbows** (each capped at 100). |

</details>

Most awakened effects are passive or enhance existing abilities. **Hel's Oath** also unlocks **Soul Swap** (**E** by default; rebindable). The awakened effect appears in the item's tooltip once it reaches the required quality. **All 12 upgradeable artifacts have awakenings**, and upgrading beyond the threshold remains possible.

### Where to find Haldor's idols

Both special idols are sold by Haldor. The regular **Haldor's Idol** can also drop alongside standard idols without replacing their loot. **Haldor's Adamantine Idol** has an independent **1% chance per eligible chest** by default, configurable in `balance.cfg`. Buy and sell prices are listed in the trading section above.

---

## Combat mechanics & playstyles

Every relic has its own character: some reward aggression, while others help you survive a lethal blow or reposition in battle.

### Berserker - Wrath, Blood Debt & Ragnarök

The **Berserker Greataxe, Helmet, and Cloak** complement one another without requiring a particular chest armor or leggings. This is a playstyle for warriors willing to risk their health in exchange for growing power.

- **Bloodthirst:** the Greataxe becomes more dangerous as health is lost.
- **Wrath I–V:** consecutive qualifying hits build up to five stages, increasing attack speed and reducing stamina costs.
- **Blood Debt:** taking damage and landing Greataxe hits fuel a special combat resource displayed on its own HUD.
- **Reckoning:** at **Wrath II+**, with enough Blood Debt, use the secondary attack to spend that resource, deal additional damage, and recover health on a successful hit.
- **Ragnarök:** wield the Greataxe, equip the Berserker Cloak, reach **Wrath V**, and build enough Blood Debt. Press **E** (default) to summon **12–30 meteors**. The sky darkens, the battlefield erupts, and **Exhaustion** follows.

The Helmet improves defense and healing at different Wrath stages. The Cloak accelerates Blood Debt generation but increases incoming damage. Ability thresholds, bonuses, Exhaustion penalties, and keybindings are configurable.

### Bone Crushers - Retribution, Facebreaker & Steel Heart

**Bone Crushers** are fast cestus weapons that turn lost health into **Battle Fervor**. At full charge, press the **middle mouse button (Mouse2)** by default: **Retribution** releases a shockwave and creates a temporary protective barrier.

**Facebreaker** provides crowd control: **hold E** (default) to aim, then release it to pull enemies toward the targeted point. Targets are slowed for **4 seconds**, while enemies caught on **both sides** are also staggered. The ability has its own animation, impact sounds, and a **5-second default cooldown**. Its key can be changed, and previously saved bindings remain intact. If E is also bound to an interaction, opening doors or chests and picking up items take priority.

Equipping the **Pit King's Mantle** alongside Bone Crushers unlocks **Steel Heart**, strengthening the protective barrier and Retribution's shockwave.

The Bone Crushers shield indicator is customizable in `general.cfg`: move it, scale it, separate it from the health bar, or hide its display without disabling the actual shield.

### Hel's Oath - Hel's Embrace, ghosts & Soul Swap

**Hel's Oath** is a high-risk bow that trades **50% of maximum health for additional maximum stamina**.

With the **full Fenris set**, successful hits build **Hel's Touch** through five stages, increasing movement speed and reducing stamina costs. Normal arrows charge **Hel's Embrace**: when ready, the next arrow triggers a powerful **Lightning and Frost explosion**. At **Hel's Touch V**, the explosion is empowered.

**Ghosts of the Last Hunt (awakened):** successful normal hits summon up to **three spectral archers**. They echo their master's shots for **33% damage** and can fire through obstacles. The first apparition is a distant **Guardian**, useful as a tactical teleport destination. During Hel's Embrace, remaining ghosts fire empowered arrows dealing **50% of the ability's damage** before disappearing. Ghost attacks do not charge other abilities.

**Soul Swap:** press **E** (default) to teleport to a materialized ghost, sacrificing **one soul and 40% of maximum stamina**. There is **no cooldown**, and the swap works even while jumping or rolling. Without new hits, ghosts begin to fade after **30 seconds**, losing one soul every **10 seconds**.

The **Pelt of Helheim** completes the combination: when the Hel's Oath, Fenris, and Hel's Touch synergy requirements are met, it grants **Very Resistant to Pierce**.

### Other relics - survival & risk

- **Thread of the Norns:** a one-use amulet that sacrifices itself to save its owner from a lethal hit.
- **Cuirass of Silent Reckoning:** delays a portion of dangerous incoming damage, giving you time to recover without erasing the consequences.
- **Cloak of the Wounded Beast:** improves healing at low health at the cost of increased incoming damage.
- **Cloak of Burned Resolve:** redirects a portion of incoming damage to stamina; running out of stamina is dangerous.
- **Corrupted Brisingamen:** increases carrying capacity.
- **Crossbow of the Shadow Hunt:** devastating opening shots and ambush attacks in exchange for mobility.

---

## All 13 unique equipment items

The **default buy prices** are shown below. They can be changed in `trading.yml`.

| Item | Coins | Playstyle |
| --- | ---: | --- |
| **Berserker Greataxe** | 2500 | Missing-health damage, Wrath, Blood Debt, and Reckoning |
| **Berserker Helmet** | 2500 | Wrath-based defense and healing |
| **Berserker Cloak** | 2500 | Faster Blood Debt generation; enables Ragnarök at a cost to survivability |
| **Bone Crushers** | 2500 | Cestus weapons that turn damage suffered into a barrier and shockwave |
| **Pit King's Mantle** | 3500 | Bonus health and Steel Heart synergy with Bone Crushers |
| **Hel's Oath** | 2500 | Health-for-stamina bow, Hel's Touch and Embrace, awakened **Ghosts of the Last Hunt**, and **Soul Swap** |
| **Pelt of Helheim** | 2500 | Very high Pierce resistance with the full Hel/Fenris synergy |
| **Cuirass of Silent Reckoning** | 3500 | Delays a portion of dangerous damage without negating it |
| **Thread of the Norns** | 6666 | One-use protection from a lethal hit |
| **Cloak of the Wounded Beast** | 2500 | Increased low-health healing at the cost of taking more damage |
| **Cloak of Burned Resolve** | 2500 | Redirects some incoming damage to stamina |
| **Corrupted Brisingamen** | 1600 | Increased carrying capacity |
| **Crossbow of the Shadow Hunt** | 2500 | Powerful opening shots with reduced mobility |

---

### Relic legends & readable descriptions

**12 equipment artifacts and both Haldor idols** have original, interconnected lore. Shop previews focus on each relic's story, while inventory tooltips also explain its gameplay mechanics. The Crossbow of the Shadow Hunt retains a conventional description until a future rework.

Long descriptions can be scrolled with the **mouse wheel** or **Page Up / Page Down**. This helps on smaller screens; the feature can be disabled in `general.cfg` for compatibility with other UI mods.

---
## Configuration - examples for players & server owners

On first launch, the mod creates three configuration files in:

```text
BepInEx/config/aveasura-HaldorExpansion/
├── general.cfg   # Local preferences and shop interface
├── balance.cfg   # Equipment and ability balance
└── trading.yml   # Inventory, prices, world progression and sell values
```

### Apply changes without restarting

The mod automatically checks these files during gameplay. By default, **checks run every 3 seconds**, with a minimum interval of **0.5 seconds**. File metadata is checked before reading the full contents, and changes are applied only after a second stability check.

To change gameplay settings on a dedicated server, edit the appropriate files **on the server**. A client's local `balance.cfg` and `trading.yml` cannot override active server settings. If a config is damaged or contains invalid values, the last accepted settings remain in effect. Details are written to the BepInEx log.

### `general.cfg`

An example using **actual setting names** (edit the values in your generated file as needed):

```ini
[Configuration]
Config check interval (seconds) = 3

[Trading UI]
Enable Haldor tabs = true
Hide items from disabled tabs = false
```

To restore Haldor's standard interface, set `Enable Haldor tabs = false`. To remove items from a hidden tab in **All**, enable `Hide items from disabled tabs`. If you also hide **Other**, idols will disappear from the shop.

**Ability keybindings:** in newly generated configs, Facebreaker and Soul Swap default to **E**, while Hel's Embrace defaults to **Mouse2**. Existing custom bindings are preserved. When an ability shares a key with an interaction, opening chests or doors, picking up items, and other available interactions take priority.

**Optional F1 settings:** install **BepInEx ConfigurationManager** to edit supported settings in-game. In F1, **Trading UI** appears immediately after **Compatibility**. You can configure idol buy/sell prices, individual item availability, shop tabs, and the Blood Debt HUD. ConfigurationManager is optional; the mod works without it.

### `balance.cfg` - awakening and idol drop examples

This file controls combat balance, abilities, and the Forge of Possibilities. For example, these **existing settings** control the awakening quality threshold and Adamantine Idol chest drop chance:

```ini
[Awakening / Пробуждение]
Quality threshold = 10

[Forge of Possibilities]
Adamantine Idol chest drop chance = 1
```

- `Quality threshold = 10` - awakens abilities at **quality 10** (allowed values: **1 to 1000**). This does not impose a maximum upgrade level.
- `Adamantine Idol chest drop chance = 1` - sets the chance of finding an Adamantine Idol in an eligible chest to **1%**. A value of `0` disables these drops; `100` makes the chance 100%.

This file also contains settings for **Hel's Oath**, **Wrath**, **Blood Debt**, **Reckoning**, **Ragnarök**, defensive mechanics, and other effects. For instance, Hel's Oath has `Damage for full charge`, `Explosion radius`, `Embrace coefficient`, `Touch V Embrace coefficient`, and `Touch stack interval`.

**Important:** keep the section and setting names found in the `balance.cfg` generated by your installed mod version. Do not guess or reuse outdated names from 1.2.x. The server synchronizes combat balance in multiplayer; local keybindings are configured separately.

### `trading.yml`

The mod generates a file containing the **complete catalogue of known items** under `shop:` (what players buy from Haldor) and `sell:` (what Haldor buys from players). Below is an **example of editing existing entries**, **not a complete replacement file**. **Do not replace your entire `trading.yml` with this snippet**:

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
    price: 350
  SurtlingCore:
    price: 70
  aveasura_he_idol:
    price: 300
  aveasura_he_adamantine_idol:
    price: 2500
```

In this example, Iron is sold in **stacks of 30 for 250 coins**, and it becomes available after defeating **The Elder** (`defeated_gdking`). To make an item available immediately, set `requiredGlobalKey: ""`. This is a **custom configuration example**, not a guarantee that every player has those exact values. Keep all other entries in the file.

- `price`: the buy price **for the entire stack** (**for one idol** in the case of idols). Under `shop:`, a value of `0` makes the item **free**. Under `sell:`, a value of `0` **disables selling** that item.
- `enabled`: `false` removes an item from Haldor's **buyable inventory**.
- `stack`: number of items per purchase. `0` means the item's maximum stack size. Other values cannot exceed that maximum. **All idols are forced to `stack: 1`, even if a different value is entered.**
- `requiredGlobalKey`: the required global world key, or `""` if the item should be available immediately.
- Identifiers such as `Bone_crushers` are **internal prefab IDs**, not localized item names. Do not rename them.
- Keep the `shop:` and `sell:` sections, all entries and field names, and the correct indentation (**spaces only, no tabs**). Only a limited YAML subset is supported, not every YAML feature.

### `custom-trading.yml` (optional, user-defined items)

This separate file is generated on first launch with an English quick-start guide, examples and a list of common progression keys. It is for **extra items**, including items from other mods. It does not replace `trading.yml`.

- `haldorSells` lets you add new products. `price` is the price of one purchase and `amount` is the number of items received, capped at the prefab's stack limit.
- `haldorBuys` lets Haldor purchase configured items through his **original sell coin button**. `price` is paid for **one item**. The button sells the current inventory stack, even if it contains only 18 of 50 berries.
- New modded idols can use `category: Idols` to appear in the existing Idols filter.
- `Enable Custom Trading` in the optional BepInEx Configuration Manager (F1) disables or enables both sections without deleting their contents. It does not change vanilla or built-in trade prices.
- The maximum YAML size is **1 MiB**. Host or dedicated server values take precedence in multiplayer and can update live.

See the comments inside the generated file for the full format. Note that the regular `trading.yml` still uses `stack`, while custom offers use `amount`.

### Boss progression keys - examples

Set `requiredGlobalKey` to **one of the world's global keys**. The item becomes available after the corresponding boss is defeated, provided that your world and Valheim version support the key.

| `requiredGlobalKey` value | Boss defeated |
| --- | --- |
| `"defeated_eikthyr"` | Eikthyr |
| `"defeated_gdking"` | The Elder |
| `"defeated_bonemass"` | Bonemass |
| `"defeated_dragon"` | Moder |
| `"defeated_goblinking"` | Yagluth |
| `"defeated_queen"` | The Queen |
| `"defeated_fader"` | Fader |
| `"defeated_frozenking"` | Kell Fimbulbringer / Deep North progression |

For example, to unlock an item after defeating **Bonemass**:

```yaml
requiredGlobalKey: "defeated_bonemass"
```

To remove the progression requirement, use an empty string: `requiredGlobalKey: ""`.

Not every world or Valheim version necessarily supports all these keys - particularly the Deep North progression key.

### Upgrading from 1.2.x

When upgrading from 1.2.x, the mod attempts to migrate existing settings into the new files. The previous configuration is **backed up**, and updates may add new settings while preserving customized values. Backups and configuration-format version information are stored in a separate folder under the HaldorExpansion configuration directory.

Do not delete individual entries from already migrated configuration files: the mod validates the expected file structure. If an error occurs, check the BepInEx log and restore the last working backup. Before making extensive manual changes, creating your own config backup is recommended.

---

## Installation

Install **BepInEx 5** and **Jotunn** (compatible mod managers may install these dependencies automatically), then place the compiled mod at:

```text
BepInEx/plugins/aveasura-HaldorExpansion/HaldorExpansion.dll
```

There is no need to copy separate PNG files: icons are embedded in the DLL. Players **do not need** to edit the `.csproj` or create `HaldorExpansion.Local.props` - these are only relevant when building from source.

### Multiplayer / dedicated server

Install the same HaldorExpansion version **on the dedicated server and on every player's client**. The mod uses `NetworkCompatibility(EveryoneMustHaveMod, Minor)`.

The server controls synchronized combat balance, prices, shop stock, and world-progression requirements. Connected players use the server's values, but their local configuration files are not permanently overwritten. Shop-tab visibility and local keybindings can be configured individually.

---

## Compatibility & optional mods

Required dependencies: **BepInEx 5** and **Jotunn**. HaldorExpansion has previously been used in single-player and on dedicated servers alongside other mods, including:

- `Smoothbrain-CreatureLevelAndLootControl`
- `warpalicious-Monster_Modifiers`
- `ASharpPen-Custom_Raids`
- `Azumatt-AzuExtendedPlayerInventory`
- `Azumatt-AzuSkillTweaks`
- `rendl0449-CraftFromContainers`
- `Advize-PlantEverything`
- `Marf-FuelEternal`
- `...`

These mods are **not required**. Compatibility depends on the installed versions: previous tests do not guarantee that every mod combination will work without issues in 1.3.3.

---

## Building from source

Development requires Valheim, BepInEx 5, Jotunn, **.NET Framework 4.8**, and Visual Studio or Rider.

C# project file: `HaldorExpansion/HaldorExpansion.csproj`. Local paths to Valheim, r2modman, and the dedicated server can be set in the optional file:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

Copy `HaldorExpansion/HaldorExpansion.Local.props.example`, rename the copy to `HaldorExpansion.Local.props`, and set the paths for your machine. This is a local file and **should not be committed to Git**.

Supported MSBuild path properties: `R2ModmanProfileDir`, `ValheimInstallDir`, `ValheimDedicatedServerDir`, `ClientPluginDeployDir`, `ServerPluginDeployDir`.

The source PNG files are in `HaldorExpansion/Assets/Icons/` and are embedded in the DLL at build time. Players do not need them.

## Feedback

Questions, found a bug, or want to share your thoughts?

You can contact me on Discord: **@aveasura**

Bug reports, suggestions, and feedback are always welcome!

---

## ❤️ Support Development

HaldorExpansion is free and will always remain free.

If you enjoy the mod and would like to support its continued development, you can make a one-time donation or become a supporter on [Boosty](https://boosty.to/aveasura).

Support is entirely optional, but I'm sincerely grateful to everyone who chooses to help. Thank you! 🍻
