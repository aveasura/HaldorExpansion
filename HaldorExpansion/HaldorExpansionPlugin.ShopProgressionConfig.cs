using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Extensions;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static readonly Dictionary<string, ConfigEntry<string>> ShopRequiredGlobalKeys =
            new Dictionary<string, ConfigEntry<string>>();

        private static readonly Dictionary<string, ConfigEntry<int>> ShopPrices =
            new Dictionary<string, ConfigEntry<int>>();

        private readonly HashSet<string> ShopProgressionSectionsWithBossHelp =
            new HashSet<string>();

        private void RegisterShopProgressionConfig()
        {
            // Custom HaldorExpansion items.
            BindShopItemConfig("Shop Progression - Custom Items", RingPrefabName, "Corrupted Brisingamen", RingPrice);
            BindShopItemConfig("Shop Progression - Custom Items", WoundedBeastCapePrefabName, "Cloak of the Wounded Beast", WoundedBeastCapePrice);
            BindShopItemConfig("Shop Progression - Custom Items", StaminaCapeFeature.PrefabName, "Fractured Breath Cape", StaminaCapeFeature.Price);
            BindShopItemConfig("Shop Progression - Custom Items", DelayedDoomChestPrefabName, "Cuirass of Silent Reckoning", DelayedDoomChestPrice);
            BindShopItemConfig("Shop Progression - Custom Items", PitKingChestPrefabName, "Pit King's Cuirass", PitKingChestPrice);
            BindShopItemConfig("Shop Progression - Custom Items", GritCestusPrefabName, "Grit Cestus", GritCestusPrice);
            BindShopItemConfig("Shop Progression - Custom Items", ShadowCrossbowPrefabName, "Crossbow of the Shadow Hunt", ShadowCrossbowPrice);

            // Metals.
            BindShopItemConfig("Shop Progression - Metals", "Iron", "Iron", 225);
            BindShopItemConfig("Shop Progression - Metals", "Copper", "Copper", 310);
            BindShopItemConfig("Shop Progression - Metals", "Tin", "Tin", 180);
            BindShopItemConfig("Shop Progression - Metals", "Silver", "Silver", 550);
            BindShopItemConfig("Shop Progression - Metals", "Flametal", "Flametal", 800);

            // Stone / building materials.
            BindShopItemConfig("Shop Progression - Building Materials", "Stone", "Stone", 20);
            BindShopItemConfig("Shop Progression - Building Materials", "Grausten", "Grausten", 25);
            BindShopItemConfig("Shop Progression - Building Materials", "BlackMarble", "Black Marble", 40);

            // Wood.
            BindShopItemConfig("Shop Progression - Wood", "Wood", "Wood", 10);
            BindShopItemConfig("Shop Progression - Wood", "RoundLog", "Core Wood", 10);
            BindShopItemConfig("Shop Progression - Wood", "ElderBark", "Ancient Bark", 30);
            BindShopItemConfig("Shop Progression - Wood", "FineWood", "Fine Wood", 150);
            BindShopItemConfig("Shop Progression - Wood", "YggdrasilWood", "Yggdrasil Wood", 250);
            BindShopItemConfig("Shop Progression - Wood", "Ashwood", "Ashwood", 30);
            BindShopItemConfig("Shop Progression - Wood", "Blackwood", "Blackwood", 100);
            BindShopItemConfig("Shop Progression - Wood", "CharredWood", "Charred Wood", 50);

            // Textiles.
            BindShopItemConfig("Shop Progression - Textiles", "JuteRed", "Red Jute", 250);
            BindShopItemConfig("Shop Progression - Textiles", "JuteBlue", "Blue Jute", 250);

            // Other materials.
            BindShopItemConfig("Shop Progression - Materials", "Resin", "Resin", 100);
            BindShopItemConfig("Shop Progression - Materials", "Feathers", "Feathers", 150);
            BindShopItemConfig("Shop Progression - Materials", "GreydwarfEye", "Greydwarf Eye", 200);
            BindShopItemConfig("Shop Progression - Materials", "Tar", "Tar", 250);
            BindShopItemConfig("Shop Progression - Materials", "Crystal", "Crystal", 250);
            BindShopItemConfig("Shop Progression - Materials", "Needle", "Needle", 250);
        }

        private const string ShopProgressionDescription =
            "World global key required before this item appears in Haldor's shop. " +
            "Leave empty to keep the item always available.";

        private const string ShopPriceDescription =
            "Price in coins for this item in Haldor's shop. " +
            "Negative values are treated as 0.";

        private const string BossGlobalKeyHelp =
            ShopProgressionDescription + "\n" +
            "Vanilla boss keys:\n" +
            "defeated_eikthyr - Eikthyr\n" +
            "defeated_gdking - The Elder\n" +
            "defeated_bonemass - Bonemass\n" +
            "defeated_dragon - Moder\n" +
            "defeated_goblinking - Yagluth\n" +
            "defeated_queen - The Queen\n" +
            "defeated_fader - Fader\n" +
            "defeated_frozenking - Kall Fimbulbringer";

        private void BindShopItemConfig(string section, string prefabName, string displayName, int defaultPrice)
        {
            BindShopPrice(section, prefabName, displayName, defaultPrice);
            BindShopRequiredGlobalKey(section, prefabName, displayName);
        }

        private void BindShopPrice(string section, string prefabName, string displayName, int defaultPrice)
        {
            string configKey = SanitizeConfigKey(displayName) + " Price";

            ConfigEntry<int> entry = Config.BindConfig(
                section,
                configKey,
                defaultPrice,
                ShopPriceDescription,
                synced: true);

            ShopPrices[prefabName] = entry;
        }

        private void BindShopRequiredGlobalKey(string section, string prefabName, string displayName)
        {
            bool firstEntryInSection = ShopProgressionSectionsWithBossHelp.Add(section);
            string configKey = SanitizeConfigKey(displayName) + " RequiredGlobalKey";

            ConfigEntry<string> entry = Config.BindConfig(
                section,
                configKey,
                string.Empty,
                firstEntryInSection ? BossGlobalKeyHelp : ShopProgressionDescription,
                synced: true);

            ShopRequiredGlobalKeys[prefabName] = entry;
        }

        private static string SanitizeConfigKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            return value
                .Replace("=", string.Empty)
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ")
                .Replace("\\", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("'", string.Empty)
                .Replace("[", "(")
                .Replace("]", ")")
                .Trim();
        }

        internal static int GetShopPrice(string prefabName, int fallbackPrice)
        {
            if (string.IsNullOrEmpty(prefabName))
                return fallbackPrice < 0 ? 0 : fallbackPrice;

            ConfigEntry<int> entry;
            if (!ShopPrices.TryGetValue(prefabName, out entry) || entry == null)
                return fallbackPrice < 0 ? 0 : fallbackPrice;

            return entry.Value < 0 ? 0 : entry.Value;
        }

        internal static string GetShopRequiredGlobalKey(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return string.Empty;

            ConfigEntry<string> entry;
            if (!ShopRequiredGlobalKeys.TryGetValue(prefabName, out entry) || entry == null)
                return string.Empty;

            return NormalizeGlobalKey(entry.Value);
        }

        private static string NormalizeGlobalKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
