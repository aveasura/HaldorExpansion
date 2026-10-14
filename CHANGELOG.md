# Changelog

All notable changes to HaldorExpansion will be documented in this file.

## [1.3.0]

### Added
- **Berserker Equipment:** Introduced the Berserker Greataxe, Helmet, and Cloak — a new combat-focused equipment set built around relentless aggression, growing power, and high-risk gameplay.
- **Wrath System:** A stacking combat mechanic that rewards aggressive play with increasingly powerful offensive and defensive bonuses.
- **Blood Debt:** A new combat resource that fuels the Berserker's devastating abilities, featuring a dedicated HUD indicator.
- **Customizable Blood Debt HUD:** Added client-side controls for visibility, positioning, scaling, and dimensions.
- **Reckoning:** A powerful secondary attack that unleashes accumulated Blood Debt against enemies while restoring health.
- **Ragnarök:** A devastating meteor storm that scales with accumulated Blood Debt, featuring cinematic visuals, darkened skies, and destructive impact effects.
- **Exhaustion:** A temporary penalty following Ragnarök, adding an element of risk to the Berserker's ultimate ability.
- **Berserker Trading:** New equipment available from Haldor, with configurable prices and progression requirements.

- **Forge of Possibilities:** Added upgrade support for 12 unique HaldorExpansion artifacts. Equipment can be upgraded beyond quality level 10, allowing players to continue strengthening their gear.
- **Awakened Potential:** Introduced unique awakened abilities and passive bonuses for 11 of the 12 upgradeable artifacts upon reaching quality level 10. Awakened powers remain active at all subsequent upgrade levels. The awakened ability for **Hel's Oath (bow)** is still in development and will be added in a future update.
- **Haldor Idol:** Introduced a new upgrade resource obtainable from eligible loot sources alongside existing idols, without replacing their original drops.
- **Idol Trading:** Haldor now buys and sells all 16 vanilla idols and the custom Haldor Idol, with individually configurable prices and availability.

- **Facebreaker:** Introduced a new active ability for Bone Crushers that pulls enemies together, slowing them and staggering those caught on opposite sides.
- **Custom Facebreaker Animation:** Added a unique casting animation with directional character positioning, synchronized enemy pulling, impact, and knockback.
- **Facebreaker Sound Effects:** Added custom impact sounds synchronized with the casting animation, including distinct audio for staggered and non-staggered targets.

- **Haldor's Shop Categories:** Added category tabs for easier navigation through Haldor's inventory.
- **Scrollable Item Tooltips:** Added scrolling support for long item descriptions, improving accessibility on smaller screens.
- **Artifact Lore:** Added original Norse-inspired stories and interconnected sagas for 12 relics, fully localized in English and Russian.
- **New Trading Resources:** Added Sap, Refined Eitr, Soft Tissue, Linen Thread, and Black Metal to Haldor's inventory.

- **In-Game Configuration:** Added optional BepInEx ConfigurationManager support, allowing players to manage mod settings directly in-game.
- **Live Configuration Reloading:** Configuration changes are automatically detected and applied without restarting the game.

### Changed
- **Bone Crushers:** Improved charge generation scaling based on health lost, increased attack speed, and removed hit-stop interruptions for faster, smoother combat.
- **Pit King's Mantle:** Adjusted its health bonuses and synergy with Bone Crushers.
- **Retribution (formerly Nova):** Increased the area of effect to improve crowd control and combat effectiveness.
- **Cloak of the Wounded Beast:** Adjusted passive healing interactions with damage-over-time effects.
- **Cuirass of Silent Reckoning:** Improved its damage-delaying effect to also activate on potentially lethal hits, even below the normal damage threshold.
- **Thread of the Norns:** Improved its protection against lethal hits and interaction with the Cuirass of Silent Reckoning.

- **Haldor's Shop:** Item previews now feature atmospheric lore, while inventory tooltips retain detailed gameplay information.
- **Configuration System:** Reorganized settings into `general.cfg`, `balance.cfg`, and `trading.yml` for easier customization.
- **Trading Configuration:** Expanded customization options for Haldor's inventory, including item availability, quantities, pricing, and progression requirements.
- **Equipment Descriptions:** Improved readability, formatting, and localization across existing equipment.

### Fixed
- Fixed equipped HaldorExpansion items occasionally appearing invisible after loading into the game.
- Fixed equipment preview and item icon issues in Haldor's shop.
- Fixed damage calculation inconsistencies involving the Cuirass of Silent Reckoning and other equipment effects.

### Performance
- Significantly improved Haldor's shop responsiveness and category switching.
- Optimized item loading, filtering, and UI updates.
- Reduced unnecessary configuration file operations.
- Improved overall efficiency of configuration monitoring and live reloading.

### Internal
- Improved configuration migration and compatibility with existing settings.
- Improved item registration and equipment repair handling.
- Refined combat effect processing and Harmony patch compatibility.
- Removed temporary debugging and profiling code.