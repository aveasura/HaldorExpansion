# Haldor Expansion - Unique Equipment, Combat Abilities & Trading

**English** · [Русский](README.ru.md) · [Changelog](CHANGELOG.md)

**Turn trophies into coins. Coins into relics. Relics into legends.**

**Haldor Expansion (HaldorExpansion)** is a Valheim mod that adds unique weapons, armor, cloaks, and accessories with powerful combat abilities, alongside an expanded and highly customizable trading system. Discover new fighting styles, upgrade your equipment, awaken hidden powers, and build your own economy with items from other mods.

This GitHub README is the **complete player and server-owner guide**. For a short overview, visit [Thunderstore](https://thunderstore.io/c/valheim/p/aveasura/HaldorExpansion/), [Nexus Mods](https://www.nexusmods.com/valheim/mods/4075), or [Hexium](https://valheim.hexium.gg/mods/aveasura/HaldorExpansion).

## Contents

- [Quick start](#quick-start)
- [Haldor's expanded shop](#haldors-expanded-shop)
- [Equipment upgrading and awakening](#equipment-upgrading-and-awakening)
- [Combat mechanics and playstyles](#combat-mechanics-and-playstyles)
- [All 13 unique equipment items](#all-13-unique-equipment-items)
- [Configuration guide](#configuration-guide)
- [Custom trading: step by step](#custom-trading-step-by-step)
- [Multiplayer and dedicated servers](#multiplayer-and-dedicated-servers)
- [Installation](#installation)
- [Troubleshooting](#troubleshooting)

---

## Quick start

1. **Install Haldor Expansion** alongside **BepInEx 5** and **Jotunn**. Use your mod manager, or follow the [manual installation](#installation) instructions below.
2. **Launch the game once** to create the configuration files under `BepInEx/config/aveasura-HaldorExpansion/`.
3. **Find Haldor** and open his shop. Browse the new goods in **All**, or use the category tabs for equipment, resources and idols.
4. **Try the included content.** Buy one of the unique items, sell trophies or other supported loot for coins, and obtain idols to upgrade equipment at a compatible crafting station.
5. **Customize the economy if you like.** Use `trading.yml` to change built-in offers or `custom-trading.yml` to add new items from Valheim and other mods. Save the file; the mod applies valid changes while the game is running.
6. **Optional:** install BepInEx Configuration Manager and press **F1** (its default hotkey) to adjust supported in-game settings. The manager is **not** required for the mod to work.

**Multiplayer note:** install the mod on both the server and all clients. Server gameplay and trading rules take priority over a connected client's local files.

### 🎬 Ragnarök in action

[![Ragnarök Ability Showcase](https://img.youtube.com/vi/U9n6rRKwHu8/hqdefault.jpg)](https://youtu.be/U9n6rRKwHu8)

---

## Haldor's expanded shop

Haldor becomes useful beyond his original stock: buy rare materials and unique equipment, sell expedition loot, and stock up on idols for equipment upgrades. The mod includes the following offers **by default**; their availability and prices can be customized.

- **13 unique equipment items**, including weapons, armor, cloaks and accessories.
- **31 resource/material types**, including metals, timber, magical ingredients and Deep North resources.
- **18 upgrade idol types**: 16 standard idols and two Haldor idols.
- Haldor buys numerous creature and boss trophies, rare cores and idols.
- Additional custom buy/sell offers may be created using items from other loaded mods.

### Shop categories and item visibility

The Haldor shop has seven categories: **All / Equipment / Wood / Ores and Metals / Resources / Other / Idols**. The **Idols** tab contains the 18 built-in idol types; custom items can also be assigned to it.

**Hiding a tab and hiding its items are different actions:**

- `Show Idols tab = false` hides the Idols tab, but idol products remain accessible under **All** by default.
- `Hide items from disabled tabs = true` also removes offers belonging to disabled categories, **including Haldor's original Valheim items**.
- This filtering still works when `Enable Haldor tabs = false` (the original-style UI). If all categories are disabled while filtering is enabled, the shop is empty.
- Re-enabling a category only restores offers that are still allowed by their other settings and progression requirements.

Each player's tab visibility is a **personal setting**. It does not change the server's trading rules for everyone else.

### Which offers can I customize?

There are two distinct systems:

| What you want to do | File | How it works |
| --- | --- | --- |
| Change the price, bundle size, progression or availability of an included item | `trading.yml` | Edit its existing `shop:` entry |
| Change how many coins Haldor pays for a built-in trophy, idol or core | `trading.yml` | Edit its existing `sell:` entry |
| Add a new product to Haldor's shop | `custom-trading.yml` | Add a prefab under `haldorSells:` |
| Let players sell another item to Haldor | `custom-trading.yml` | Add a prefab under `haldorBuys:` |
| Disable or remove a custom offer | `custom-trading.yml` | Set `enabled: false` or remove the custom block |
| Hide whole shop categories, including original Haldor goods | `general.cfg` / optional F1 | Use shop-tab visibility and filtering settings |

**Limits:** `trading.yml` is a list of known built-in items, not a place to add arbitrary prefab IDs. Custom shop offers do **not** replace an existing item listing already supplied by Haldor or another mod; duplicates are skipped and reported in the BepInEx log.

### What can you buy? - resources and materials

The default catalog includes metals, stone, wood, textiles and rare magical crafting ingredients. **Prices below are paid by the player for one purchase**, often for a whole stack. *Full stack* means the item's maximum stack size.

<details>
<summary><strong>Show all 31 resource types and default prices</strong></summary>

| Resources available to buy | Amount per purchase | Coins per purchase |
| --- | --- | ---: |
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

Actual prices may differ if your world or server has custom settings.

</details>

### What can you sell? - trophies, cores and idols

Use Haldor's **usual sell coin button**. The included sell catalog supports numerous creature and boss trophies (including Deep North trophies), four rare core types and idols. Default examples include **Eikthyr Trophy - 350 coins**, **Yagluth Trophy - 1500 coins**, and **Fader Trophy - 5000 coins**. The fallback price for other supported trophies is configurable (**10 coins** by default).

| Core sold to Haldor | Default coins per item |
| --- | ---: |
| Surtling Core | 70 |
| Black Core | 200 |
| Molten Core | 350 |
| Frost Core (`Frostcore`) | 550 |

To let Haldor buy **more kinds of loot**, even items added by another mod, see [`custom-trading.yml`](#custom-trading-step-by-step).

### Idol trading

Haldor buys and sells the following idol groups. These are **default prices for one idol**, not prices for an entire inventory stack.

| Idol | Buy from Haldor | Sell to Haldor |
| --- | ---: | ---: |
| Any of the **16 standard idols** | 700 | 400 |
| **Haldor's Idol** | 700 | 400 |
| **Haldor's Adamantine Idol** | 3000 | 2500 |

You can configure price, availability and world-progression requirements for built-in idol offers. Not every idol is compatible with every piece of equipment.

---

## Equipment upgrading and awakening

Equipment upgrades and awakened artifact powers are **related but different** systems. You can improve compatible equipment with idols, while **12 unique HaldorExpansion artifacts** have additional powers that awaken at a configurable quality threshold.

### How to upgrade an item

1. Get the **actual equipment** you want to improve and an appropriate upgrade idol.
2. Visit a **crafting station with an upgrader** and select a recipe compatible with that item.
3. Choose the regular idol-based upgrade recipe or, if offered, its **Haldor's Adamantine Idol** alternative.
4. Perform the upgrade and inspect the resulting quality. Every attempt can succeed or fail, and the outcome depends on which idol is used.

Haldor Expansion provides dedicated upgrade recipes for **12 of its 13 unique equipment items** using **Haldor's Idol**. The one-use **Thread of the Norns** is the exception. The Adamantine alternative can also work with **other equipment that already has an eligible idol-based upgrade recipe**. It does **not** automatically add upgrade recipes for every item in Valheim or every other mod.

### Haldor's Idol vs. Adamantine Idol

| Idol | Successful attempt | Failed attempt |
| --- | --- | --- |
| **Haldor's Idol** | Quality **+1** | Equipment **may be destroyed** |
| **Haldor's Adamantine Idol** | Quality **+1** | Equipment is **never destroyed**; quality **−1** (minimum **1**) |

The two Haldor idols use the same underlying **upgrade success chance**. A failed Adamantine attempt at quality 1 leaves quality at 1, but the idol is still consumed. Each attempt consumes **one idol**, including unsuccessful attempts.

The **16 standard idols** remain separate upgrade materials, with compatibility determined by their existing equipment recipes. The Adamantine alternative only appears where an eligible underlying recipe exists.

### Awakening 12 unique artifacts

Every **upgradeable** HaldorExpansion artifact has an awakened effect. The default awakening threshold is **quality 10** and can be set to **1–1000** in `balance.cfg` (`[Awakening / Пробуждение]` → `Quality threshold`).

- Awakening belongs to each **individual item instance**, not to every copy of that artifact.
- The item must meet the quality threshold. If its quality drops below it, awakening becomes inactive until it qualifies again.
- Quality 10 is **not a maximum upgrade level**. Upgrading beyond the threshold is allowed.
- Existing and already-equipped artifacts respond to changes in the server's current threshold.
- Some awakened effects additionally depend on an item, ability, set synergy or gameplay condition. Reaching the quality threshold does not eliminate those requirements.

<details>
<summary><strong>Show all awakened artifact abilities (spoilers)</strong></summary>

| Artifact | Awakened effect |
| --- | --- |
| **Berserker Greataxe** | **Bloody Harvest:** greataxe kills grant **30 Blood Debt** |
| **Berserker Helmet** | **Feast of the Fallen:** greataxe kills during Wrath restore **10% of missing health** |
| **Berserker Cloak** | Ragnarök preserves Wrath (Blood Debt is still consumed and Exhaustion still applies) |
| **Bone Crushers** | Normal attacks restore health equal to **3% of HP actually lost by the target** |
| **Pit King's Mantle** | With Bone Crushers, **Steel Heart** barrier bonus increases to **+100%**, with stronger Retribution damage |
| **Hel's Oath** | **Ghosts of the Last Hunt** (up to three spectral archers) and **Soul Swap** teleportation |
| **Pelt of Helheim** | **+80 max stamina** and **80% reduced fall damage** |
| **Cuirass of Silent Reckoning** | With delayed-damage debt active and HP at or below **25%**, restores **2% max HP/s** |
| **Cloak of the Wounded Beast** | **+30 max HP** and **+30 effective Unarmed** (capped at skill 100) |
| **Cloak of Burned Resolve** | **25% reduction to slash damage** at the resistance stage |
| **Corrupted Brisingamen** | **Blessing of the Rejected Goddess:** **+40% stamina regeneration** |
| **Crossbow of the Shadow Hunt** | **+60 effective Sneak** and **+30 effective Crossbows** (each capped at skill 100) |

</details>

### How to obtain Haldor's special idols

Both Haldor idols can be bought from (or sold to) Haldor. The regular idol also drops alongside standard idols in eligible loot without replacing their drops.

**Haldor's Adamantine Idol** has a **base 1% chance per eligible chest** by default, but the separate setting **`Randomize default Adamantine Idol chest chance = true`** is enabled by default: while the base value is 1, each eligible chest receives an independent randomized chance in the **1–2%** range. Disable the randomization for a **fixed 1%**; other configured base chances remain fixed. See [balance configuration](#balancecfg).

---

## Combat mechanics and playstyles

HaldorExpansion equipment offers different fighting styles and combinations. Weapon abilities are not simply stat boosts; many introduce resources, animations, cooldowns and special effects.

### Berserker - Wrath, Blood Debt and Ragnarök

Combine the **Berserker Greataxe**, **Berserker Helmet**, and **Berserker Cloak**. A specific chest armor or leggings is not required.

- **Bloodthirst:** greataxe damage grows more dangerous as your health decreases.
- **Wrath I–V:** qualifying consecutive strikes build Wrath, changing attack speed and stamina costs.
- **Blood Debt:** taking damage and landing hits with the greataxe generate a special combat resource, shown on its own HUD.
- **Reckoning:** at **Wrath II+** with enough Blood Debt, use the greataxe secondary attack to spend the resource, deal bonus damage and recover health on hit.
- **Ragnarök:** equip the greataxe and Berserker Cloak, reach **Wrath V**, gather enough Blood Debt and press **E** (default). The ability summons **12–30 meteors**, darkens the sky and is followed by **Exhaustion**.

The Helmet and Cloak modify healing, defense, Blood Debt generation and damage risk. The awakened Cloak prevents Ragnarök from removing Wrath, **not** from consuming Blood Debt or applying Exhaustion. Exact ability parameters and inputs are configurable.

### Bone Crushers - Battle Fervor, Retribution and Facebreaker

**Bone Crushers** are close-range cestus weapons that convert damage taken into **Battle Fervor**.

- When the resource is full, press the **middle mouse button (Mouse2)** by default to release **Retribution**: a shockwave and a temporary protective barrier.
- **Facebreaker:** **hold E** (default) to aim, then release to pull enemies toward the selected point. Targets are slowed for **4 seconds**, while affected enemies on both sides are staggered. Default cooldown: **5 seconds**.
- Wearing **Pit King's Mantle** with Bone Crushers activates **Steel Heart**, improving the barrier and shockwave.
- The shield indicator can be moved, scaled, separated from the health bar or hidden in `general.cfg` without disabling the actual shield.

If an ability shares a key with an interaction, interacting with doors, containers or pickups takes priority. Previously customized keybinds are preserved on upgrade.

### Hel's Oath - Hel's Touch, spectral archers and Soul Swap

**Hel's Oath** is a high-risk bow: it sacrifices **50% maximum health** in exchange for extra maximum stamina.

- With the **full Fenris set**, normal hits build **Hel's Touch I–V**, improving movement and reducing stamina costs.
- Normal arrows charge **Hel's Embrace**. When charged, the next arrow produces a powerful **Lightning and Frost** explosion, stronger at Touch V.
- Once the bow is awakened, **Ghosts of the Last Hunt** summons up to **three spectral archers** from successful normal hits. Their mirrored shots deal **33% damage** and pass through obstacles.
- The first apparition acts as a distant **Guardian**, providing a positioning option for **Soul Swap**.
- During an Embrace attack, remaining ghosts release empowered arrows for **50% of the ability's damage** before disappearing; ghost shots do not charge other abilities.
- **Soul Swap:** press **E** (default) to teleport to a materialized ghost. It consumes **one soul and 40% of maximum stamina** without a cooldown; it can be used while jumping or rolling.
- Without additional qualifying hits, ghosts start fading after **30 seconds**, losing one soul every **10 seconds**.

The **Pelt of Helheim** works with the bow/Fenris/Touch setup, granting **Very Resistant to Pierce** when its synergy requirements are met.

### Other relics - survival, defense and ambushes

- **Thread of the Norns:** a **single-use** amulet that is consumed to save you from a lethal blow.
- **Cuirass of Silent Reckoning:** delays part of dangerous incoming damage so you can react and recover.
- **Cloak of the Wounded Beast:** improves healing at low health, at the cost of taking more damage.
- **Cloak of Burned Resolve:** redirects part of incoming damage toward stamina; running out of stamina is dangerous.
- **Corrupted Brisingamen:** increases carry capacity.
- **Crossbow of the Shadow Hunt:** excels at powerful opening shots and ambushes, but sacrifices mobility.

---

## All 13 unique equipment items

These are the **included default shop prices**. Change them in `trading.yml`; server settings may differ.

| Item | Default coins | Primary playstyle |
| --- | ---: | --- |
| **Berserker Greataxe** | 2500 | Missing-health damage, Wrath, Blood Debt and Reckoning |
| **Berserker Helmet** | 2500 | Wrath-based defense and healing |
| **Berserker Cloak** | 2500 | Generates Blood Debt faster; enables Ragnarök at a survivability cost |
| **Bone Crushers** | 2500 | Cestus attacks, Battle Fervor, barrier and shockwave |
| **Pit King's Mantle** | 3500 | Bonus health and Steel Heart synergy with Bone Crushers |
| **Hel's Oath** | 2500 | Health-for-stamina bow, Hel's Touch, Embrace, ghosts and Soul Swap |
| **Pelt of Helheim** | 2500 | High Pierce resistance through the Hel/Fenris synergy |
| **Cuirass of Silent Reckoning** | 3500 | Delayed damage and survival |
| **Thread of the Norns** | 6666 | One-time rescue from a lethal hit |
| **Cloak of the Wounded Beast** | 2500 | Emergency healing at the cost of vulnerability |
| **Cloak of Burned Resolve** | 2500 | Damage redirected to stamina |
| **Corrupted Brisingamen** | 1600 | Carry capacity |
| **Crossbow of the Shadow Hunt** | 2500 | High-damage ambushes with reduced mobility |

### Relic lore and item descriptions

**12 equipment artifacts and both Haldor idols** include original interconnected lore; the Crossbow of the Shadow Hunt keeps a conventional description until a future rework. Shop previews emphasize the story, while inventory tooltips also explain gameplay mechanics. Long descriptions scroll using the **mouse wheel** or **Page Up / Page Down**; this can be disabled in `general.cfg` for compatibility with UI mods.

---

## Configuration guide

The mod generates **four configuration files** on first launch:

```text
BepInEx/config/aveasura-HaldorExpansion/
├── general.cfg          # Personal preferences, UI and the server-owned Custom Trading switch
├── balance.cfg          # Gameplay, equipment abilities, awakening and drop balance
├── trading.yml          # Known built-in Haldor shop/sell offers and progression
└── custom-trading.yml   # Extra user-defined buy and sell offers
```

| I want to... | Where to edit | Multiplayer ownership |
| --- | --- | --- |
| Change built-in item prices, quantity or availability | `trading.yml` | Server |
| Change Haldor's default trophy, core or idol buyback prices | `trading.yml` | Server |
| Add items from other mods to Haldor's selling or buying rules | `custom-trading.yml` | Server |
| Adjust combat powers, upgrade awakening or idol drops | `balance.cfg` | Server |
| Show/hide shop tabs, change HUD position or rebind abilities | `general.cfg` | Personal |
| Turn all custom offers on/off without deleting the YAML | `general.cfg` → `Enable Custom Trading` | Server |

**Live reload:** the mod checks config files during play, normally every **3 seconds** (minimum **0.5 seconds**), and verifies changed files before applying them. Valid changes take effect without a game restart. Invalid changes retain the last accepted state and are reported in the BepInEx log. For shop content, close/reopen the shop if a change is not visible immediately.

### `general.cfg`

For most users, edit the generated file directly or use the optional **BepInEx Configuration Manager (F1)**. This excerpt shows **actual configuration keys**; it is **not** the entire config:

```ini
[Configuration]
Config check interval (seconds) = 3

[Trading UI]
Enable Haldor tabs = true
Hide items from disabled tabs = false
Show Idols tab = true

[Custom Trading]
Enable Custom Trading = true
```

- **Enable Haldor tabs:** changes the layout only; hiding category UI does not reset the inventory.
- **Show ... tab:** enables or hides a specific category. Items remain in All unless category filtering is on.
- **Hide items from disabled tabs:** removes items in disabled categories from the shop, **including Haldor's original items**. If all categories are disabled, it can empty the shop entirely.
- **Enable Custom Trading:** server-synchronized master switch for the additional buy/sell rules from `custom-trading.yml`. It does not affect existing vanilla or HaldorExpansion offers, nor delete your YAML entries.

**F1 is optional.** It can expose built-in buy/sell prices, combat balance, shop tabs, the Blood Debt HUD and the Custom Trading switch. **Creating the custom offers themselves still requires editing `custom-trading.yml`.** In multiplayer, the server administrator controls synchronized options, while personal UI preferences stay on each client.

New installs use **E** as the default key for Facebreaker and Soul Swap, and **Mouse2** for Hel's Embrace. Existing custom keybinds are preserved. Normal interaction with objects takes priority when keybinds overlap.

### `balance.cfg`

This file adjusts abilities, combat and the Forge of Possibilities. Two examples of **real settings** are:

```ini
[Awakening / Пробуждение]
Quality threshold = 10

[Forge of Possibilities]
Adamantine Idol chest drop chance = 1
Randomize default Adamantine Idol chest chance = true
```

- **Quality threshold:** all 12 custom awakenings activate at this quality or above (default **10**, configurable **1–1000**); this is **not** an upgrade level cap.
- **Adamantine Idol chest drop chance:** base chance per eligible chest, from **0–100%**. Zero disables those drops.
- **Randomize default Adamantine Idol chest chance:** if **true** and the base value is **1**, the chance for each eligible chest is independently **1–2%**. Set **false** for a fixed 1%, or configure a different fixed chance.

Other balance settings control Wrath, Blood Debt, Reckoning, Ragnarök, Hel's Touch/Embrace, defensive effects and other abilities. **Use the exact setting names in the config generated by your installed version**, rather than names from old versions.

### `trading.yml` - modify included offers

`trading.yml` contains the **existing built-in item catalog**. It uses these two root sections:

- `shop:` - what **players buy from Haldor**.
- `sell:` - what **Haldor buys from players**.

For example, this is an **excerpt**, not a replacement file:

```yaml
shop:
  Bone_crushers:
    price: 2500
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  aveasura_he_idol:
    price: 700
    enabled: true
    stack: 1
    requiredGlobalKey: ""
  aveasura_he_adamantine_idol:
    price: 3000
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
```

Here Iron is sold as **30 pieces for 250 coins**, after the world activates the **Elder defeated** key. An empty `requiredGlobalKey: ""` removes the progression requirement.

| Setting | Meaning |
| --- | --- |
| `shop: ... price` | Coins for the **whole purchase**; `0` means free |
| `shop: ... enabled` | `false` prevents that product from being offered |
| `shop: ... stack` | Units in one purchase; `0` uses the prefab's maximum stack size, otherwise capped to maximum |
| `shop: ... requiredGlobalKey` | World key that must be active; `""` means immediately available |
| `sell: ... price` | Coins Haldor pays **per item**; `0` disables the item from being sold to him |

Idols are restricted to **one idol per purchase**, regardless of other stack settings. Built-in prefab names such as `Bone_crushers` are internal IDs; **do not rename them**.

**Important:** only edit the records and values already present. The built-in trading file is schema-validated. **Do not delete existing entries or replace the whole file with the small example above.** Use `custom-trading.yml` for new item IDs.

### Custom trading: step by step

`custom-trading.yml` is a **separate automatically generated file** for new offers. It supports items from Valheim and any loaded mod that registers a valid item prefab. The two main sections are:

- `haldorSells:` - custom items **players buy from Haldor**.
- `haldorBuys:` - custom items **players sell to Haldor**.

The starter examples in the file are **disabled**, so simply creating the file does not add extra trade offers.

#### Example: Add an item to Haldor's shop

1. Open `custom-trading.yml` with a text editor.
2. Under `haldorSells:`, copy an example block, or add a new one with **the exact, case-sensitive prefab ID**. For modded content, find that ID in the source mod's documentation, not just the localized inventory name.
3. Set `enabled: true`, positive `price` and `amount`. Optionally set `requiredGlobalKey` and `category`.
4. Save the file. Reopen Haldor's shop and check the result. The file is monitored in real time; **restarting Valheim is not necessary**.
5. If the item is missing, check the prefab ID, progression, duplicate-listing warnings and the BepInEx log.

Example: Haldor sells **10 raspberries for 20 coins**, but only after Eikthyr has been defeated:

```yaml
haldorSells:
  Raspberry:
    enabled: true
    price: 20
    amount: 10
    requiredGlobalKey: "defeated_eikthyr"
    category: Resources
```

**`price` is the price of ONE purchase**, not one raspberry. `amount` cannot exceed the prefab's maximum stack size.

#### Example: Let Haldor buy a new item from players

Add a record in the **same file**:

```yaml
haldorBuys:
  Raspberry:
    enabled: true
    price: 1
```

Haldor now pays **1 coin per raspberry** using his **ordinary sell coin button**. Selling a partial stack of **18 raspberries** pays **18 coins**, not the price of a full stack or all raspberries in the inventory. Each sell action handles **one actual eligible inventory stack**. Buying and selling values are independent, so you can use the same prefab in both sections with different prices.

**Do not paste two duplicate `haldorSells:` or `haldorBuys:` root sections.** Place additional items inside the existing corresponding section of the generated file. The examples above demonstrate the structure and are **not default offers**.

#### Example: Items from other mods and the Idols tab

Use a real prefab ID from the installed mod. For illustration, `MyMod_AncientIdol` below is only a **placeholder**, not an item HaldorExpansion installs:

```yaml
# Add this entry under the existing haldorSells: section
  MyMod_AncientIdol:
    enabled: true
    price: 1500
    amount: 1
    requiredGlobalKey: "defeated_bonemass"
    category: Idols
```

The `Idols` value assigns an item to the Idols shop tab; it **does not make an ordinary modded item function as an upgrade idol**. That behavior must already be implemented by the source mod.

#### Custom-trading fields and restrictions

| Field | Section | Allowed meaning |
| --- | --- | --- |
| `enabled` | Both | `true` or `false` (defaults to `true` if omitted) |
| `price` | Both | **Required positive whole number**, at least 1 |
| `amount` | `haldorSells` only | Units per purchase, **1–10,000**, capped at maximum stack size; default 1 |
| `requiredGlobalKey` | `haldorSells` only | Exact world key, or `""` for immediate access |
| `category` | `haldorSells` only | `Auto`, `Equipment`, `Wood`, `Metals`, `Resources`, `Other`, `Idols` |

Other important rules:

- Use **spaces, not tabs**. Root sections have no indentation, prefab IDs use **2 spaces**, and fields **4 spaces**.
- Prefab IDs are **case-sensitive**. Each item ID may occur only once **within each section**.
- The file supports a limited YAML **mapping** format, not arbitrary YAML lists, aliases, tags or inline objects.
- `price: 0` is **not valid in custom trading**, unlike the free built-in shop offer setting.
- `amount` is used by custom products; built-in `trading.yml` instead uses `stack`.
- The file limit is **1 MiB**, including comments. A malformed edit is rejected, the previous valid rules stay active, and the error with a line number appears in the BepInEx log.
- Only the **host/dedicated server's copy** controls custom offers in multiplayer. The `Enable Custom Trading` master toggle is also server-synchronized.
- Custom offers never silently overwrite existing Haldor or third-party shop listings. If a duplicate is detected, the existing entry is preserved.

#### Disable custom trading without losing your config

Set `Enable Custom Trading = false` under `[Custom Trading]` in `general.cfg`, or use the optional F1 Configuration Manager as host/admin. Both custom **buy** and **sell** rules are disabled, but your YAML file and individually saved `enabled` flags are preserved. **Built-in and original trading continue to work.**

### World-progression keys

A `requiredGlobalKey` limits a **shop offer** to a world state. Use an empty string for immediate availability or a key that becomes active after a boss is defeated.

| Global key | Boss |
| --- | --- |
| `defeated_eikthyr` | Eikthyr |
| `defeated_gdking` | The Elder |
| `defeated_bonemass` | Bonemass |
| `defeated_dragon` | Moder |
| `defeated_goblinking` | Yagluth |
| `defeated_queen` | The Queen |
| `defeated_fader` | Fader |
| `defeated_frozenking` | Deep North / Kall Fimbulbringer progression, if available |

For example, `requiredGlobalKey: "defeated_bonemass"` unlocks an offer after that key is active. Global keys are exact and case-sensitive. Other mods may define additional keys. The Valheim console command **`listkeys`** shows the keys **currently active** in your world, not all keys that could ever become active.

### Upgrading configuration from older versions

The mod attempts to migrate supported older settings and creates backups when required. Earlier 1.2.x configurations were reorganized into `general.cfg`, `balance.cfg` and `trading.yml`; version 1.3.5 also generates the optional `custom-trading.yml`. Preserve the files created by your current install.

**Do not delete individual required records** in migrated `trading.yml` or manually replace it with one of the examples in this README. Before major edits, keep your own copy of the whole config folder. If migration or parsing fails, check the BepInEx log and the mod's backup directory.

---

## Multiplayer and dedicated servers

Install **the same compatible HaldorExpansion release** on the dedicated server/host and on **every client**. The mod uses Jotunn network synchronization with `NetworkCompatibility(EveryoneMustHaveMod, Minor)`.

| Setting | Authority |
| --- | --- |
| `balance.cfg`: abilities, awakening, drops | Host/dedicated server |
| `trading.yml`: included shop stock, prices, sell values and progression | Host/dedicated server |
| `custom-trading.yml`: new buy/sell offers | Host/dedicated server |
| `Enable Custom Trading` | Host/server admin |
| Shop tab visibility, optional long-tooltip behavior, keybinds and personal HUD layout | Each player locally |

Clients receive server rules while connected, but their local files are **not permanently overwritten**. Editing your own `custom-trading.yml` cannot change another server's economy. The server can apply valid trade changes while players are connected without requiring clients to restart.

---

## Installation

**Mod manager (recommended):** install Haldor Expansion and its required dependencies **BepInEx 5** and **Jotunn** in your Valheim profile. Mod managers may install dependencies automatically.

**Manual install:** with BepInEx 5 and Jotunn already installed, place the plugin DLL in:

```text
BepInEx/plugins/aveasura-HaldorExpansion/HaldorExpansion.dll
```

Source PNG icon files are embedded into the DLL when built; players do **not** need to copy them. The `.csproj` file and `HaldorExpansion.Local.props` are needed only when building the mod from source.

---

## Troubleshooting

<details>
<summary><strong>A new custom item does not appear in Haldor's shop</strong></summary>

Check the **exact internal prefab ID** and whether the source mod is loaded. Ensure `enabled: true`, a positive price, and an active progression key (or `requiredGlobalKey: ""`). Make sure the Custom Trading master toggle is on. Other mods or the built-in catalog may already sell that item: duplicates are deliberately skipped. Also check category filtering and the BepInEx log.

</details>

<details>
<summary><strong>Haldor will not buy an item added to haldorBuys</strong></summary>

Verify that you added it to `haldorBuys:` (not `haldorSells:`) with the correct prefab ID, `enabled: true` and a **positive per-item price**. Use Haldor's **normal sell coin button**. Every click sells one eligible inventory stack. For rejected YAML, check the BepInEx log.

</details>

<details>
<summary><strong>The shop is empty or Idols are missing</strong></summary>

Look at `[Trading UI]` in `general.cfg` or the optional F1 menu. Disabling categories together with `Hide items from disabled tabs = true` removes their inventory rows, **including vanilla goods**. If all categories are disabled, the shop becomes empty. Turning off `Enable Haldor tabs` alone only changes the visual layout.

</details>

<details>
<summary><strong>My YAML changes are not applied</strong></summary>

Confirm you edited the correct file: known built-in offers belong in `trading.yml`; new offers belong in `custom-trading.yml`. Keep the correct indentation, quoted progression strings, and the required fields. Changes are polled while the game runs, but invalid files retain the **last valid configuration**. Check the BepInEx log for the affected line. Closing/reopening the shop may help refresh what is displayed.

</details>

<details>
<summary><strong>My settings work locally but not on a server</strong></summary>

The multiplayer **host/server controls gameplay and trading rules**. Connected clients cannot override `balance.cfg`, `trading.yml` or `custom-trading.yml`. Have the server administrator make the changes on the dedicated server, then check the BepInEx logs. Personal UI and keybind settings remain local.

</details>

<details>
<summary><strong>I cannot upgrade an item with Haldor's Adamantine Idol</strong></summary>

You need an **upgrader crafting station**, the equipment in your inventory and a compatible **underlying idol-based upgrade recipe**. The Adamantine alternative is not a universal recipe for every item; it appears only when an eligible source recipe exists. HaldorExpansion's own 12 upgradeable artifacts have dedicated recipes; the one-use Thread of the Norns does not.

</details>

<details>
<summary><strong>An artifact's awakened effect is not active</strong></summary>

Check the item's **own quality**, not just the quality of another copy. The default threshold is **10**, but the server can change it. Some abilities also require the correct weapon, armor/set combination or other conditions. If quality drops below the configured threshold, awakening becomes inactive until the item qualifies again.

</details>

<details>
<summary><strong>Compatibility / UI issues</strong></summary>

Try disabling optional long-tooltip scrolling or Haldor's category-tab UI if another mod changes the same panels. This does not disable the entire trading expansion. Back up your configs, check the BepInEx log and report reproducible issues at [GitHub Issues](https://github.com/aveasura/HaldorExpansion/issues).

</details>

---

## Compatibility and optional mods

HaldorExpansion has previously been used alongside mods such as `Smoothbrain-CreatureLevelAndLootControl`, `warpalicious-Monster_Modifiers`, `ASharpPen-Custom_Raids`, `Azumatt-AzuExtendedPlayerInventory`, `Azumatt-AzuSkillTweaks`, `rendl0449-CraftFromContainers`, `Advize-PlantEverything`, and `Marf-FuelEternal`.

These are **not required dependencies**. Prior coexistence does **not** guarantee that every mod combination or newer version is fully compatible. Required dependencies remain **BepInEx 5** and **Jotunn**.

## Building from source

Development requires Valheim, BepInEx 5, Jotunn, **.NET Framework 4.8**, and Visual Studio or Rider. The main project is `HaldorExpansion/HaldorExpansion.csproj`.

For local game, mod-manager and server paths, copy `HaldorExpansion/HaldorExpansion.Local.props.example` to `HaldorExpansion/HaldorExpansion.Local.props` and adjust it for your computer. This local file should **not be committed to Git**.

Supported MSBuild path properties: `R2ModmanProfileDir`, `ValheimInstallDir`, `ValheimDedicatedServerDir`, `ClientPluginDeployDir`, `ServerPluginDeployDir`. Original icons live in `HaldorExpansion/Assets/Icons/` and are embedded in the compiled DLL.

---

## More information & feedback

- [Download on Thunderstore](https://thunderstore.io/c/valheim/p/aveasura/HaldorExpansion/) · [Nexus Mods](https://www.nexusmods.com/valheim/mods/4075) · [Hexium](https://valheim.hexium.gg/mods/aveasura/HaldorExpansion)
- [Changelog](CHANGELOG.md)
- [Report Bugs & Issues](https://github.com/aveasura/HaldorExpansion/issues)
- **Discord:** Message me at `@aveasura` for questions, feedback or suggestions.

## ❤️ Support Development

HaldorExpansion is free and will remain free.

If you enjoy the mod and would like to support its continued development, you can do so on **Boosty**. Your support is entirely optional, but always appreciated! 🍻

**[Support on Boosty](https://boosty.to/aveasura)**
