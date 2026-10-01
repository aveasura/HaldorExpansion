using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimInventoryRebind
    {
        internal static bool FixItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null) return false;
                GameObject prefab = ObjectDB.instance.GetItemPrefab(PeltOfHelheimItemRegistration.PrefabName);
                ItemDrop prefabDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (prefabDrop == null) return false;

                Inventory inventory = player.GetInventory();
                List<ItemDrop.ItemData> items = inventory?.GetAllItems();
                if (items == null) return false;

                bool found = false;
                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null ||
                        item.m_shared.m_name != PeltOfHelheimItemRegistration.ItemKey) continue;

                    found = true;
                    int quality = Mathf.Max(1, item.m_quality);
                    float durability = item.m_durability;
                    bool equipped = item.m_equipped;
                    int variant = item.m_variant;
                    long crafterId = item.m_crafterID;
                    string crafterName = item.m_crafterName;

                    item.m_dropPrefab = prefab;
                    item.m_shared = prefabDrop.m_itemData.m_shared;
                    item.m_quality = Mathf.Clamp(quality, 1, Mathf.Max(1, item.m_shared.m_maxQuality));
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
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] Pelt of Helheim inventory rebind error: {e}");
                return false;
            }
        }
    }
}
