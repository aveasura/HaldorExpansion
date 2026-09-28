using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Extensions;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static readonly Dictionary<string, ConfigEntry<string>> ShopRequiredGlobalKeys =
            new Dictionary<string, ConfigEntry<string>>();

        private readonly HashSet<string> ShopProgressionSectionsWithBossHelp =
            new HashSet<string>();

        private void RegisterShopProgressionConfig()
        {
            // Custom HaldorExpansion items.
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", RingPrefabName, "Corrupted Brisingamen");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", WoundedBeastCapePrefabName, "Cloak of the Wounded Beast");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", StaminaCapeFeature.PrefabName, "Fractured Breath Cape");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", DelayedDoomChestPrefabName, "Cuirass of Silent Reckoning");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", PitKingChestPrefabName, "Pit King's Cuirass");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", GritCestusPrefabName, "Grit Cestus");
            BindShopRequiredGlobalKey("Shop Progression - Custom Items", ShadowCrossbowPrefabName, "Crossbow of the Shadow Hunt");

            // Metals.
            BindShopRequiredGlobalKey("Shop Progression - Metals", "Iron", "Iron");
            BindShopRequiredGlobalKey("Shop Progression - Metals", "Copper", "Copper");
            BindShopRequiredGlobalKey("Shop Progression - Metals", "Tin", "Tin");
            BindShopRequiredGlobalKey("Shop Progression - Metals", "Silver", "Silver");
            BindShopRequiredGlobalKey("Shop Progression - Metals", "Flametal", "Flametal");

            // Stone / building materials.
            BindShopRequiredGlobalKey("Shop Progression - Building Materials", "Stone", "Stone");
            BindShopRequiredGlobalKey("Shop Progression - Building Materials", "Grausten", "Grausten");
            BindShopRequiredGlobalKey("Shop Progression - Building Materials", "BlackMarble", "Black Marble");

            // Wood.
            BindShopRequiredGlobalKey("Shop Progression - Wood", "Wood", "Wood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "RoundLog", "Core Wood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "ElderBark", "Ancient Bark");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "FineWood", "Fine Wood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "YggdrasilWood", "Yggdrasil Wood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "Ashwood", "Ashwood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "Blackwood", "Blackwood");
            BindShopRequiredGlobalKey("Shop Progression - Wood", "CharredWood", "Charred Wood");

            // Textiles.
            BindShopRequiredGlobalKey("Shop Progression - Textiles", "JuteRed", "Red Jute");
            BindShopRequiredGlobalKey("Shop Progression - Textiles", "JuteBlue", "Blue Jute");

            // Other materials.
            BindShopRequiredGlobalKey("Shop Progression - Materials", "Resin", "Resin");
            BindShopRequiredGlobalKey("Shop Progression - Materials", "Feathers", "Feathers");
            BindShopRequiredGlobalKey("Shop Progression - Materials", "GreydwarfEye", "Greydwarf Eye");
            BindShopRequiredGlobalKey("Shop Progression - Materials", "Tar", "Tar");
            BindShopRequiredGlobalKey("Shop Progression - Materials", "Crystal", "Crystal");
            BindShopRequiredGlobalKey("Shop Progression - Materials", "Needle", "Needle");
        }

        private const string ShopProgressionDescription =
            "World global key required before this item appears in Haldor's shop. " +
            "Leave empty to keep the item always available.";

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
