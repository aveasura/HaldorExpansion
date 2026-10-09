using System;
using System.Collections.Generic;

namespace HaldorExpansion.Trading
{
    internal enum ShopCategory
    {
        All,
        Equipment,
        Resources,
        Other
    }

    internal static class ShopCategoryFilter
    {
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

        internal static List<T> Filter<T>(List<T> items, ShopCategory category, Func<T, ShopCategory> classify)
        {
            if (category == ShopCategory.All) return items;
            var result = new List<T>();
            foreach (T item in items)
                if (classify(item) == category)
                    result.Add(item);
            return result;
        }

        internal static int ClampSelection(int index, int count)
        {
            return count == 0 || index < 0 ? -1 : Math.Min(index, count - 1);
        }
    }
}