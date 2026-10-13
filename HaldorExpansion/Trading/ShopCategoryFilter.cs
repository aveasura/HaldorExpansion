using System;
using System.Collections.Generic;

namespace HaldorExpansion.Trading
{
    internal enum ShopCategory
    {
        All,
        Equipment,
        Wood,
        Metals,
        Resources,
        Other
    }

    internal static class ShopCategoryFilter
    {
        // Match exact IDs/keys, never translated text or substrings (wooden shields are equipment).
        private static readonly HashSet<string> WoodIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood",
            "$item_wood", "$item_finewood", "$item_roundlog", "$item_elderbark", "$item_yggdrasilwood",
            "$item_blackwood"
        };

        private static readonly HashSet<string> MetalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CopperOre", "TinOre", "SilverOre", "IronScrap", "CopperScrap", "BronzeScrap",
            "BlackMetalScrap", "FlametalOre", "FlametalOreNew", "Copper", "Tin", "Bronze", "Iron", "Silver",
            "BlackMetal", "Flametal", "FlametalNew", "$item_copperore", "$item_tinore", "$item_silverore",
            "$item_ironscrap", "$item_copperscrap", "$item_bronzescrap", "$item_blackmetalscrap",
            "$item_flametalore", "$item_flametalore_new", "$item_copper", "$item_tin", "$item_bronze",
            "$item_iron", "$item_silver", "$item_blackmetal", "$item_flametal", "$item_flametal_new"
        };

        internal static ShopCategory Classify(string itemType, string prefab, string sharedName)
        {
            ShopCategory basic = Classify(itemType);
            if (basic != ShopCategory.Resources) return basic;
            if (WoodIds.Contains(prefab ?? "") || WoodIds.Contains(sharedName ?? "")) return ShopCategory.Wood;
            if (MetalIds.Contains(prefab ?? "") || MetalIds.Contains(sharedName ?? "")) return ShopCategory.Metals;
            return basic;
        }

        // Unknown/new game item types remain visible in Other and All.
        internal static ShopCategory Classify(string itemType)
        {
            switch (itemType)
            {
                case "OneHandedWeapon":
                case "TwoHandedWeapon":
                case "TwoHandedWeaponLeft":
                case "Bow":
                case "Shield":
                case "Helmet":
                case "Chest":
                case "Legs":
                case "Hands":
                case "Shoulder":
                case "Utility":
                case "Tool":
                case "Torch":
                case "Trinket": return ShopCategory.Equipment;
                case "Material": return ShopCategory.Resources;
                default: return ShopCategory.Other;
            }
        }

        internal static List<T> Filter<T>(List<T> items, ShopCategory category, Func<T, ShopCategory> classify,
            Func<ShopCategory, bool> allowed = null)
        {
            if (category == ShopCategory.All && allowed == null) return items;
            var result = new List<T>();
            foreach (T item in items)
            {
                ShopCategory actual = classify(item);
                if ((category == ShopCategory.All || actual == category) && (allowed == null || allowed(actual)))
                    result.Add(item);
            }

            return result;
        }

        internal static int ClampSelection(int index, int count)
        {
            return count == 0 || index < 0 ? -1 : Math.Min(index, count - 1);
        }
    }
}