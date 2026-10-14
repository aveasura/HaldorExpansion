using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Upgrades;
partial class RefinementTests
{
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS: " + name); }
    static ItemDrop Make(string name, int max = 4) {
        var p = new UnityEngine.GameObject { name = name };
        p.drop = new ItemDrop { gameObject = p };
        p.drop.m_itemData.m_dropPrefab = p;
        p.drop.m_itemData.m_shared.m_name = "$" + name;
        p.drop.m_itemData.m_shared.m_maxQuality = max;
        return p.drop;
    }
    static void Main() {
        ObjectDB.instance = new ObjectDB();
        var ordinary = Make("Upgrader0Weapon");
        IdolRegistration.Native = ordinary.gameObject;
        var rare = Make("aveasura_he_adamantine_idol");
        rare.m_itemData.m_shared.m_upgradeChance = .65f;
        AdamantineIdolRegistration.ItemDrop = rare;
        var equipment = Make("SwordBronze");
        var original = new Recipe { m_item = equipment, m_resources = new[] { new Piece.Requirement { m_resItem = ordinary, m_upgraderResource = true } } };
        ObjectDB.instance.m_recipes.Add(original);
        var player = new Player { station = new CraftingStation { m_upgrader = true } };
        var item = equipment.m_itemData.Clone(); item.m_quality = 10; item.m_durability = 17; item.m_equipped = true;
        item.m_customData["author"] = "preserve";
        player.inventory.items.Add(item);
        var idol = rare.m_itemData.Clone(); idol.m_stack = 5; player.inventory.items.Add(idol);
        var list = new List<Recipe> { original };
        AdamantineRefinement.AppendAvailable(player, list);
        Check(list.Count == 2 && list[0] == original, "explicit alternative; original recipe retained");
        var variant = list[1];
        Check(original.m_resources[0].m_resItem == ordinary, "original resource untouched");
        Check(variant.m_resources.Length == 1 && variant.m_resources[0].m_amount == 1, "one universal idol requirement");
        Check(AdamantineRefinement.TryRefine(player, variant, item, .9f, out var success) && !success && item.m_quality == 9, "Q10 failure -> Q9");
        Check(idol.m_stack == 4 && player.inventory.items.Contains(item) && item.m_equipped && item.m_durability == 17 && item.m_customData["author"] == "preserve", "same equipped instance, metadata, durability; consumed once");
        Check(AdamantineRefinement.TryRefine(player, variant, item, .2f, out success) && success && item.m_quality == 10 && idol.m_stack == 3, "Q9 success -> Q10");
        item.m_quality = 25;
        Check(AdamantineRefinement.TryRefine(player, variant, item, .65f, out success) && success && item.m_quality == 26, "native inclusive success boundary and quality above 10");
        item.m_quality = 1;
        Check(AdamantineRefinement.TryRefine(player, variant, item, 1f, out success) && !success && item.m_quality == 1 && idol.m_stack == 1, "Q1 fails safely even at maximum random value");
        Check(!AdamantineRefinement.TryRefine(player, original, item, 0f, out success) && idol.m_stack == 1, "ordinary attempts not intercepted");
        player.station.m_upgrader = false;
        Check(!AdamantineRefinement.TryRefine(player, variant, item, 0f, out success) && idol.m_stack == 1, "wrong station rejected");
        player.station.m_upgrader = true;
        var unsupported = Make("Stone", 1).m_itemData; player.inventory.items.Add(unsupported);
        Check(!AdamantineRefinement.TryRefine(player, variant, unsupported, 0f, out success) && idol.m_stack == 1, "wrong/unsupported target rejected");
        Check(AdamantineRefinement.TryRefine(player, variant, item, .9f, out success) && !player.inventory.items.Contains(idol), "last idol removed once");
        Check(!AdamantineRefinement.TryRefine(player, variant, item, 0f, out success) && item.m_quality == 1, "no idol means no attempt even in no-cost mode");
        var invalid = new Recipe { m_item = Make("Stone", 1) }; ObjectDB.instance.m_recipes.Add(invalid);
        var unknown = new List<Recipe> { invalid }; AdamantineRefinement.AppendAvailable(player, unknown);
        Check(unknown.Count == 1, "no recipe for unsupported items");
        player.inventory.items.Add(rare.m_itemData.Clone());
        player.RequirementsMet=false;
        Check(!AdamantineRefinement.TryRefine(player,variant,item,0f,out success), "native requirement rejection respected");
        player.RequirementsMet=true;
        original.m_resources = new Piece.Requirement[0];
        Check(!AdamantineRefinement.TryRefine(player, variant, item, 0f, out success), "stale recipe permission revalidated");
        AllArtifacts();
        Loot();
        UiCraft();
    }
}
