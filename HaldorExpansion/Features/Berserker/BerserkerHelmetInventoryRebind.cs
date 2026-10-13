using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerHelmetInventoryRebind
    {
        internal static bool FixItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null) return false;

                GameObject prefab = ObjectDB.instance.GetItemPrefab(BerserkerIds.HelmetPrefab);
                ItemDrop prefabDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (prefabDrop == null) return false;

                List<ItemDrop.ItemData> items = player.GetInventory()?.GetAllItems();
                if (items == null) return false;

                bool found = false;
                foreach (ItemDrop.ItemData item in items)
                {
                    // Identify our own prefab, never the vanilla HelmetBerserkerHood.
                    if (item == null || item.m_shared == null || item.m_dropPrefab == null ||
                        item.m_dropPrefab.name != BerserkerIds.HelmetPrefab) continue;

                    found = true;
                    int quality = Mathf.Max(1, item.m_quality);
                    float durability = item.m_durability;
                    bool equipped = item.m_equipped;
                    int variant = item.m_variant;
                    long crafterId = item.m_crafterID;
                    string crafterName = item.m_crafterName;

                    item.m_dropPrefab = prefab;
                    item.m_shared = prefabDrop.m_itemData.m_shared;
                    // Refinement can exceed m_maxQuality; preserve the saved level.
                    item.m_quality = quality;
                    item.m_variant = variant;
                    item.m_crafterID = crafterId;
                    item.m_crafterName = crafterName;
                    item.m_equipped = equipped;

                    float maxDurability = item.m_shared.m_maxDurability +
                                          item.m_shared.m_durabilityPerLevel * (item.m_quality - 1);
                    item.m_durability = Mathf.Clamp(durability, 0f, Mathf.Max(1f, maxDurability));
                }

                return found;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Helmet inventory rebind error: {e}");
                return false;
            }
        }
    }
}