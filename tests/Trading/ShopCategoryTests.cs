using System;
using System.Collections.Generic;
using HaldorExpansion.Trading;

class ShopCategoryTests
{
    static int passed;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
        passed++;
    }
    static void Main()
    {
        Check(ShopCategoryFilter.Classify("Helmet") == ShopCategory.Equipment, "helmet is equipment");
        foreach (string type in new[] { "OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft", "Bow", "Shield", "Chest", "Legs", "Hands", "Shoulder", "Utility", "Tool", "Torch", "Trinket" })
            Check(ShopCategoryFilter.Classify(type) == ShopCategory.Equipment, type + " is equipment");
        Check(ShopCategoryFilter.Classify("Material") == ShopCategory.Resources, "crafting material is a resource");
        foreach (string type in new[] { "Fish", "Consumable", "Trophy", "Ammo", "Misc", "FutureModType", null })
            Check(ShopCategoryFilter.Classify(type) == ShopCategory.Other, "unclassified item remains in Other: " + type);
        var items = new List<string> { "Material", "Helmet", "Mystery", "Bow", "Material" };
        var equipment = ShopCategoryFilter.Filter(items, ShopCategory.Equipment, ShopCategoryFilter.Classify);
        Check(equipment.Count == 2 && equipment[0] == "Helmet" && equipment[1] == "Bow", "filter preserves order");
        Check(items.Count == 5 && items[0] == "Material", "filter does not mutate assortment");
        Check(ReferenceEquals(items, ShopCategoryFilter.Filter(items, ShopCategory.All, ShopCategoryFilter.Classify)), "All restores original list");
        Check(ShopCategoryFilter.Filter(new List<string> { "Mystery" }, ShopCategory.Resources, ShopCategoryFilter.Classify).Count == 0, "empty category stays empty");
        Check(ShopCategoryFilter.ClampSelection(0, 0) == -1, "empty category clears selection");
        Check(ShopCategoryFilter.ClampSelection(12, 2) == 1, "shorter list clamps selection");
        Check(ShopCategoryFilter.ClampSelection(-1, 4) == -1, "deselection remains deselected");
        Check(ShopCategoryFilter.ClampSelection(1, 4) == 1, "valid selection stays unchanged");
        Console.WriteLine(passed + " checks passed");
    }
}
