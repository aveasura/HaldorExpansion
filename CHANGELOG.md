## 1.3.5
### Added
- **Custom Haldor Trading:** Added `custom-trading.yml`, automatically generated on first launch. Players can configure additional items for Haldor to sell and buy, including items from other mods.
- **Custom Shop Offers:** The `haldorSells` section supports custom prices, quantities per purchase, progression requirements, and item categories, including `Idols` for items from other mods.
- **Custom Item Sales:** The `haldorBuys` section allows players to sell configured items to Haldor using the standard sell button. Prices are set per item, and each sale uses one actual inventory stack, including partial stacks.
- **Custom Trading Toggle:** Added a server-synchronized **Enable Custom Trading** setting in F1. Both custom trading sections can be disabled without deleting the YAML file or affecting built-in offers.
- **Live Configuration:** Custom trading changes apply without restarting the game and synchronize in multiplayer. Server trading rules take priority over local client configuration.
- **Haldor's Shop:** Added a dedicated **Idols** category containing all 18 idols (16 vanilla and 2 custom). The tab can be enabled or disabled through the in-game configuration (F1).

### Changed
- **Haldor's Shop UI:** Improved category tab layout and spacing.
- **Custom Trading Performance:** Reduced temporary memory allocations during shop refreshes and improved handling of large catalogs with mixed custom and third-party offers.
- **Shop Filtering Description:** Clarified that **Hide Items From Disabled Tabs** also hides Haldor's original vanilla items, works when tabs are disabled, and leaves the shop empty if all categories are disabled.
- **Trading Balance:** Adjusted default prices for idols and trophies.
- **Cloak of the Wounded Beast:** Added a missing custom icon and redesigned its color palette.
- **Cloak of Burned Resolve:** Added a missing custom icon and redesigned its color palette with scorched graphite and ash tones.
- **Pit King's Mantle:** Updated its color palette for a more distinctive appearance.

### Notes
- Custom trading offers are configured through YAML. F1(BepInEx Configuration Manager) provides the master toggle.
- Prices in `haldorSells` apply to the entire purchase quantity. Prices in `haldorBuys` apply to each individual item.
- Shop tab preferences remain personal, while custom trading rules are controlled by the server.


## 1.3.4
### Fixed
- Haldor's Shop: Fixed Flametal being sold as the outdated Ancient Metal instead of the correct Ashlands Flametal.
- Trading Configuration: Added automatic migration from `Flametal` to `FlametalNew`, preserving existing custom prices, stack sizes, availability, and progression requirements.

- Note: Ancient Metal purchased before this fix will remain unchanged in existing inventories and containers. Only future purchases from Haldor are affected.

## 1.3.3
- Hel's Oath: Introduced a new awakened ability - Ghosts of the Last Hunt. Successful hits summon up to three spectral archers that mirror their master's shots and unleash a devastating final volley during Hel's Embrace.
- Hel's Oath: Added **Soul Swap**, allowing players to teleport to a summoned ghost by sacrificing its soul and a portion of maximum stamina.
- Hel's Oath: Ghosts feature independent shooting animations, intelligent positioning, and a "Guardian" that maintains distance to provide tactical opportunities for Soul Swap. Their spectral arrows can pass through walls and obstacles.
- Hel's Oath: Optimized visual effects and enhanced Hel's Embrace with redesigned, symmetrical spectral spikes.

- Bone Crushers: Fixed an issue where damage-over-time effects could bypass the ability's shield.
- Bone Crushers: Added customizable shield HUD positioning, preserving the original layout by default.

- Forge of Possibilities: Added a configurable item awakening level (default: 10).

- Controls: Fixed ability key conflicts with interactions. Facebreaker's default key is now E, while existing custom keybindings are preserved.

## 1.3.2
- Improved overall performance and optimization.
- Improved the drop mechanics of Haldor's Idols and Adamantine Haldor's Idols in chests, including support for world loot multipliers.
- Added Bloodgold, Timberwood, and Ice to Haldor's shop.
- Frostcores can now be sold to Haldor.
- Fixed an issue where Facebreaker's ability range indicator would sometimes fail to appear in multiplayer.
- The descriptions for certain items have been improved.

## 1.3.1
- Fixed Haldor's Idol drop compatibility with the latest Valheim update.
- No other gameplay or balance changes.

## 1.3.0

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
- **Adamantine Haldor Idol:** Introduced a new upgrade resource that protects equipment from destruction when an upgrade fails, though failure may still reduce its quality.
- **Idol Trading:** Haldor now buys and sells all 16 vanilla idols and both custom HaldorExpansion idols, with individually configurable prices and availability.

- **Facebreaker:** Introduced a new active ability for Bone Crushers that pulls enemies together, slowing them and staggering those caught on opposite sides.
- **Custom Facebreaker Animation:** Added a unique casting animation with directional character positioning, synchronized enemy pulling, impact, and knockback.
- **Facebreaker Sound Effects:** Added custom impact sounds synchronized with the casting animation, including distinct audio for staggered and non-staggered targets.

- **Haldor's Shop Categories:** Added category tabs for easier navigation through Haldor's inventory.
- **Scrollable Item Tooltips:** Added scrolling support for long item descriptions, improving accessibility on smaller screens.
- **Artifact Lore:** Added original Norse-inspired stories and interconnected sagas for 12 relics, fully localized in English and Russian.
- **Interconnected Idol Lore:** Added two emotional, interconnected stories for Haldor's idols, featuring hidden connections to existing artifact lore and subtle clues about their mysterious origins.
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