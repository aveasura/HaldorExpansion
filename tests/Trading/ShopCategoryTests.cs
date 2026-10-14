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
        foreach (HaldorIdolCatalog.Entry idol in HaldorIdolCatalog.Items)
            Check(ShopCategoryFilter.Classify("Material", idol.Id, "$item_upgrader") == ShopCategory.Other,
                "idol belongs to Other tab: " + idol.Id);
        Check(ShopCategoryFilter.Classify("Material", "Stone", "$item_upgrader") == ShopCategory.Resources,
            "unrelated material must not be classified as idol");
        var mixedStock = new List<string> { "Stone", "Upgrader0Weapon", "aveasura_he_idol", "Wood" };
        Func<string, ShopCategory> classifyMixed = id => ShopCategoryFilter.Classify("Material", id, null);
        var idolsInOther = ShopCategoryFilter.Filter(mixedStock, ShopCategory.Other, classifyMixed);
        Check(string.Join(",", idolsInOther) == "Upgrader0Weapon,aveasura_he_idol",
            "both vanilla and custom idols are visible in Other, in original order");
        var hideOther = ShopCategoryFilter.Filter(mixedStock, ShopCategory.All, classifyMixed,
            c => c != ShopCategory.Other);
        Check(string.Join(",", hideOther) == "Stone,Wood",
            "disabling Other with Hide items from disabled tabs also hides idols from All");
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
        foreach (string id in new[] { "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood" })
            Check(ShopCategoryFilter.Classify("Material", id, null) == ShopCategory.Wood, id + " is wood");
        foreach (string id in new[] { "CopperOre", "TinOre", "SilverOre", "IronScrap", "CopperScrap", "BlackMetalScrap", "FlametalOreNew", "Iron", "Copper", "Tin", "Silver", "Bronze", "FlametalNew", "Flametal", "BlackMetal" })
            Check(ShopCategoryFilter.Classify("Material", id, null) == ShopCategory.Metals, id + " is ore/metal");
        foreach (string id in new[] { "Sap", "Eitr", "Resin", "Stone", "Obsidian", "Coal", "AncientSeed", "ModUnknownMaterial", "WoodenToken" })
            Check(ShopCategoryFilter.Classify("Material", id, null) == ShopCategory.Resources, id + " remains a resource");
        Check(ShopCategoryFilter.Classify("Shield", "Wood", "$item_wood") == ShopCategory.Equipment, "equipment wins over resource identity");
        Check(ShopCategoryFilter.Classify("Material", "ModCoreWood", "$item_roundlog") == ShopCategory.Wood, "mod clone recognized through shared key");
        Check(ShopCategoryFilter.Classify("ModUnknownType", "Unknown", null) == ShopCategory.Other, "unknown mod type remains Other");
        var stock = new List<string> { "Copper", "Wood", "Sap", "Iron", "Eitr", "FineWood" };
        Func<string, ShopCategory> classify = id => ShopCategoryFilter.Classify("Material", id, null);
        Func<ShopCategory, bool> noMetals = c => c != ShopCategory.Metals;
        var limited = ShopCategoryFilter.Filter(stock, ShopCategory.All, classify, noMetals);
        Check(string.Join(",", limited) == "Wood,Sap,Eitr,FineWood", "sales ban also filters ALL without reordering");
        Check(ShopCategoryFilter.Filter(stock, ShopCategory.Metals, classify, noMetals).Count == 0, "banned category cannot bypass sales policy");
        Check(ReferenceEquals(stock, ShopCategoryFilter.Filter(stock, ShopCategory.All, classify)), "restoring policy returns original list and order");
        Check(string.Join(",", stock) == "Copper,Wood,Sap,Iron,Eitr,FineWood", "policy never mutates source stock");
        Console.WriteLine(passed + " checks passed");
    }
}
