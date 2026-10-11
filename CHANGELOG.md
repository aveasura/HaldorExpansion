# Changelog

All notable changes to HaldorExpansion will be documented in this file.

## [1.3.0] - Unreleased

### Added
- **Berserker Equipment:** Introduced the Berserker Greataxe, Helmet, and Cloak — a new combat-focused equipment set built around relentless aggression, growing power, and high-risk gameplay.
- **Wrath System:** A stacking combat mechanic that rewards aggressive play with increasingly powerful offensive and defensive bonuses.
- **Blood Debt:** A new combat resource that fuels the Berserker's devastating abilities, featuring a dedicated HUD indicator.
- **Reckoning:** A powerful secondary attack that unleashes accumulated Blood Debt against enemies while restoring health.
- **Ragnarök:** A devastating meteor storm that scales with accumulated Blood Debt, featuring cinematic visuals, darkened skies, and destructive impact effects.
- **Exhaustion:** A temporary penalty following Ragnarök, adding an element of risk to the Berserker's ultimate ability.
- **Berserker Trading:** New equipment available from Haldor, with configurable prices and progression requirements.
- **New Trading Resources:** Added Sap, Refined Eitr, and Soft Tissue to Haldor's inventory.
- **In-Game Configuration:** Added optional BepInEx ConfigurationManager support, allowing players to manage mod settings directly in-game.
- **Customizable Blood Debt HUD:** Added client-side controls for visibility, positioning, scaling, and dimensions.
- **Live Configuration Reloading:** Configuration changes are automatically detected and applied without restarting the game.
- **Haldor's Shop Categories:** Added category tabs for easier navigation through Haldor's inventory.
- **Scrollable Item Tooltips:** Added scrolling support for long item descriptions, improving accessibility on smaller screens.
- **Artifact Lore:** Added original Norse-inspired stories and interconnected sagas for 12 relics, fully localized in English and Russian.

### Changed
- **Haldor's Shop:** Item previews now feature atmospheric lore, while inventory tooltips retain detailed gameplay information.
- **Equipment Descriptions:** Improved readability, formatting, and localization across existing equipment.
- **Configuration System:** Reorganized settings into `general.cfg`, `balance.cfg`, and `trading.yml` for easier customization.
- **Trading Configuration:** Expanded customization options for Haldor's inventory, including item availability, quantities, pricing, and progression requirements.
- **Delayed Doom:** Improved activation conditions to account for potentially lethal attacks.
- **Norn Thread:** Improved its protective effect and interaction with Delayed Doom.
- **Cestus:** Improved charge generation scaling based on health lost.
- **Pit King:** Adjusted its health bonuses and synergy with Bone Crushers.
- **Wounded Beast Cloak:** Adjusted passive healing interactions with damage-over-time effects.

### Fixed
- Fixed equipped HaldorExpansion items occasionally appearing invisible after loading into the game.
- Fixed damage calculation inconsistencies involving Delayed Doom and other equipment effects.
- Fixed equipment preview and item icon issues in Haldor's shop.

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