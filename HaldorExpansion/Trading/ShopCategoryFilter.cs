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
            "Wood", "$item_wood",
            "FineWood", "$item_finewood",
            "RoundLog", "$item_roundlog",
            "ElderBark", "$item_elderbark",
            "YggdrasilWood", "$item_yggdrasilwood",
            "Blackwood", "$item_blackwood",
            "Frostwood", "$item_frostwood",
        };

        private static readonly HashSet<string> MetalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CopperOre", "$item_copperore",
            "TinOre", "$item_tinore",
            "SilverOre", "$item_silverore",
            "IronScrap", "$item_ironscrap",
            "CopperScrap", "$item_copperscrap",
            "BronzeScrap", "$item_bronzescrap",
            "BlackMetalScrap", "$item_blackmetalscrap",
            "FlametalOre", "$item_flametalore",
            "FlametalOreNew", "$item_flametalore_new",
            "Copper", "$item_copper",
            "Tin", "$item_tin",
            "Bronze", "$item_bronze",
            "Iron", "$item_iron",
            "Silver", "$item_silver",
            "BlackMetal", "$item_blackmetal",
            "Flametal", "$item_flametal",
            "FlametalNew", "$item_flametal_new",
            "Gold", "$item_gold"
        };

        internal static ShopCategory Classify(string itemType, string prefab, string sharedName)
        {
            // Idols share the existing Other tab; buying and selling remain independently configurable.
            if (HaldorIdolCatalog.IsIdol(prefab))
            {
                return ShopCategory.Other;
            }
            
            ShopCategory basic = Classify(itemType);
            
            if (basic != ShopCategory.Resources)
            {
                return basic;
            }
            
            if (WoodIds.Contains(prefab ?? "") || WoodIds.Contains(sharedName ?? ""))
            {
                return ShopCategory.Wood;
            }
            
            if (MetalIds.Contains(prefab ?? "") || MetalIds.Contains(sharedName ?? ""))
            {
                return ShopCategory.Metals;
            }
            
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