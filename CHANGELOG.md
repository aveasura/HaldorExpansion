# Changelog

All notable changes to HaldorExpansion will be documented in this file.

## [1.3.0] - Unreleased

### Added
- **Berserker Equipment:** Introduced the Berserker Greataxe, Helmet, and Cloak, forming a new combat-focused equipment combination with unique passive abilities.
- **Wrath System:** A five-stage stacking mechanic that rewards aggressive gameplay with increased attack speed and reduced stamina consumption.
- **Blood Debt:** A new combat resource with a dedicated HUD indicator, built through combat and used to empower special abilities.
- **Reckoning:** A powerful secondary attack that consumes Blood Debt to deal additional damage and restore health.
- **Ragnarök:** A devastating meteor-based ability featuring dynamic damage scaling, 12–30 meteors, expanding impact areas, warning effects, and enhanced visual and sound effects.
- **Berserker Helmet:** Added Wrath-based defensive bonuses and lifesteal that scales with damage actually dealt to enemies.
- **Exhaustion:** A temporary movement penalty following Ragnarök to balance its destructive power.
- **Berserker Trading:** Added configurable availability, pricing, and progression requirements for Berserker equipment at Haldor's shop.
- **Berserker Repairs:** Added forge repair support for Berserker equipment
- **Live Configuration Reloading:** Changes to `general.cfg`, `balance.cfg`, and `trading.yml` are automatically detected and applied without restarting Valheim.
- **Haldor's Shop Categories:** Added "All", "Equipment", "Resources", and "Other" tabs, which can be disabled in the configuration.
- **Scrollable Item Tooltips:** Added mouse wheel, Page Up, and Page Down support for scrolling long item descriptions, improving readability on smaller screens.(can be disabled in the configuration)
- **Artifact Lore:** Added original Norse-inspired stories and interconnected sagas for 12 relics, fully localized in English and Russian.

### Changed
- **Haldor's Shop:** Item previews now show atmospheric lore, while inventory tooltips retain detailed gameplay mechanics.
- **Equipment Descriptions:** Reworked English and Russian tooltips with clearer ability explanations, improved formatting, and reduced information overload.
- **Configuration System:** Reorganized settings into `general.cfg`, `balance.cfg`, and `trading.yml` under `BepInEx/config/aveasura-HaldorExpansion/`.
- **Trading Configuration:** Moved shop settings to `trading.yml`, retaining configurable prices and progression requirements while adding item availability controls and quantities.
- **Delayed Doom:** Now activates when a single incoming hit exceeds 50% of maximum health **or would otherwise be lethal**.
- **Norn Thread:** Triggering the amulet now clears accumulated Delayed Doom debt and extends protection from health loss from 1.5 to 3 seconds.
- **Cestus:** Charge generation now scales with the percentage of maximum health lost instead of a fixed amount of HP.
- **Pit King:** Now grants +20 maximum health alone, or +60 when Bone Crushers are equipped.
- **Wounded Beast Cloak:** Passive healing is now halved while fire, poison, or spirit damage-over-time effects are active.

### Fixed
- **Equipment Rendering:** Fixed equipped HaldorExpansion items being invisible on the character when first loading into the game.
- Fixed Delayed Doom evaluating damage before the Berserker, Wounded Beast, and Fractured cloak modifiers were applied.
- Fixed a Berserker Helmet identifier conflict with a vanilla item that prevented proper repair.
- Fixed equipment previews and item icon handling in Haldor's shop.
- Fixed shop items changing their original order after being disabled and re-enabled through configuration.
- Fixed missing or newly introduced configuration entries not being added to existing configuration files while preserving user settings.
- Fixed Jotunn warnings regarding item registration.

### Performance
- Reduced delays when opening Haldor's shop by caching item icons and avoiding unnecessary item reconfiguration.
- Optimized Haldor's category tabs to avoid redundant UI layout recalculations.
- Reduced redundant Blood Debt HUD updates.
- Optimized shop item processing, list filtering, and refresh behavior.
- Reduced unnecessary configuration file reads by checking file metadata before reloading.
- Added configurable polling intervals for live configuration updates (2 seconds by default, minimum 0.5 seconds).

### Internal
- Centralized Berserker item identifiers under a consistent naming scheme for improved registration and maintenance.
- Improved configuration migration with automatic addition of new settings, backups, schema validation, and format-version tracking.
- Improved item registration and hidden repair recipe handling.
- Refined combat effect processing and Harmony patch ordering.
- Removed temporary UI profiling and combat diagnostic code.