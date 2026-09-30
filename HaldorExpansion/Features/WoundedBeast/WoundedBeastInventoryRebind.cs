using System.Collections.Generic;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.WoundedBeast
{
    internal static class WoundedBeastInventoryRebind
    {

        internal static bool FixWoundedBeastCapeItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null)
                    return false;

                GameObject capePrefab = ObjectDB.instance.GetItemPrefab(WoundedBeastItemRegistration.WoundedBeastCapePrefabName);
                if (capePrefab == null)
                    return false;

                ItemDrop prefabDrop = capePrefab.GetComponent<ItemDrop>();
                if (prefabDrop == null)
                    return false;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return false;

                bool found = false;
                List<ItemDrop.ItemData> items = inventory.GetAllItems();
                if (items == null)
                    return false;

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null)
                        continue;

                    if (item.m_shared.m_name != WoundedBeastItemRegistration.WoundedBeastCapeItemKey)
                        continue;

                    found = true;

                    int oldQuality = Mathf.Max(1, item.m_quality);
                    float oldDurability = item.m_durability;
                    bool oldEquipped = item.m_equipped;
                    int oldVariant = item.m_variant;
                    long oldCrafterID = item.m_crafterID;
                    string oldCrafterName = item.m_crafterName;

                    item.m_dropPrefab = capePrefab;
                    item.m_shared = prefabDrop.m_itemData.m_shared;

                    item.m_quality = Mathf.Clamp(oldQuality, 1, Mathf.Max(1, item.m_shared.m_maxQuality));
                    item.m_variant = oldVariant;
                    item.m_crafterID = oldCrafterID;
                    item.m_crafterName = oldCrafterName;
                    item.m_equipped = oldEquipped;

                    float maxDurability = item.m_shared.m_maxDurability + item.m_shared.m_durabilityPerLevel * (item.m_quality - 1);
                    item.m_durability = Mathf.Clamp(oldDurability, 0f, Mathf.Max(1f, maxDurability));

                    item.m_shared.m_canBeReparied = true;
                    item.m_shared.m_useDurability = true;
                }

                return found;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] FixWoundedBeastCapeItemsInInventory error: {e}");
                return false;
            }
        }
    }
}
