using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.DelayedDoom;
using UnityEngine;
class RebindTests
{
    static int failures;
    static void Check(bool condition, string message) { Console.WriteLine((condition ? "PASS " : "FAIL ") + message); if (!condition) failures++; }
    static void Main()
    {
        ObjectDB.instance = new ObjectDB();
        foreach (bool helmet in new[] { true, false })
        {
            string name = helmet ? BerserkerIds.HelmetPrefab : DelayedDoomItemRegistration.DelayedDoomChestPrefabName;
            string key = helmet ? "$helmet" : DelayedDoomIds.ItemKey;
            var prefab = new GameObject { name = name };
            prefab.drop = new ItemDrop();
            prefab.drop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData { m_name = key, m_maxQuality = 2, m_maxDurability = 1200, m_durabilityPerLevel = 200 };
            ObjectDB.instance.items[name] = prefab;
            var player = new Player();
            var item = new ItemDrop.ItemData { m_dropPrefab = prefab, m_shared = new ItemDrop.ItemData.SharedData { m_name = key }, m_quality = 7, m_durability = 25, m_equipped = true, m_variant = 3, m_crafterID = 42, m_crafterName = "owner" };
            player.inventory.items.Add(item);
            for (int pass = 0; pass < 2; pass++)
            {
                if (helmet) BerserkerHelmetInventoryRebind.FixItemsInInventory(player);
                else DelayedDoomInventoryRebind.FixDelayedDoomItemsInInventory(player);
                Check(item.m_quality == 7, name + " quality 7 survives rebind " + pass);
                Check(item.m_durability == 25, "rebind does not repair");
                Check(item.m_equipped && item.m_variant == 3 && item.m_crafterID == 42 && item.m_crafterName == "owner", "metadata survives");
            }
            item.m_quality = 100;
            item.m_durability = 0;
            if (helmet) BerserkerHelmetInventoryRebind.FixItemsInInventory(player); else DelayedDoomInventoryRebind.FixDelayedDoomItemsInInventory(player);
            Check(item.m_quality == 100 && item.m_durability == 0, "high level and broken state survive");
            item.m_quality = 0;
            if (helmet) BerserkerHelmetInventoryRebind.FixItemsInInventory(player); else DelayedDoomInventoryRebind.FixDelayedDoomItemsInInventory(player);
            Check(item.m_quality == 1, "invalid lower level normalized");
        }
        Environment.Exit(failures == 0 ? 0 : 1);
    }
}
